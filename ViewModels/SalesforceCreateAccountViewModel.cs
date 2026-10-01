using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels;

public class SalesforceCreateAccountViewModel
{
    public string TargetUserId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Location")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Personal Photo URL")]
    public string PersonalPhotoUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Display(Name = "Salesforce Account Name")]
    public string AccountName { get; set; } = string.Empty;

    [StringLength(128)]
    [Display(Name = "Job Title")]
    public string JobTitle { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Additional Notes")]
    public string AdditionalNotes { get; set; } = string.Empty;
}

public class SalesforceIntegrationResultViewModel
{
    public string AccountId { get; set; } = string.Empty;

    public string ContactId { get; set; } = string.Empty;

    public string AccountUrl { get; set; } = string.Empty;

    public string ContactUrl { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}