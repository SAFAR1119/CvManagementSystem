namespace CvManagementSystem.Models;

public class PositionAttribute
{
    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }
}