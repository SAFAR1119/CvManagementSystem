using Microsoft.AspNetCore.Identity;

namespace CvManagementSystem.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsBlocked { get; set; }

    public CandidateProfile? CandidateProfile { get; set; }
}