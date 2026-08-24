using System.ComponentModel.DataAnnotations;

namespace UPZMG.Implementations.Users;

public class EditUser
{
    public required Guid RequesterKey { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(100)]
    public required string UserName { get; set; }
    [MaxLength(256)]
    public string? Password { get; set; }
    public bool IsActive { get; set; }
    public string? RoleId { get; set; }
}