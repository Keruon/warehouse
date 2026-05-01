using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;

namespace Storage.Controllers.Locations;

public sealed record DeleteLocationCommand(Guid LocationId) : IRequest<bool>;

public sealed class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteLocationCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(DeleteLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _unitOfWork.WarehouseLocations.GetByIdAsync(command.LocationId, cancellationToken)
            ?? throw new KeyNotFoundException("Location was not found.");

        var hasStock = await _unitOfWork.StockLocations.ExistsAsync(x => x.LocationId == command.LocationId && x.Quantity > 0, cancellationToken);
        if (hasStock)
        {
            throw new InvalidOperationException("Cannot delete location while it still has stock.");
        }

        var actorId = GetActorId();
        var oldValues = new
        {
            location.ShelfId,
            location.Name,
            location.Code,
            location.BinX,
            location.BinY,
            location.IsReserved,
            location.IsActive
        };

        location.IsActive = false;
        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = actorId;

        _unitOfWork.WarehouseLocations.Update(location);
        await WriteAuditLogAsync(actorId, "DELETE", "WarehouseLocation", location.Id, oldValues, new { location.IsActive }, cancellationToken);
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
