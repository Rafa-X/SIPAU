namespace UPZMG.Implementations.Users;
using System.ComponentModel.DataAnnotations;
public class CreateUser
{
    public required Guid RequesterKey { get; set; }
    [MaxLength(100)]
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string RoleId { get; set; }
    public required bool IsActive { get; set; }
    public required string Password { get; set; }
}