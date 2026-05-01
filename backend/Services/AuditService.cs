using Microsoft.AspNetCore.Http;
using Storage.Data;

namespace Storage.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        Guid entityId,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var remoteIp = ipAddress ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var agent = userAgent ?? _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues),
            IpAddress = remoteIp,
            UserAgent = agent,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
