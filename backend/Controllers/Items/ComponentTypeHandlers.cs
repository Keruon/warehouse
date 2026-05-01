using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Items;

public sealed record CreateComponentTypeCommand(CreateComponentTypeRequest Request) : IRequest<ComponentTypeResponse>;
public sealed record UpdateComponentTypeCommand(Guid Id, UpdateComponentTypeRequest Request) : IRequest<ComponentTypeResponse>;
public sealed record DeleteComponentTypeCommand(Guid Id) : IRequest<bool>;
public sealed record GetComponentTypeByIdQuery(Guid Id) : IRequest<ComponentTypeResponse?>;
public sealed record GetComponentTypesQuery(PagedQuery PagedQuery, Guid? CategoryId, string? partNumber, string? manufacturer, string? stockSystemCode, bool? IsActive) : IRequest<PaginatedResponse<ComponentTypeResponse>>;

public sealed class CreateComponentTypeCommandHandler : IRequestHandler<CreateComponentTypeCommand, ComponentTypeResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateComponentTypeCommandHandler(ApplicationDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ComponentTypeResponse> Handle(CreateComponentTypeCommand command, CancellationToken cancellationToken)
    {
        var categoryExists = await _context.ComponentCategories.AnyAsync(x => x.Id == command.Request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new KeyNotFoundException("Component category was not found.");
        }

        var kind = command.Request.Kind.Trim();
        var value = command.Request.Value.Trim();
        var footprint = string.IsNullOrWhiteSpace(command.Request.Footprint) ? null : command.Request.Footprint.Trim();

        var duplicate = await _context.ComponentTypes.AnyAsync(
            x => x.CategoryId == command.Request.CategoryId
                && x.Kind == kind
                && x.Value == value
                && x.Footprint == footprint,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("A component type with the same kind, value, and footprint already exists in this category.");
        }

        var entity = _mapper.Map<ComponentType>(command.Request);
        entity.Id = Guid.NewGuid();
        entity.Kind = kind;
        entity.Value = value;
        entity.Footprint = footprint;
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.CreatedBy = GetActorId();
        entity.ModifiedBy = GetActorId();

        await _context.ComponentTypes.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ComponentTypeResponse>(entity);
    }

    private Guid GetActorId()
    {
        var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out var parsed) ? parsed : Guid.Empty;
    }
}

public sealed class UpdateComponentTypeCommandHandler : IRequestHandler<UpdateComponentTypeCommand, ComponentTypeResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateComponentTypeCommandHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ComponentTypeResponse> Handle(UpdateComponentTypeCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.ComponentTypes.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Component type was not found.");

        var categoryExists = await _context.ComponentCategories.AnyAsync(x => x.Id == command.Request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new KeyNotFoundException("Component category was not found.");
        }

        var kind = command.Request.Kind.Trim();
        var value = command.Request.Value.Trim();
        var footprint = string.IsNullOrWhiteSpace(command.Request.Footprint) ? null : command.Request.Footprint.Trim();

        var duplicate = await _context.ComponentTypes.AnyAsync(
            x => x.Id != command.Id
                && x.CategoryId == command.Request.CategoryId
                && x.Kind == kind
                && x.Value == value
                && x.Footprint == footprint,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("A component type with the same kind, value, and footprint already exists in this category.");
        }

        _mapper.Map(command.Request, entity);
        entity.Kind = kind;
        entity.Value = value;
        entity.Footprint = footprint;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ComponentTypeResponse>(entity);
    }
}

public sealed class DeleteComponentTypeCommandHandler : IRequestHandler<DeleteComponentTypeCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public DeleteComponentTypeCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteComponentTypeCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.ComponentTypes.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Component type was not found.");

        var usedByComponents = await _context.Components.AnyAsync(x => x.ComponentTypeId == command.Id, cancellationToken);
        if (usedByComponents)
        {
            throw new InvalidOperationException("Cannot delete a component type that is used by components.");
        }

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed class GetComponentTypeByIdQueryHandler : IRequestHandler<GetComponentTypeByIdQuery, ComponentTypeResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetComponentTypeByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ComponentTypeResponse?> Handle(GetComponentTypeByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _context.ComponentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
        return entity is null ? null : _mapper.Map<ComponentTypeResponse>(entity);
    }
}

public sealed class GetComponentTypesQueryHandler : IRequestHandler<GetComponentTypesQuery, PaginatedResponse<ComponentTypeResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetComponentTypesQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<ComponentTypeResponse>> Handle(GetComponentTypesQuery query, CancellationToken cancellationToken)
    {
        var page = query.PagedQuery.Page <= 0 ? 1 : query.PagedQuery.Page;
        var pageSize = query.PagedQuery.PageSize <= 0 ? 20 : Math.Min(query.PagedQuery.PageSize, 200);

        var q = query.IsActive.HasValue
            ? _context.ComponentTypes.IgnoreQueryFilters().Where(x => x.IsActive == query.IsActive.Value)
            : _context.ComponentTypes.AsQueryable();

        if (query.CategoryId.HasValue)
        {
            q = q.Where(x => x.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.partNumber))
        {
            var componentTypeIds = await _context.Components.AsNoTracking()
                .Where(x => EF.Functions.ILike(x.PartNumber, $"%{query.partNumber}%"))
                .Select(x => x.ComponentTypeId)
                .Distinct()
                .ToListAsync(cancellationToken);
            q = q.Where(x => componentTypeIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.manufacturer))
        {
            var supplierIds = await _context.Suppliers.AsNoTracking()
                .Where(x => EF.Functions.ILike(x.Name, $"%{query.manufacturer}%"))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var componentTypeIds = await _context.Components.AsNoTracking()
                .Where(x => x.SupplierId.HasValue && supplierIds.Contains(x.SupplierId.Value))
                .Select(x => x.ComponentTypeId)
                .Distinct()
                .ToListAsync(cancellationToken);
            q = q.Where(x => componentTypeIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.stockSystemCode))
        {
            q = q.Where(x =>
                EF.Functions.ILike(x.Kind, $"%{query.stockSystemCode}%")
                || EF.Functions.ILike(x.Value, $"%{query.stockSystemCode}%")
                || (x.Footprint != null && EF.Functions.ILike(x.Footprint, $"%{query.stockSystemCode}%")));
        }

        q = q.AsNoTracking()
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Value)
            .ThenBy(x => x.Footprint);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResponse<ComponentTypeResponse>
        {
            Items = _mapper.Map<List<ComponentTypeResponse>>(rows),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
