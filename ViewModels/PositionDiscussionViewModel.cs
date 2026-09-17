namespace CvManagementSystem.ViewModels;

public class PositionDiscussionViewModel
{
    public int Id { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public string MessageMarkdown { get; set; } = string.Empty;

    public string RenderedHtml { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}