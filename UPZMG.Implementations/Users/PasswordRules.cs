namespace UPZMG.Implementations.Users;

/// <summary>
/// What a password has to be. The form and the API ask the same thing: with the rule only in the
/// form, a request that reached the API directly could store a one-character password, or one of
/// only spaces, which sign-in always refuses and so leaves an account nobody can enter.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 256;

    /// <summary>Why this password cannot be used, or null when it can.</summary>
    public static string? Problem(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return "La contraseña es obligatoria.";
        if (password.Length < MinimumLength) return $"Debe tener al menos {MinimumLength} caracteres.";

        return password.Length > MaximumLength ? $"No puede pasar de {MaximumLength} caracteres." : null;
    }
}
