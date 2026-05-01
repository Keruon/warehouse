using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;

namespace Storage.Controllers.Shelves;

public sealed record DeleteShelfCommand(Guid ShelfId) : IRequest<bool>;

public sealed class DeleteShelfCommandHandler : IRequestHandler<DeleteShelfCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteShelfCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(DeleteShelfCommand command, CancellationToken cancellationToken)
    {
        var shelf = await _unitOfWork.WarehouseShelves.GetByIdAsync(command.ShelfId, cancellationToken)
            ?? throw new KeyNotFoundException("Shelf was not found.");

        var hasActiveLocations = await _unitOfWork.WarehouseLocations.ExistsAsync(x => x.ShelfId == command.ShelfId, cancellationToken);
        if (hasActiveLocations)
        {
            throw new InvalidOperationException("Cannot delete shelf while it has active locations.");
        }

        var actorId = GetActorId();
        var oldValues = new
        {
            shelf.AreaId,
            shelf.Name,
            shelf.Code,
            shelf.WeightLimitKg,
            shelf.Description,
            shelf.IsActive
        };

        shelf.IsActive = false;
        shelf.ModifiedAt = DateTime.UtcNow;
        shelf.ModifiedBy = actorId;

        _unitOfWork.WarehouseShelves.Update(shelf);
        await WriteAuditLogAsync(actorId, "DELETE", "WarehouseShelf", shelf.Id, oldValues, new { shelf.IsActive }, cancellationToken);
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
