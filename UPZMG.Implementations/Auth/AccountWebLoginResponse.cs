namespace UPZMG.Implementations;

public sealed class AccountWebLoginResponse
{
	public Guid UserId { get; init; }
	public string Email { get; init; } = string.Empty;
	public string RoleName { get; init; } = string.Empty;

	/// <summary>The name recorded on the account, when it has one.</summary>
	public string? UserName { get; init; }

	/// <summary>The API refused because of too many failed attempts, not because of the password.</summary>
	public bool Throttled { get; init; }

	/// <summary>How long the account has to wait, as the API reported it.</summary>
	public int RetryAfterSeconds { get; init; }
}
