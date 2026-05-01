using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Shelves;

public sealed record UpdateShelfCommand(Guid ShelfId, UpdateShelfRequest Request) : IRequest<ShelfResponse>;

public sealed class UpdateShelfCommandHandler : IRequestHandler<UpdateShelfCommand, ShelfResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateShelfCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ShelfResponse> Handle(UpdateShelfCommand command, CancellationToken cancellationToken)
    {
        var shelf = await _unitOfWork.WarehouseShelves.GetByIdAsync(command.ShelfId, cancellationToken)
            ?? throw new KeyNotFoundException("Shelf was not found.");

        var areaExists = await _unitOfWork.WarehouseAreas.ExistsAsync(x => x.Id == command.Request.AreaId, cancellationToken);
        if (!areaExists)
        {
            throw new KeyNotFoundException("Area was not found.");
        }

        var duplicate = await _unitOfWork.WarehouseShelves.ExistsAsync(
            x => x.Id != command.ShelfId
              && x.AreaId == command.Request.AreaId
              && x.Name == command.Request.Name
              && x.Code == command.Request.Code,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Another shelf with the same name and code already exists in the area.");
        }

        var oldValues = new
        {
            shelf.AreaId,
            shelf.Name,
            shelf.Code,
            shelf.WeightLimitKg,
            shelf.Description,
            shelf.IsActive
        };

        _mapper.Map(command.Request, shelf);
        shelf.ModifiedAt = DateTime.UtcNow;
        shelf.ModifiedBy = GetActorId();

        _unitOfWork.WarehouseShelves.Update(shelf);
        await WriteAuditLogAsync(shelf.ModifiedBy, "UPDATE", "WarehouseShelf", shelf.Id, oldValues, command.Request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var locationCount = (await _unitOfWork.WarehouseLocations.FindAsync(x => x.ShelfId == shelf.Id, cancellationToken)).Count;
        var response = _mapper.Map<ShelfResponse>(shelf);
        response.LocationCount = locationCount;
        return response;
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
