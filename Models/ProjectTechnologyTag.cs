namespace CvManagementSystem.Models;

public class ProjectTechnologyTag
{
    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int TechnologyTagId { get; set; }

    public TechnologyTag TechnologyTag { get; set; } = null!;
}