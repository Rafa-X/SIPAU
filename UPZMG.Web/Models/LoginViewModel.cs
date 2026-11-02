namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the Login page.
/// Preserves the email on failed attempts and carries error messages.
/// </summary>
public class LoginViewModel
{
    public string? Email { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; } = string.Empty;

    /// <summary>The screen that asked for the sign-in, so the person lands back on it.</summary>
    public string? ReturnUrl { get; set; }
}
