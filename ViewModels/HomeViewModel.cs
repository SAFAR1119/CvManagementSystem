namespace CvManagementSystem.ViewModels;

public class HomeViewModel
{
    public List<HomePositionViewModel> LatestPositions { get; set; } = new();

    public List<HomePositionViewModel> PopularPositions { get; set; } = new();

    public List<HomeTagViewModel> TechnologyTags { get; set; } = new();

    public DashboardStatisticsViewModel Statistics { get; set; } = new();

    public bool IsRecruiter { get; set; }

    public bool IsAdministrator { get; set; }
}

public class HomePositionViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int SubmittedCvCount { get; set; }

    public bool IsPublic { get; set; }
}

public class HomeTagViewModel
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public string Url { get; set; } = string.Empty;
}

public class DashboardStatisticsViewModel
{
    public int NewCvsLast24Hours { get; set; }

    public int TotalPositions { get; set; }

    public int TotalCandidates { get; set; }

    public int TotalRecruiters { get; set; }

    public int TotalSubmittedCvs { get; set; }
}