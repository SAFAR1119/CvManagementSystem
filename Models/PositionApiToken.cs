using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class PositionApiToken
{
    public int Id { get; set; }

    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}