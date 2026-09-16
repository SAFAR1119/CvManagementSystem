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
    public string? Description { get; set; }

    [Display(Name = "Public position")]
    public bool IsPublic { get; set; } = true;

    [Range(1, 20)]
    [Display(Name = "Maximum projects")]
    public int MaxProjects { get; set; } = 3;

    public Guid Version { get; set; }

    public List<int> SelectedAttributeIds { get; set; } = new();

    public List<int> RequiredAttributeIds { get; set; } = new();

    public List<int> SelectedTechnologyTagIds { get; set; } = new();

    public List<SelectableAttributeViewModel> AvailableAttributes { get; set; }
        = new();

    public List<SelectableTechnologyTagViewModel> AvailableTechnologyTags { get; set; }
        = new();
}

public class SelectableAttributeViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;
}

public class SelectableTechnologyTagViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}