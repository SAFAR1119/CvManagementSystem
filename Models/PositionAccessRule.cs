using CvManagementSystem.Models;

namespace CvManagementSystem.Models;

public class PositionAccessRule
{
    public int Id { get; set; }

    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    public AccessRuleOperator Operator { get; set; }

    public string Value { get; set; } = string.Empty;
}