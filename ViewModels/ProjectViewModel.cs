using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class ProjectViewModel
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }

    public string? UserId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Project name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start date")]
    public DateOnly StartDate { get; set; }

    [Display(Name = "End date")]
    public DateOnly? EndDate { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string DescriptionMarkdown { get; set; } = string.Empty;

    public List<string> TechnologyTags { get; set; } = new();

    public List<string> AvailableTechnologyTags { get; set; } = new();

    public string Period
    {
        get
        {
            var start = StartDate.ToString("MMM yyyy");

            return EndDate.HasValue
                ? $"{start} - {EndDate.Value:MMM yyyy}"
                : $"{start} - Present";
        }
    }
}