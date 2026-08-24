namespace UPZMG.Implementations;

public sealed class AccountWebLoginResponse
{
	public Guid UserId { get; init; }
	public string Email { get; init; } = string.Empty;
	public string RoleName { get; init; } = string.Empty;
}
