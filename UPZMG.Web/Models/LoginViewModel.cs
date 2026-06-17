namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the Login page.
/// Preserves the email on failed attempts and carries error messages.
/// </summary>
public class LoginViewModel
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ErrorMessage { get; set; }
}
