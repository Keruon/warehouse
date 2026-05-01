using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Locations;

public sealed record UpdateLocationCommand(Guid LocationId, UpdateLocationRequest Request) : IRequest<LocationResponse>;

public sealed class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, LocationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateLocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LocationResponse> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _unitOfWork.WarehouseLocations.GetByIdAsync(command.LocationId, cancellationToken)
            ?? throw new KeyNotFoundException("Location was not found.");

        var shelf = await _unitOfWork.WarehouseShelves.GetByIdAsync(command.Request.ShelfId, cancellationToken)
            ?? throw new KeyNotFoundException("Shelf was not found.");

        var duplicateBin = await _unitOfWork.WarehouseLocations.ExistsAsync(
            x => x.Id != command.LocationId
              && x.ShelfId == command.Request.ShelfId
              && x.BinX == command.Request.BinX
              && x.BinY == command.Request.BinY,
            cancellationToken);
        if (duplicateBin)
        {
            throw new InvalidOperationException("The bin coordinates are already in use on this shelf.");
        }

        var oldValues = new
        {
            location.ShelfId,
            location.Name,
            location.Code,
            location.Description,
            location.BinX,
            location.BinY,
            location.Depth,
            location.Width,
            location.Height,
            location.Volume,
            location.IsReserved,
            location.IsActive
        };

        _mapper.Map(command.Request, location);
        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = GetActorId();

        _unitOfWork.WarehouseLocations.Update(location);
        await WriteAuditLogAsync(location.ModifiedBy, "UPDATE", "WarehouseLocation", location.Id, oldValues, command.Request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var currentStock = (await _unitOfWork.StockLocations.FindAsync(x => x.LocationId == location.Id, cancellationToken)).Sum(x => x.Quantity);

        var response = _mapper.Map<LocationResponse>(location);
        response.AreaId = shelf.AreaId;
        response.CurrentStockQuantity = currentStock;
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
