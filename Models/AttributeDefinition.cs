using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class AttributeDefinition
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public AttributeDataType DataType { get; set; }

    public int CategoryId { get; set; }

    public AttributeCategory Category { get; set; } = null!;

    public ICollection<AttributeOption> Options { get; set; }
        = new List<AttributeOption>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Used for optimistic locking.
    public Guid Version { get; set; } = Guid.NewGuid();

    // Used later for "recently used" attribute selection.
    public DateTime? LastUsedAt { get; set; }

    public int UsageCount { get; set; }
}