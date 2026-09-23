using CvManagementSystem.Models;

namespace CvManagementSystem.ViewModels;

public class CvDetailsViewModel
{
    public int Id { get; set; }

    public int PositionId { get; set; }

    public string PositionTitle { get; set; } = string.Empty;

    public string CandidateFirstName { get; set; } = string.Empty;

    public string CandidateLastName { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string? ProfessionalTitle { get; set; }

    public string? ProfessionalSummary { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? GitHubUrl { get; set; }

    public string? PortfolioUrl { get; set; }

    public string? PhotoUrl { get; set; }

    public Guid ProfileVersion { get; set; }

    public bool IsPublished { get; set; }

    public bool IsReadOnly { get; set; }

    public bool CanPublish { get; set; }

    public int MissingRequiredAttributes { get; set; }

    public List<CvAttributeViewModel> Attributes { get; set; }
        = new();

    public List<CvProjectViewModel> Projects { get; set; }
        = new();

    // True when the candidate's profile has at least one project at all,
    // regardless of whether any of them match this position's technology
    // tags. The Projects section is hidden entirely when this is false, so
    // a candidate with no projects never sees an empty section or heading.
    public bool HasAnyProjects { get; set; }

    // The position's MaxProjects value, surfaced so the editor can tell the
    // candidate how many of the eligible (tag-matching) projects listed in
    // Projects they may select. The pool itself is not capped to this
    // count — every eligible project is shown so a newly added one is
    // always visible — only the number the candidate may check is.
    public int MaxProjects { get; set; }

    public List<EducationViewModel> EducationEntries { get; set; } = new();

    public List<WorkExperienceViewModel> WorkExperiences { get; set; } = new();

    public int LikeCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }
}

public class CvAttributeViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public string? Value { get; set; }

    public string? ValueHtml { get; set; }

    public bool IsRequired { get; set; }

    public Guid Version { get; set; }

    public List<string> Options { get; set; }
        = new();

    public bool IsMissing =>
        string.IsNullOrWhiteSpace(Value);
}

public class CvProjectViewModel
{
    public int ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Period { get; set; } = string.Empty;

    public string DescriptionMarkdown { get; set; }
        = string.Empty;

    public List<string> TechnologyTags { get; set; }
        = new();

    // Whether this project is currently attached to the CV via the
    // CvProjects relationship. Editable by the candidate before publishing.
    public bool IsSelected { get; set; }
}
