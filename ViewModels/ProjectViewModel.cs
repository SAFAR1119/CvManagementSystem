using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class ProjectViewModel
{
    public int Id { get; set; }

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

    [Display(Name = "Technology tags")]
    public List<int> SelectedTechnologyTagIds { get; set; } = new();

    public List<SelectableTechnologyTagViewModel> AvailableTechnologyTags { get; set; }
        = new();

    public string Period
    {
        get
        {
            var start = StartDate.ToString("MMM yyyy");

            if (!EndDate.HasValue)
            {
                return $"{start} - Present";
            }

            return $"{start} - {EndDate.Value:MMM yyyy}";
        }
    }
}