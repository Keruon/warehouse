using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Areas;

public sealed record UpdateAreaCommand(Guid AreaId, UpdateAreaRequest Request) : IRequest<AreaResponse>;

public sealed class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand, AreaResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateAreaCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AreaResponse> Handle(UpdateAreaCommand command, CancellationToken cancellationToken)
    {
        var area = await _unitOfWork.WarehouseAreas.GetByIdAsync(command.AreaId, cancellationToken)
            ?? throw new KeyNotFoundException("Area was not found.");

        var duplicate = await _unitOfWork.WarehouseAreas.ExistsAsync(
            x => x.Id != command.AreaId
              && x.Name == command.Request.Name
              && x.Code == command.Request.Code
              && x.FloorLevel == command.Request.FloorLevel,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Another area with the same name, code, and floor level already exists.");
        }

        var oldValues = new
        {
            area.Name,
            area.Code,
            area.ZoneType,
            area.FloorLevel,
            area.Description,
            area.IsActive
        };

        _mapper.Map(command.Request, area);
        area.ModifiedAt = DateTime.UtcNow;
        area.ModifiedBy = GetActorId();

        _unitOfWork.WarehouseAreas.Update(area);
        await WriteAuditLogAsync(area.ModifiedBy, "UPDATE", "WarehouseArea", area.Id, oldValues, command.Request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var shelfCount = (await _unitOfWork.WarehouseShelves.FindAsync(x => x.AreaId == area.Id, cancellationToken)).Count;
        var response = _mapper.Map<AreaResponse>(area);
        response.ShelfCount = shelfCount;
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
