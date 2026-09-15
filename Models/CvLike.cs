namespace CvManagementSystem.Models;

public class CvLike
{
    public int Id { get; set; }

    public int CvId { get; set; }

    public Cv Cv { get; set; } = null!;

    public string RecruiterId { get; set; } = string.Empty;

    public ApplicationUser Recruiter { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}