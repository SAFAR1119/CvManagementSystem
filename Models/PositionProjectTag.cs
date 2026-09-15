namespace CvManagementSystem.Models;

public class PositionProjectTag
{
    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    public int TechnologyTagId { get; set; }

    public TechnologyTag TechnologyTag { get; set; } = null!;
}