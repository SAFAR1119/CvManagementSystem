using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Models;

public class PositionDiscussion
{
    public int Id { get; set; }

    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    [Required]
    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser Author { get; set; } = null!;

    [Required]
    public string MessageMarkdown { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}