using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace CertificateApp.Models;

[Table("CertificateOfAttendance")]
public class CertificateOfAttendance
{
    [Display(Name = "Student Name")]
    [Required(ErrorMessage = "Student name is required")]
    public string? StudentName { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime? BornDate { get; set; }

    [Display(Name = "Student ID")]
    [Required(ErrorMessage = "Student ID is required")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string? StudentID { get; set; }

    [Display(Name = "Studied From")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Start date is required")]
    public DateTime? StudiedFrom { get; set; }

    [Display(Name = "Studied To")]
    [DataType(DataType.Date)]
    public DateTime? StudiedTo { get; set; }

    [Display(Name = "Year / Semester")]
    public string? Year { get; set; }

    [Display(Name = "Faculty")]
    [Required(ErrorMessage = "Faculty is required")]
    public string? Faculty { get; set; }

    [Display(Name = "Major")]
    [Required(ErrorMessage = "Major is required")]
    public string? Major { get; set; }

    [Display(Name = "Academic Year")]
    public string? AcademicYear { get; set; }

    [Display(Name = "Approved By")]
    public string? ApprovedBy { get; set; }

    [Display(Name = "Comment")]
    public string? Comment { get; set; }

    [Display(Name = "Status")]
    public string? Status { get; set; }

    // ─── Computed display helpers ─────────────────────────────

    [NotMapped]
    public string FormattedBirthDate =>
        BornDate.HasValue
            ? BornDate.Value.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)
            : "";

    [NotMapped]
    public string FormattedStudiedFrom =>
        StudiedFrom.HasValue
            ? StudiedFrom.Value.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            : "";

    /// <summary>
    /// "to" display value: when no end date is set (current student) we render "Date".
    /// </summary>
    [NotMapped]
    public string FormattedStudiedTo =>
        StudiedTo.HasValue
            ? StudiedTo.Value.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            : "Date";

    [NotMapped]
    public string CertificateValidity
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AcademicYear))
                return "";

            var start = AcademicYear.IndexOf('(');
            var end = AcademicYear.IndexOf(')');

            if (start >= 0 && end > start)
                return AcademicYear[(start + 1)..end];

            return AcademicYear;
        }
    }

    [NotMapped]
    public string? OriginalStudentID { get; set; }
}
