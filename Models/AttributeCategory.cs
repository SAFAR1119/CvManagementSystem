using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class AttributeCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<AttributeDefinition> Attributes { get; set; }
        = new List<AttributeDefinition>();
}