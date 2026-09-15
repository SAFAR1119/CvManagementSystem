using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class CvAttributeValue
{
    public int Id { get; set; }

    public int CvId { get; set; }

    public Cv Cv { get; set; } = null!;

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    [StringLength(5000)]
    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid Version { get; set; } = Guid.NewGuid();
}