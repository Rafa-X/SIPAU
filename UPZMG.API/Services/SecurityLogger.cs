namespace UPZMG.Api.Services;

public interface ISecurityEventLogger
{
    void LogTokenExchangeRejected(Guid userId, string reason);
    void LogTokenIssued(Guid userId, IReadOnlyCollection<string> roles, DateTime expiresUtc);
}

public sealed class SecurityLogger : ISecurityEventLogger
{
    private readonly ILogger<SecurityLogger> _logger;

    public SecurityLogger(ILogger<SecurityLogger> logger)
    {
        _logger = logger;
    }

    public void LogTokenExchangeRejected(Guid userId, string reason)
    {
        _logger.LogWarning($"JWT token exchange rejected for user {userId}. Reason: {reason}.");
    }

    public void LogTokenIssued(Guid userId, IReadOnlyCollection<string> roles, DateTime expiresUtc)
    {
        _logger.LogInformation(
            $"JWT access token issued for user {userId}. RoleCount: {roles.Count}. ExpiresUtc: {expiresUtc}.");
    }
}