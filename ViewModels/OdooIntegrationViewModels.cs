namespace CvManagementSystem.ViewModels;

public class OdooApiTokenViewModel
{
    public int PositionId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public string? GeneratedToken { get; set; }
}

public class OdooPositionAggregateViewModel
{
    public int ApiVersion { get; set; } = 1;

    public int PositionId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string Experience { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public string WorkMode { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public int MaxProjects { get; set; }

    public int PublishedCvCount { get; set; }

    public List<OdooAttributeAggregateViewModel> Attributes { get; set; }
        = new();
}

public class OdooAttributeAggregateViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }

    public OdooAggregateValueViewModel Aggregate { get; set; } = new();
}

public class OdooAggregateValueViewModel
{
    public string Kind { get; set; } = string.Empty;

    public int ValueCount { get; set; }

    public decimal? Average { get; set; }

    public decimal? Minimum { get; set; }

    public decimal? Maximum { get; set; }

    public int? TrueCount { get; set; }

    public int? FalseCount { get; set; }

    public List<OdooPopularValueViewModel> PopularValues { get; set; }
        = new();
}

public class OdooPopularValueViewModel
{
    public string Value { get; set; } = string.Empty;

    public int Count { get; set; }
}