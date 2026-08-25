namespace UPZMG.Implementations;

public sealed class AccountWebLoginRequest
{
	public string Email { get; init; } = string.Empty;
	public string Password { get; init; } = string.Empty;
	public string SharedSecret { get; init; } = string.Empty;
}
