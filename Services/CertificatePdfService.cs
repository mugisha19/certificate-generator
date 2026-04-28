using System.Globalization;
using CertificateApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CertificateApp.Services;

public class CertificatePdfService
{
    private readonly byte[] _letterhead;
    private readonly byte[] _qrCode;
    private readonly byte[] _signature;

    public CertificatePdfService(IWebHostEnvironment environment)
    {
        var assetRoot = Path.Combine(environment.WebRootPath, "images", "certificate");
        _letterhead = File.ReadAllBytes(Path.Combine(assetRoot, "letterhead.jpg"));
        _qrCode     = File.ReadAllBytes(Path.Combine(assetRoot, "qr.png"));
        _signature  = File.ReadAllBytes(Path.Combine(assetRoot, "signature.png"));
    }

    public byte[] GenerateCertificate(CertificateOfAttendance cert)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // ─── A4 page rules ─────────────────────────────────────
                //   210 × 297 mm portrait, 20 mm margins on all sides
                //   (ISO 216 A4 + standard official-document margins).
                page.Size(PageSizes.A4);
                page.MarginHorizontal(20, Unit.Millimetre);
                page.MarginVertical(20, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(11.5f).FontColor("#111827"));

                page.Content().Column(column =>
                {
                    column.Item().Image(_letterhead).FitWidth();

                    // ─ Contact strip (matches the original letterhead text)
                    column.Item().PaddingTop(4).AlignCenter()
                        .Text("Mobile Phone : (+250)724 796 996 / 724 474 805/ 788 473 035")
                        .FontSize(10);

                    column.Item().PaddingTop(2).AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(s => s.FontSize(10));
                        text.Span("Email: ");
                        text.Span("registrar@auca.ac.rw").FontColor("#1d4ed8").Underline();
                        text.Span(", ||  ");
                        text.Span("juvenal.nsengiyumva@auca.ac.rw").FontColor("#1d4ed8").Underline();
                    });

                    column.Item().PaddingTop(6).BorderBottom(2).BorderColor("#0f4c81");

                    column.Item().PaddingTop(34).AlignLeft()
                        .Text($"Kigali, {DateTime.Now.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)}")
                        .FontSize(11);

                    column.Item().PaddingTop(28).AlignCenter()
                        .Text("CERTIFICATE OF GOOD STANDING")
                        .Bold().FontSize(16).Underline();

                    column.Item().PaddingTop(36).Text(text =>
                    {
                        text.Span("I, the undersigned, ");
                        text.Span(cert.ApprovedBy ?? "Eng. Nsengiyumva Juvenal").Bold();
                        text.Span(", Director for Admissions and Academic Records of the ");
                        text.Span("Adventist University of Central Africa").Bold();
                        text.Span(", hereby certify that:");
                    });

                    column.Item().PaddingTop(12).Text(cert.StudentNameWithComma()).Bold().FontSize(12);

                    column.Item().PaddingTop(8).Text(text =>
                    {
                        text.Span("Born on ");
                        text.Span(cert.FormattedBirthDate).Bold();
                    });

                    column.Item().PaddingTop(8).Text(text =>
                    {
                        text.Span("has been a regular student of this University, registered under ID No. ");
                        text.Span(cert.StudentID ?? "").Bold();
                        text.Span(",");
                    });

                    column.Item().PaddingTop(8).Text(text =>
                    {
                        text.Span("From ");
                        text.Span(cert.FormattedStudiedFrom).Bold();
                        text.Span(" to ");
                        text.Span(cert.FormattedStudiedTo).Bold();
                        text.Span(".");
                    });

                    column.Item().PaddingTop(8).Text(t => { t.Span("Year: ");          t.Span(cert.Year         ?? "").Bold(); });
                    column.Item().PaddingTop(4).Text(t => { t.Span("Faculty: ");       t.Span(cert.Faculty      ?? "").Bold(); });
                    column.Item().PaddingTop(4).Text(t => { t.Span("Major: ");         t.Span(cert.Major        ?? "").Bold(); });
                    column.Item().PaddingTop(4).Text(t => { t.Span("Academic year: "); t.Span(cert.AcademicYear ?? "").Bold(); });
                    column.Item().PaddingTop(4).Text(t => { t.Span("Validity: ");      t.Span(cert.CertificateValidity).Bold(); });

                    if (!string.IsNullOrWhiteSpace(cert.Comment))
                    {
                        column.Item().PaddingTop(12).Text(cert.Comment)
                            .Italic()
                            .FontSize(11);
                    }

                    column.Item().PaddingTop(12)
                        .Text("This certificate is issued for any legal or administrative purpose it may serve");

                    column.Item().PaddingTop(26).Row(row =>
                    {
                        // ─ Signature on the LEFT ──────────────────────────
                        row.ConstantItem(260).AlignBottom().Column(signature =>
                        {
                            signature.Item().Width(160).Image(_signature);
                            signature.Item().PaddingTop(2).BorderBottom(1).BorderColor(Colors.Black);
                            signature.Item().PaddingTop(4).Text(cert.ApprovedBy ?? "Eng. Nsengiyumva Juvenal")
                                .Bold().FontSize(11);
                            signature.Item().Text("Director for Admissions and Academic Records").FontSize(10);
                            signature.Item().Text("Adventist University of Central Africa").FontSize(10);
                        });

                        row.RelativeItem();

                        // ─ QR on the RIGHT ────────────────────────────────
                        row.ConstantItem(110).AlignBottom().Column(qr =>
                        {
                            qr.Item().Width(90).Height(90).Image(_qrCode);
                            qr.Item().PaddingTop(4).AlignCenter().Text("Scan to verify my Validity").FontSize(8);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}

internal static class CertificatePdfExtensions
{
    public static string StudentNameWithComma(this CertificateOfAttendance cert)
    {
        if (string.IsNullOrWhiteSpace(cert.StudentName))
            return "";

        return cert.StudentName.TrimEnd().EndsWith(",")
            ? cert.StudentName
            : $"{cert.StudentName},";
    }
}
