namespace CvManagementSystem.ViewModels;

public class AdminUsersViewModel
{
    public List<AdminUserRowViewModel> Users { get; set; }
        = new();
}

public class AdminUserRowViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsBlocked { get; set; }

    public List<string> Roles { get; set; }
        = new();
}