using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CvManagementSystem.ViewModels;

public enum SupportPriority
{
    High,
    Average,
    Low
}

public class SupportTicketViewModel
{
    [Required]
    [StringLength(500)]
    [Display(Name = "Summary")]
    public string Summary { get; set; } = string.Empty;

    [Display(Name = "Priority")]
    public SupportPriority Priority { get; set; } = SupportPriority.Average;

    [Display(Name = "Position")]
    public int? PositionId { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Positions { get; set; } =
        Enumerable.Empty<SelectListItem>();
}