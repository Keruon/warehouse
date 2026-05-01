using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;
using System.Linq;

namespace Storage.Controllers.Inventory;

public sealed record CreateComponentCommand(CreateComponentRequest Request) : IRequest<ComponentResponse>;
public sealed record UpdateComponentCommand(Guid Id, UpdateComponentRequest Request) : IRequest<ComponentResponse>;
public sealed record DeleteComponentCommand(Guid Id) : IRequest<bool>;
public sealed record GetComponentByIdQuery(Guid Id) : IRequest<ComponentResponse?>;
public sealed record SearchComponentsQuery(ComponentSearchRequest Search) : IRequest<PaginatedResponse<ComponentResponse>>;

public sealed class CreateComponentCommandHandler : IRequestHandler<CreateComponentCommand, ComponentResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateComponentCommandHandler(ApplicationDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ComponentResponse> Handle(CreateComponentCommand command, CancellationToken cancellationToken)
    {
        var type = await _context.ComponentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.Request.ComponentTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Component type was not found.");

        Supplier? supplier = null;
        if (command.Request.SupplierId.HasValue)
        {
            supplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.Request.SupplierId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Supplier was not found.");
        }

        var entity = _mapper.Map<Component>(command.Request);
        entity.Id = Guid.NewGuid();
        entity.ComponentTypeName = BuildComponentTypeName(type);
        entity.SupplierCode = supplier?.Code;
        entity.SupplierName = supplier?.Name;
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.CreatedBy = GetActorId();
        entity.ModifiedBy = GetActorId();

        await _context.Components.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(entity, cancellationToken);
    }

    private Guid GetActorId()
    {
        var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out var parsed) ? parsed : Guid.Empty;
    }

    private async Task<ComponentResponse> ToResponseAsync(Component entity, CancellationToken cancellationToken)
    {
        var componentType = await _context.ComponentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.ComponentTypeId, cancellationToken);
        var response = _mapper.Map<ComponentResponse>(entity);
        response.ComponentTypeName = componentType is null ? null : BuildComponentTypeName(componentType);
        response.CategoryId = componentType?.CategoryId ?? Guid.Empty;
        return response;
    }

    private static string BuildComponentTypeName(ComponentType type)
    {
        return string.Join(" ", new[] { type.Kind, type.Value, type.Footprint }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

public sealed class UpdateComponentCommandHandler : IRequestHandler<UpdateComponentCommand, ComponentResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateComponentCommandHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ComponentResponse> Handle(UpdateComponentCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.Components.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Component was not found.");

        var type = await _context.ComponentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.Request.ComponentTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Component type was not found.");

        Supplier? supplier = null;
        if (command.Request.SupplierId.HasValue)
        {
            supplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.Request.SupplierId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Supplier was not found.");
        }

        _mapper.Map(command.Request, entity);
        entity.ComponentTypeName = BuildComponentTypeName(type);
        entity.SupplierCode = supplier?.Code;
        entity.SupplierName = supplier?.Name;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<ComponentResponse>(entity);
        response.ComponentTypeName = BuildComponentTypeName(type);
        response.CategoryId = type.CategoryId;
        return response;
    }

    private static string BuildComponentTypeName(ComponentType type)
    {
        return string.Join(" ", new[] { type.Kind, type.Value, type.Footprint }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

public sealed class DeleteComponentCommandHandler : IRequestHandler<DeleteComponentCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public DeleteComponentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteComponentCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.Components.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Component was not found.");

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed class GetComponentByIdQueryHandler : IRequestHandler<GetComponentByIdQuery, ComponentResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetComponentByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ComponentResponse?> Handle(GetComponentByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _context.Components.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var type = await _context.ComponentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.ComponentTypeId, cancellationToken);
        var response = _mapper.Map<ComponentResponse>(entity);
        response.ComponentTypeName = type is null ? null : BuildComponentTypeName(type);
        response.CategoryId = type?.CategoryId ?? Guid.Empty;
        return response;
    }

    private static string BuildComponentTypeName(ComponentType type)
    {
        return string.Join(" ", new[] { type.Kind, type.Value, type.Footprint }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

public sealed class SearchComponentsQueryHandler : IRequestHandler<SearchComponentsQuery, PaginatedResponse<ComponentResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public SearchComponentsQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<ComponentResponse>> Handle(SearchComponentsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Search.Page <= 0 ? 1 : query.Search.Page;
        var pageSize = query.Search.PageSize <= 0 ? 20 : Math.Min(query.Search.PageSize, 200);

        var q = _context.Components.AsNoTracking().AsQueryable();

        if (query.Search.TypeId.HasValue)
        {
            q = q.Where(x => x.ComponentTypeId == query.Search.TypeId.Value);
        }

        if (query.Search.SupplierId.HasValue)
        {
            q = q.Where(x => x.SupplierId == query.Search.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search.PartNumber))
        {
            q = q.Where(x => EF.Functions.ILike(x.PartNumber, $"%{query.Search.PartNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Search.Manufacturer))
        {
            q = q.Where(x => x.SupplierName != null && EF.Functions.ILike(x.SupplierName, $"%{query.Search.Manufacturer}%"));
        }

        if (query.Search.CategoryId.HasValue)
        {
            var typeIds = await _context.ComponentTypes.AsNoTracking().Where(x => x.CategoryId == query.Search.CategoryId.Value).Select(x => x.Id).ToListAsync(cancellationToken);
            q = q.Where(x => typeIds.Contains(x.ComponentTypeId));
        }

        q = q.OrderBy(x => x.PartNumber);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var typeIdsForRows = rows.Select(x => x.ComponentTypeId).Distinct().ToList();
        var typesById = await _context.ComponentTypes.AsNoTracking().Where(x => typeIdsForRows.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);

        var responses = _mapper.Map<List<ComponentResponse>>(rows);
        foreach (var item in responses)
        {
            if (typesById.TryGetValue(item.ComponentTypeId, out var type))
            {
                item.ComponentTypeName = BuildComponentTypeName(type);
                item.CategoryId = type.CategoryId;
            }
        }

        return new PaginatedResponse<ComponentResponse>
        {
            Items = responses,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    private static string BuildComponentTypeName(ComponentType type)
    {
        return string.Join(" ", new[] { type.Kind, type.Value, type.Footprint }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
