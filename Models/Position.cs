using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace CvManagementSystem.Models;

public enum ExperienceLevel
{
    Fresher = 0,
    OneYear = 1,
    TwoYears = 2,
    ThreeYears = 3,
    FourYears = 4,
    FiveYears = 5,
    SixYears = 6,
    SevenYears = 7,
    EightYears = 8,
    NineYears = 9,
    TenYears = 10
}

public enum EmploymentType
{
    FullTime = 1,
    PartTime = 2
}

public enum WorkMode
{
    OnSite = 1,
    Remote = 2,
    Hybrid = 3
}

public class Position
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    public ExperienceLevel Experience { get; set; }
        = ExperienceLevel.Fresher;

    public EmploymentType EmploymentType { get; set; }
        = EmploymentType.FullTime;

    public WorkMode WorkMode { get; set; }
        = WorkMode.OnSite;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = true;

    [Range(1, 20)]
    public int MaxProjects { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid Version { get; set; } = Guid.NewGuid();

    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public ICollection<PositionAttribute> Attributes { get; set; }
        = new List<PositionAttribute>();

    public ICollection<PositionProjectTag> ProjectTags { get; set; }
        = new List<PositionProjectTag>();

    public ICollection<PositionAccessRule> AccessRules { get; set; }
        = new List<PositionAccessRule>();
}