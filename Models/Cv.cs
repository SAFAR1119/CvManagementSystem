using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class Cv
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }

    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<CvAttributeValue> AttributeValues { get; set; }
        = new List<CvAttributeValue>();

    public ICollection<CvProject> Projects { get; set; }
        = new List<CvProject>();
}