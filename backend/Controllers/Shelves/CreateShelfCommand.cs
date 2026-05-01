using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Shelves;

public sealed record CreateShelfCommand(CreateShelfRequest Request) : IRequest<ShelfResponse>;

public sealed class CreateShelfCommandHandler : IRequestHandler<CreateShelfCommand, ShelfResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateShelfCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ShelfResponse> Handle(CreateShelfCommand command, CancellationToken cancellationToken)
    {
        var areaExists = await _unitOfWork.WarehouseAreas.ExistsAsync(x => x.Id == command.Request.AreaId, cancellationToken);
        if (!areaExists)
        {
            throw new KeyNotFoundException("Area was not found.");
        }

        var duplicate = await _unitOfWork.WarehouseShelves.ExistsAsync(
            x => x.AreaId == command.Request.AreaId && x.Name == command.Request.Name && x.Code == command.Request.Code,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("A shelf with the same name and code already exists in the area.");
        }

        var actorId = GetActorId();
        var shelf = _mapper.Map<WarehouseShelf>(command.Request);
        shelf.Id = Guid.NewGuid();
        shelf.CreatedAt = DateTime.UtcNow;
        shelf.ModifiedAt = DateTime.UtcNow;
        shelf.CreatedBy = actorId;
        shelf.ModifiedBy = actorId;

        await _unitOfWork.WarehouseShelves.AddAsync(shelf, cancellationToken);
        await WriteAuditLogAsync(actorId, "CREATE", "WarehouseShelf", shelf.Id, null, command.Request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<ShelfResponse>(shelf);
        response.LocationCount = 0;
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
