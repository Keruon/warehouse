using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Areas;

public sealed record CreateAreaCommand(CreateAreaRequest Request) : IRequest<AreaResponse>;

public sealed class CreateAreaCommandHandler : IRequestHandler<CreateAreaCommand, AreaResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateAreaCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AreaResponse> Handle(CreateAreaCommand command, CancellationToken cancellationToken)
    {
        var duplicate = await _unitOfWork.WarehouseAreas.ExistsAsync(
            x => x.Name == command.Request.Name
              && x.Code == command.Request.Code
              && x.FloorLevel == command.Request.FloorLevel,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("An area with the same name, code, and floor level already exists.");
        }

        var actorId = GetActorId();
        var area = _mapper.Map<WarehouseArea>(command.Request);
        area.Id = Guid.NewGuid();
        area.CreatedAt = DateTime.UtcNow;
        area.ModifiedAt = DateTime.UtcNow;
        area.CreatedBy = actorId;
        area.ModifiedBy = actorId;

        await _unitOfWork.WarehouseAreas.AddAsync(area, cancellationToken);
        await WriteAuditLogAsync(actorId, "CREATE", "WarehouseArea", area.Id, null, command.Request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<AreaResponse>(area);
        response.ShelfCount = 0;
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
