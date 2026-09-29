namespace UPZMG.Implementations.Users;

/// <summary>
/// A user as the API hands it back. It deliberately has no password: the hash is the one thing
/// the authentication service must keep to itself, and a response that carries it ends up in
/// client memory and in whatever logs the response passes through.
/// </summary>
public sealed record UserView(
    Guid Id,
    Guid EmployeeId,
    string UserName,
    string Email,
    bool IsActive,
    DateTime? LastLogin,
    Guid? RoleId,
    string? RoleName);
