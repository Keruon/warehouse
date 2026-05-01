using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Inventory;

public sealed record CreateSupplierCommand(CreateSupplierRequest Request) : IRequest<SupplierResponse>;
public sealed record UpdateSupplierCommand(Guid Id, UpdateSupplierRequest Request) : IRequest<SupplierResponse>;
public sealed record DeleteSupplierCommand(Guid Id) : IRequest<bool>;
public sealed record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierResponse?>;
public sealed record GetSuppliersQuery(PagedQuery PagedQuery, bool? IsActive) : IRequest<PaginatedResponse<SupplierResponse>>;

public sealed class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateSupplierCommandHandler(ApplicationDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SupplierResponse> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        var duplicate = await _context.Suppliers.AnyAsync(
            x => x.Code == command.Request.Code,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Supplier code must be unique.");
        }

        var entity = _mapper.Map<Supplier>(command.Request);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.CreatedBy = GetActorId();
        entity.ModifiedBy = GetActorId();

        await _context.Suppliers.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupplierResponse>(entity);
    }

    private Guid GetActorId()
    {
        var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out var parsed) ? parsed : Guid.Empty;
    }
}

public sealed class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateSupplierCommandHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<SupplierResponse> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Supplier was not found.");

        var duplicate = await _context.Suppliers.AnyAsync(
            x => x.Id != command.Id && x.Code == command.Request.Code,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Supplier code must be unique.");
        }

        _mapper.Map(command.Request, entity);
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupplierResponse>(entity);
    }
}

public sealed class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public DeleteSupplierCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteSupplierCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Supplier was not found.");

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSupplierByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<SupplierResponse?> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
        return entity is null ? null : _mapper.Map<SupplierResponse>(entity);
    }
}

public sealed class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PaginatedResponse<SupplierResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSuppliersQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<SupplierResponse>> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        var page = query.PagedQuery.Page <= 0 ? 1 : query.PagedQuery.Page;
        var pageSize = query.PagedQuery.PageSize <= 0 ? 20 : Math.Min(query.PagedQuery.PageSize, 200);

        var q = query.IsActive.HasValue
            ? _context.Suppliers.IgnoreQueryFilters().Where(x => x.IsActive == query.IsActive.Value)
            : _context.Suppliers.AsQueryable();

        q = q.AsNoTracking().OrderBy(x => x.Code);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResponse<SupplierResponse>
        {
            Items = _mapper.Map<List<SupplierResponse>>(rows),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
