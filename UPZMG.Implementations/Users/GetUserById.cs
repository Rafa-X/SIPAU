namespace UPZMG.Implementations.Users;

public class GetUserById
{
    public required Guid RequesterKey { get; set; }
    public required Guid UserId { get; set; }
}