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

public class SelectableAttributeViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }
}

public class SelectableTechnologyTagViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class SelectableCategoryViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PositionAccessRuleViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string AttributeName { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public AccessRuleOperator Operator { get; set; }

    public string Value { get; set; } = string.Empty;
}