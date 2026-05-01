using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;

namespace Storage.Controllers.Areas;

public sealed record DeleteAreaCommand(Guid AreaId) : IRequest<bool>;

public sealed class DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteAreaCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(DeleteAreaCommand command, CancellationToken cancellationToken)
    {
        var area = await _unitOfWork.WarehouseAreas.GetByIdAsync(command.AreaId, cancellationToken)
            ?? throw new KeyNotFoundException("Area was not found.");

        var hasActiveShelves = await _unitOfWork.WarehouseShelves.ExistsAsync(x => x.AreaId == command.AreaId, cancellationToken);
        if (hasActiveShelves)
        {
            throw new InvalidOperationException("Cannot delete area while it has active shelves.");
        }

        var actorId = GetActorId();
        var oldValues = new
        {
            area.Name,
            area.Code,
            area.ZoneType,
            area.FloorLevel,
            area.Description,
            area.IsActive
        };

        area.IsActive = false;
        area.ModifiedAt = DateTime.UtcNow;
        area.ModifiedBy = actorId;

        _unitOfWork.WarehouseAreas.Update(area);
        await WriteAuditLogAsync(actorId, "DELETE", "WarehouseArea", area.Id, oldValues, new { area.IsActive }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Guid GetActorId()
    {
        var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out var parsed) ? parsed : Guid.Empty;
    }

    private async Task WriteAuditLogAsync(Guid userId, string action, string entityType, Guid entityId, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    }
}
