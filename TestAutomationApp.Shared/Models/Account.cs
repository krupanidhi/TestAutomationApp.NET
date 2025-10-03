using System.ComponentModel.DataAnnotations;

namespace TestAutomationApp.Shared.Models;

public class Account
{
    public int Id { get; set; }

    public string? Salutation { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(32, ErrorMessage = "First name cannot exceed 32 characters")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(1, ErrorMessage = "Middle initial cannot exceed 1 character")]
    public string? MiddleInitial { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(32, ErrorMessage = "Last name cannot exceed 32 characters")]
    public string LastName { get; set; } = string.Empty;

    public string? EmployeeId { get; set; }

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string EmailAddress { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; set; }

    [Phone(ErrorMessage = "Invalid fax number")]
    public string? FaxNumber { get; set; }

    public string? OrganizationType { get; set; }

    [StringLength(64, ErrorMessage = "Coordinator name cannot exceed 64 characters")]
    public string? CoordinatorName { get; set; }

    public string? Status { get; set; }

    public string? WorkLocation { get; set; }

    [Required(ErrorMessage = "Office/Bureau is required")]
    public string OfficeBureau { get; set; } = string.Empty;

    public string? OfficeSearchList { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
