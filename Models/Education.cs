using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class Education
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }

    public CandidateProfile CandidateProfile { get; set; } = null!;

    [Required, StringLength(300)]
    public string Degree { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Institution { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
}
