using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;
using Storage.Helpers.DTOs;
using Storage.Services.Projects;

namespace Storage.Controllers.Locations;

public sealed record CreateLocationCommand(CreateLocationRequest Request) : IRequest<LocationResponse>;

public sealed class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, LocationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProjectLocationService _projectLocationService;

    public CreateLocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IProjectLocationService projectLocationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _projectLocationService = projectLocationService;
    }

    public async Task<LocationResponse> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        var shelfId = await _projectLocationService.ResolveShelfIdAsync(command.Request.LocationKind, command.Request.ShelfId, actorId, cancellationToken);

        var shelf = await _unitOfWork.WarehouseShelves.GetByIdAsync(shelfId, cancellationToken)
            ?? throw new KeyNotFoundException("Shelf was not found.");

        if (command.Request.LocationKind == LocationKind.Project)
        {
            await _projectLocationService.EnsureShelfSupportsProjectLocationAsync(shelf.Id, cancellationToken);
        }
        else
        {
            var duplicateBin = await _unitOfWork.WarehouseLocations.ExistsAsync(
                x => x.ShelfId == shelfId && x.BinX == command.Request.BinX && x.BinY == command.Request.BinY,
                cancellationToken);
            if (duplicateBin)
            {
                throw new InvalidOperationException("The bin coordinates are already in use on this shelf.");
            }
        }

        var duplicateCode = await _unitOfWork.WarehouseLocations.ExistsAsync(
            x => x.ShelfId == shelfId && x.Name == command.Request.Name && x.Code == command.Request.Code,
            cancellationToken);
        if (duplicateCode)
        {
            throw new InvalidOperationException("A location with the same name and code already exists on this shelf.");
        }

        var location = _mapper.Map<WarehouseLocation>(command.Request);
        location.Id = Guid.NewGuid();
        location.ShelfId = shelfId;
        location.CreatedAt = DateTime.UtcNow;
        location.ModifiedAt = DateTime.UtcNow;
        location.CreatedBy = actorId;
        location.ModifiedBy = actorId;

        await _unitOfWork.WarehouseLocations.AddAsync(location, cancellationToken);
        await WriteAuditLogAsync(actorId, "CREATE", "WarehouseLocation", location.Id, null, command.Request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<LocationResponse>(location);
        response.AreaId = shelf.AreaId;
        response.CurrentStockQuantity = 0;
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
