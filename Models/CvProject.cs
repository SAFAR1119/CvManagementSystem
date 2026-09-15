namespace CvManagementSystem.Models;

public class CvProject
{
    public int CvId { get; set; }

    public Cv Cv { get; set; } = null!;

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int SortOrder { get; set; }
}