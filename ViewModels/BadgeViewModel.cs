namespace CvManagementSystem.ViewModels;

public class BadgeViewModel
{
    public string Category { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class BadgeSummaryViewModel
{
    public int ProjectCount { get; set; }

    public int PublishedCvCount { get; set; }

    public int LikeCount { get; set; }

    public List<BadgeViewModel> Earned { get; set; } = new();
}