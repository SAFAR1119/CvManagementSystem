using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class CandidateProfileViewModel
{
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
    [Display(Name = "Photo URL")]
    public string? PhotoUrl { get; set; }

    public List<CandidateAttributeViewModel> Attributes { get; set; }
        = new();

    public List<CandidateProjectViewModel> Projects { get; set; }
        = new();

    public List<CandidateCvSummaryViewModel> Cvs { get; set; }
        = new();
}

public class CandidateAttributeViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string? Value { get; set; }
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