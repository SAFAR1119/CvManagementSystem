using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class TechnologyTag
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ProjectTechnologyTag> ProjectTags { get; set; }
        = new List<ProjectTechnologyTag>();
}