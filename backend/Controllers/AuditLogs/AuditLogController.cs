using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.AuditLogs;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AuditLogController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> GetAuditLogs([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var logsQuery = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logsQuery = logsQuery.Where(x => x.EntityType == query.EntityType);
        }

        if (query.UserId.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.UserId == query.UserId.Value);
        }

        if (query.FromUtc.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.Timestamp >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.Timestamp <= query.ToUtc.Value);
        }

        logsQuery = logsQuery.OrderByDescending(x => x.Timestamp);

        var total = await logsQuery.CountAsync(cancellationToken);
        var logs = await logsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResponse<AuditLogResponse>
        {
            Items = logs.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        });
    }

    [HttpGet("{entityType}/{entityId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetAuditTrail(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var logs = await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        return Ok(logs.Select(MapToResponse).ToList());
    }

    private static AuditLogResponse MapToResponse(AuditLog log)
    {
        return new AuditLogResponse
        {
            Id = log.Id,
            UserId = log.UserId,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Timestamp = log.Timestamp
        };
    }
}
