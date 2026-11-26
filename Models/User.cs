using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models;

/// <summary>
/// Represents a TechHive Solutions employee or partner user.
/// Data annotations are checked automatically because the controller uses [ApiController].
/// </summary>
public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email must include a valid domain (e.g., user@domain.com).")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 50 characters.")]
    public string Department { get; set; } = string.Empty;
}
