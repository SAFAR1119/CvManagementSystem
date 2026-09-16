namespace CvManagementSystem.ViewModels;

public class GeneratedCvViewModel
{
    public int PositionId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string? PhotoUrl { get; set; }

    public List<GeneratedCvAttributeViewModel> Attributes { get; set; }
        = new();

    public List<GeneratedCvProjectViewModel> Projects { get; set; }
        = new();

    public int MaxProjects { get; set; }

    public bool CanPublish =>
        Attributes
            .Where(x => x.IsRequired)
            .All(x => !string.IsNullOrWhiteSpace(x.Value));

    public int MissingRequiredAttributes =>
        Attributes.Count(x =>
            x.IsRequired &&
            string.IsNullOrWhiteSpace(x.Value));
}

public class GeneratedCvAttributeViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string? Value { get; set; }

    public bool IsRequired { get; set; }

    public bool IsMissing =>
        string.IsNullOrWhiteSpace(Value);
}

public class GeneratedCvProjectViewModel
{
    public int ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Period { get; set; } = string.Empty;

    public string DescriptionMarkdown { get; set; } = string.Empty;

    public List<string> TechnologyTags { get; set; }
        = new();
}