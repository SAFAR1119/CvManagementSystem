namespace CvManagementSystem.ViewModels;

public class GlobalSearchViewModel
{
    public string Query { get; set; } = string.Empty;

    public List<GlobalSearchPositionViewModel> Positions { get; set; } = new();

    public List<GlobalSearchCvViewModel> Cvs { get; set; } = new();

    public bool CanSeeCvs { get; set; }
}

public class GlobalSearchPositionViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int SubmittedCvCount { get; set; }
}

public class GlobalSearchCvViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int PositionId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public int LikeCount { get; set; }

    public DateTime UpdatedAt { get; set; }
}