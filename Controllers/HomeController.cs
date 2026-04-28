using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CertificateApp.Data;
using CertificateApp.Models;
using CertificateApp.Services;

namespace CertificateApp.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly CertificatePdfService _pdfService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(AppDbContext db, CertificatePdfService pdfService, ILogger<HomeController> logger)
    {
        _db = db;
        _pdfService = pdfService;
        _logger = logger;
    }

    // GET: /  — Dashboard listing all certificates
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _db.Certificates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                (c.StudentName != null && c.StudentName.Contains(search)) ||
                (c.StudentID != null && c.StudentID.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        var certs = await query
            .OrderBy(c => c.StudentName)
            .ThenBy(c => c.StudentID)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Total = await _db.Certificates.CountAsync();
        ViewBag.ActiveCount = await _db.Certificates.CountAsync(c => c.Status == "Active");

        return View(certs);
    }

    // GET: /Home/Create
    public IActionResult Create()
    {
        var today = DateTime.Today;
        var defaultStart = new DateTime(today.Year, today.Month, 1);

        var model = new CertificateOfAttendance
        {
            StudiedFrom  = defaultStart,
            StudiedTo    = null, // null → renders as "Date" (currently studying)
            AcademicYear = $"{today.Year}-{today.Year + 1} (September {today.Year}-August {today.Year + 1})",
            ApprovedBy   = "Eng. Nsengiyumva Juvenal",
            Status       = "Active"
        };
        return View(model);
    }

    // POST: /Home/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CertificateOfAttendance model)
    {
        if (!ModelState.IsValid)
            return View(model);

        NormalizeCertificate(model);

        var studentId = model.StudentID?.Trim();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            ModelState.AddModelError(nameof(model.StudentID), "Student ID is required");
            return View(model);
        }

        model.StudentID = studentId;

        var exists = await _db.Certificates.AnyAsync(c => c.StudentID == studentId);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.StudentID), "A certificate with this Student ID already exists.");
            return View(model);
        }

        _db.Certificates.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Certificate for {model.StudentName} created successfully!";
        return RedirectToAction(nameof(Details), new { studentId = model.StudentID });
    }

    // GET: /Home/Details/{studentId}
    public async Task<IActionResult> Details(string studentId)
    {
        var cert = await _db.Certificates.FindAsync(studentId);
        if (cert == null) return NotFound();
        return View(cert);
    }

    // GET: /Home/Edit/{studentId}
    public async Task<IActionResult> Edit(string studentId)
    {
        var cert = await _db.Certificates.FindAsync(studentId);
        if (cert == null) return NotFound();
        cert.OriginalStudentID = cert.StudentID;
        return View(cert);
    }

    // POST: /Home/Edit/{studentId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string studentId, CertificateOfAttendance model)
    {
        if (!ModelState.IsValid) return View(model);

        NormalizeCertificate(model);

        var originalStudentId = model.OriginalStudentID?.Trim() ?? studentId?.Trim();
        var updatedStudentId = model.StudentID?.Trim();

        if (string.IsNullOrWhiteSpace(originalStudentId) || string.IsNullOrWhiteSpace(updatedStudentId))
            return BadRequest();

        var current = await _db.Certificates.FindAsync(originalStudentId);
        if (current == null) return NotFound();

        if (!string.Equals(originalStudentId, updatedStudentId, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _db.Certificates.AnyAsync(c => c.StudentID == updatedStudentId);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(model.StudentID), "A certificate with this Student ID already exists.");
                return View(model);
            }

            _db.Certificates.Remove(current);
            await _db.SaveChangesAsync();

            model.StudentID = updatedStudentId;
            model.OriginalStudentID = updatedStudentId;
            _db.Certificates.Add(model);
        }
        else
        {
            current.StudentName = model.StudentName;
            current.BornDate = model.BornDate;
            current.StudiedFrom = model.StudiedFrom;
            current.StudiedTo = model.StudiedTo;
            current.Year = model.Year;
            current.Faculty = model.Faculty;
            current.Major = model.Major;
            current.AcademicYear = model.AcademicYear;
            current.ApprovedBy = model.ApprovedBy;
            current.Comment = model.Comment;
            current.Status = model.Status;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Certificate updated successfully!";
        return RedirectToAction(nameof(Details), new { studentId = updatedStudentId });
    }

    // POST: /Home/Delete/{studentId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string studentId)
    {
        var cert = await _db.Certificates.FindAsync(studentId);
        if (cert != null)
        {
            _db.Certificates.Remove(cert);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Certificate deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /Home/GeneratePdf/{studentId}
    public async Task<IActionResult> GeneratePdf(string studentId)
    {
        var cert = await _db.Certificates.FindAsync(studentId);
        if (cert == null) return NotFound();

        try
        {
            var pdfBytes = _pdfService.GenerateCertificate(cert);
            var fileName = $"Certificate_{cert.StudentID}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed for cert {StudentId}", studentId);
            TempData["Error"] = "PDF generation failed. Please try again.";
            return RedirectToAction(nameof(Details), new { studentId });
        }
    }

    // GET: /Home/PreviewPdf/{studentId}  — inline browser preview
    public async Task<IActionResult> PreviewPdf(string studentId)
    {
        var cert = await _db.Certificates.FindAsync(studentId);
        if (cert == null) return NotFound();

        var pdfBytes = _pdfService.GenerateCertificate(cert);
        return File(pdfBytes, "application/pdf");
    }

    private static void NormalizeCertificate(CertificateOfAttendance model)
    {
        model.StudentName  = model.StudentName?.Trim();
        model.StudentID    = model.StudentID?.Trim();
        model.Year         = model.Year?.Trim();
        model.Faculty      = model.Faculty?.Trim();
        model.Major        = model.Major?.Trim();
        model.AcademicYear = model.AcademicYear?.Trim();
        model.ApprovedBy   = model.ApprovedBy?.Trim();
        model.Comment      = model.Comment?.Trim();
        model.Status       = model.Status?.Trim();

        model.BornDate    = ToUtcDate(model.BornDate);
        model.StudiedFrom = ToUtcDate(model.StudiedFrom);
        model.StudiedTo   = ToUtcDate(model.StudiedTo);
    }

    private static DateTime? ToUtcDate(DateTime? value)
    {
        if (!value.HasValue) return null;
        return DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc);
    }
}
