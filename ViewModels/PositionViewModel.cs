using CvManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class PositionViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Position title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Experience")]
    public ExperienceLevel Experience { get; set; }
        = ExperienceLevel.Fresher;

    [Display(Name = "Employment type")]
    public EmploymentType EmploymentType { get; set; }
        = EmploymentType.FullTime;

    [Display(Name = "Work mode")]
    public WorkMode WorkMode { get; set; }
        = WorkMode.OnSite;

    [StringLength(2000)]
    [Display(Name = "Short description")]
    public string? Description { get; set; }

    [Display(Name = "Public position")]
    public bool IsPublic { get; set; } = true;

    [Range(1, 20)]
    [Display(Name = "Maximum projects")]
    public int MaxProjects { get; set; } = 3;

    public Guid Version { get; set; }

    public List<int> SelectedAttributeIds { get; set; }
        = new();

    public List<int> RequiredAttributeIds { get; set; }
        = new();

    public List<int> SelectedTechnologyTagIds { get; set; }
        = new();

    public List<PositionAccessRuleViewModel> AccessRules { get; set; }
        = new();

    public List<SelectableAttributeViewModel> AvailableAttributes { get; set; }
        = new();

    public List<SelectableTechnologyTagViewModel> AvailableTechnologyTags { get; set; }
        = new();

    public List<SelectableCategoryViewModel> AttributeCategories { get; set; }
        = new();
}


// =============================================================
// SELECTABLE ATTRIBUTE
// =============================================================

public class SelectableAttributeViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }
}


// =============================================================
// SELECTABLE TECHNOLOGY TAG
// =============================================================

public class SelectableTechnologyTagViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}


// =============================================================
// SELECTABLE CATEGORY
// =============================================================

public class SelectableCategoryViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}


// =============================================================
// POSITION ACCESS RULE
// =============================================================

public class PositionAccessRuleViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string AttributeName { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public AccessRuleOperator Operator { get; set; }

    public string Value { get; set; } = string.Empty;
}


// =============================================================
// POSITION LIST
// =============================================================

public class PositionListViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public ExperienceLevel Experience { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public WorkMode WorkMode { get; set; }

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid Version { get; set; }

    public int SubmittedCvCount { get; set; }

    public List<string> TechnologyTags { get; set; } = new();
}


// =============================================================
// POSITION DETAILS
// =============================================================

public class PositionDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public ExperienceLevel Experience { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public WorkMode WorkMode { get; set; }

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid Version { get; set; }

    public List<PositionAttributeListViewModel> Attributes
        { get; set; } = new();

    public List<string> ProjectTags
        { get; set; } = new();

    public List<PositionAccessRuleViewModel> AccessRules
        { get; set; } = new();

    public List<PositionCvSummaryViewModel> Cvs
        { get; set; } = new();
}


// =============================================================
// POSITION ATTRIBUTE
// =============================================================

public class PositionAttributeListViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }
}


// =============================================================
// POSITION CV SUMMARY
// =============================================================

public class PositionCvSummaryViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int LikeCount { get; set; }
}