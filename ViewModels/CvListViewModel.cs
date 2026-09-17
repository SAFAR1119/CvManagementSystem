using CvManagementSystem.Models;

namespace CvManagementSystem.ViewModels;

public class CvListViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int PositionId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public int CandidateProfileId { get; set; }

    public string CandidateName { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int LikeCount { get; set; }
}