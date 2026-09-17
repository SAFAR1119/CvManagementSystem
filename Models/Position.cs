using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class Position
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = true;

    [Range(1, 20)]
    public int MaxProjects { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<PositionAttribute> Attributes { get; set; }
        = new List<PositionAttribute>();

    public ICollection<PositionProjectTag> ProjectTags { get; set; }
        = new List<PositionProjectTag>();

    public ICollection<PositionAccessRule> AccessRules { get; set; }
        = new List<PositionAccessRule>();
}