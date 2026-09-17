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