using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class CandidateProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(200)]
    public string? ProfessionalTitle { get; set; }

    [StringLength(4000)]
    public string? ProfessionalSummary { get; set; }

    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? LinkedInUrl { get; set; }

    [StringLength(500)]
    public string? GitHubUrl { get; set; }

    [StringLength(500)]
    public string? PortfolioUrl { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<CandidateAttributeValue> AttributeValues { get; set; }
        = new List<CandidateAttributeValue>();

    public ICollection<Project> Projects { get; set; }
        = new List<Project>();

    public ICollection<Education> EducationEntries { get; set; }
        = new List<Education>();

    public ICollection<WorkExperience> WorkExperiences { get; set; }
        = new List<WorkExperience>();

    public ICollection<Cv> Cvs { get; set; }
        = new List<Cv>();
}
