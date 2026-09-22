namespace UPZMG.Implementations.Roles;

public class DeleteRole
{
    public required Guid RequesterKey { get; set; }
    public required Guid RoleId { get; set; }
}