namespace UPZMG.Implementations.Users;

/// <summary>Suspends or restores an account: the reversible alternative to deleting a user.</summary>
public class SetUserActive
{
    public required Guid RequesterKey { get; set; }
    public required Guid UserId { get; set; }
    public required bool IsActive { get; set; }
}
