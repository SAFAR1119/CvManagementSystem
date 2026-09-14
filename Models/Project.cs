using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class Project
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }

    public CandidateProfile CandidateProfile { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Required]
    public string DescriptionMarkdown { get; set; } = string.Empty;

    public ICollection<ProjectTechnologyTag> TechnologyTags { get; set; }
        = new List<ProjectTechnologyTag>();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}