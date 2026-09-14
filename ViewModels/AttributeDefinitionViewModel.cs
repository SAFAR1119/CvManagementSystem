using CvManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class AttributeDefinitionViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Attribute name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required]
    [Display(Name = "Data type")]
    public AttributeDataType DataType { get; set; }

    // One option per line for Dropdown attributes.
    [Display(Name = "Dropdown options")]
    public string? OptionsText { get; set; }

    public Guid Version { get; set; }
}