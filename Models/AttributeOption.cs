using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class AttributeOption
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string Value { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}