using CvManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class CandidateProfileViewModel
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(200)]
    [Display(Name = "Professional title")]
    public string? ProfessionalTitle { get; set; }

    [StringLength(4000)]
    [Display(Name = "Professional summary")]
    public string? ProfessionalSummary { get; set; }

    [StringLength(50)]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [Url, StringLength(500)]
    [Display(Name = "LinkedIn URL")]
    public string? LinkedInUrl { get; set; }

    [Url, StringLength(500)]
    [Display(Name = "GitHub URL")]
    public string? GitHubUrl { get; set; }

    [Url, StringLength(500)]
    [Display(Name = "Portfolio URL")]
    public string? PortfolioUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "Personal photo URL")]
    public string? PhotoUrl { get; set; }

    public Guid Version { get; set; }

    public List<CandidateAttributeViewModel> Attributes { get; set; }
        = new();

    public List<AvailableProfileAttributeViewModel> AvailableAttributes { get; set; }
        = new();

    public List<CandidateProjectViewModel> Projects { get; set; }
        = new();

    public List<EducationViewModel> EducationEntries { get; set; } = new();

    public List<WorkExperienceViewModel> WorkExperiences { get; set; } = new();

    public List<CandidateCvSummaryViewModel> Cvs { get; set; }
        = new();

    public List<CandidateAttributeCategoryViewModel> Categories { get; set; }
        = new();

    public string? AttributeSearch { get; set; }

    public int? AttributeCategoryId { get; set; }

    public bool IsAdministratorView { get; set; }
}

public class CandidateAttributeViewModel
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public string? Value { get; set; }

    public Guid Version { get; set; }

    public List<CandidateAttributeOptionViewModel> Options { get; set; }
        = new();
}

public class AvailableProfileAttributeViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public DateTime? LastUsedAt { get; set; }
}

public class CandidateAttributeOptionViewModel
{
    public int Id { get; set; }

    public string Value { get; set; } = string.Empty;
}

public class CandidateAttributeCategoryViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CandidateProjectViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Period { get; set; } = string.Empty;

    public string DescriptionMarkdown { get; set; } = string.Empty;

    public List<string> TechnologyTags { get; set; } = new();
}

public class CandidateCvSummaryViewModel
{
    public int Id { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class EducationViewModel
{
    public int Id { get; set; }
    [StringLength(300)] public string Degree { get; set; } = string.Empty;
    [StringLength(300)] public string Institution { get; set; } = string.Empty;
    [DataType(DataType.Date)] public DateOnly StartDate { get; set; }
    [DataType(DataType.Date)] public DateOnly? EndDate { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class WorkExperienceViewModel
{
    public int Id { get; set; }
    [StringLength(300)] public string CompanyName { get; set; } = string.Empty;
    [StringLength(300)] public string JobTitle { get; set; } = string.Empty;
    [DataType(DataType.Date)] public DateOnly StartDate { get; set; }
    [DataType(DataType.Date)] public DateOnly? EndDate { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    [StringLength(1000)] public string? Technologies { get; set; }
    public int SortOrder { get; set; }
}
