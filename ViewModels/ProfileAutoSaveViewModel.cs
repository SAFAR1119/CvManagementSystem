using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class ProfileAutoSaveViewModel
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public Guid Version { get; set; }

    public List<ProfileAutoSaveAttributeViewModel> Attributes { get; set; }
        = new();
}

public class ProfileAutoSaveAttributeViewModel
{
    public int AttributeDefinitionId { get; set; }

    public string? Value { get; set; }

    public Guid Version { get; set; }
}