using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Items;

public sealed record CreateComponentCategoryCommand(CreateComponentCategoryRequest Request) : IRequest<ComponentCategoryResponse>;
public sealed record UpdateComponentCategoryCommand(Guid Id, UpdateComponentCategoryRequest Request) : IRequest<ComponentCategoryResponse>;
public sealed record DeleteComponentCategoryCommand(Guid Id) : IRequest<bool>;
public sealed record GetComponentCategoryByIdQuery(Guid Id) : IRequest<ComponentCategoryResponse?>;
public sealed record GetComponentCategoriesQuery(PagedQuery PagedQuery, Guid? ParentId, bool? IsActive) : IRequest<PaginatedResponse<ComponentCategoryResponse>>;

public sealed class CreateComponentCategoryCommandHandler : IRequestHandler<CreateComponentCategoryCommand, ComponentCategoryResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateComponentCategoryCommandHandler(ApplicationDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ComponentCategoryResponse> Handle(CreateComponentCategoryCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.ParentId.HasValue)
        {
            var parentExists = await _context.ComponentCategories.AnyAsync(x => x.Id == command.Request.ParentId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new KeyNotFoundException("Parent category was not found.");
            }

            var depth = await GetDepthAsync(command.Request.ParentId.Value, cancellationToken);
            if (depth >= 5)
            {
                throw new InvalidOperationException("Category hierarchy depth cannot exceed 5 levels.");
            }
        }

        var duplicate = await _context.ComponentCategories.AnyAsync(
            x => x.Name == command.Request.Name && x.ParentId == command.Request.ParentId,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("A category with the same name already exists under the selected parent.");
        }

        var entity = _mapper.Map<ComponentCategory>(command.Request);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.CreatedBy = GetActorId();
        entity.ModifiedBy = GetActorId();

        await _context.ComponentCategories.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ComponentCategoryResponse>(entity);
    }

    private async Task<int> GetDepthAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var depth = 1;
        var currentParentId = parentId;

        while (true)
        {
            var parent = await _context.ComponentCategories.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentParentId, cancellationToken);
            if (parent?.ParentId is null)
            {
                break;
            }

            depth++;
            currentParentId = parent.ParentId.Value;
        }

        return depth;
    }

    private Guid GetActorId()
    {
        var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out var parsed) ? parsed : Guid.Empty;
    }
}

public sealed class UpdateComponentCategoryCommandHandler : IRequestHandler<UpdateComponentCategoryCommand, ComponentCategoryResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateComponentCategoryCommandHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ComponentCategoryResponse> Handle(UpdateComponentCategoryCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.ComponentCategories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Category was not found.");

        if (command.Request.ParentId == command.Id)
        {
            throw new InvalidOperationException("A category cannot be its own parent.");
        }

        if (command.Request.ParentId.HasValue)
        {
            var parentExists = await _context.ComponentCategories.AnyAsync(x => x.Id == command.Request.ParentId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new KeyNotFoundException("Parent category was not found.");
            }
        }

        _mapper.Map(command.Request, entity);
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ComponentCategoryResponse>(entity);
    }
}

public sealed class DeleteComponentCategoryCommandHandler : IRequestHandler<DeleteComponentCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public DeleteComponentCategoryCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteComponentCategoryCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.ComponentCategories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Category was not found.");

        var hasChildren = await _context.ComponentCategories.AnyAsync(x => x.ParentId == command.Id, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException("Cannot delete a category that has child categories.");
        }

        var usedByTypes = await _context.ComponentTypes.AnyAsync(x => x.CategoryId == command.Id, cancellationToken);
        if (usedByTypes)
        {
            throw new InvalidOperationException("Cannot delete a category that is used by component types.");
        }

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed class GetComponentCategoryByIdQueryHandler : IRequestHandler<GetComponentCategoryByIdQuery, ComponentCategoryResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetComponentCategoryByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ComponentCategoryResponse?> Handle(GetComponentCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _context.ComponentCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
        return entity is null ? null : _mapper.Map<ComponentCategoryResponse>(entity);
    }
}

public sealed class GetComponentCategoriesQueryHandler : IRequestHandler<GetComponentCategoriesQuery, PaginatedResponse<ComponentCategoryResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetComponentCategoriesQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<ComponentCategoryResponse>> Handle(GetComponentCategoriesQuery query, CancellationToken cancellationToken)
    {
        var page = query.PagedQuery.Page <= 0 ? 1 : query.PagedQuery.Page;
        var pageSize = query.PagedQuery.PageSize <= 0 ? 20 : Math.Min(query.PagedQuery.PageSize, 200);

        var q = query.IsActive.HasValue
            ? _context.ComponentCategories.IgnoreQueryFilters().Where(x => x.IsActive == query.IsActive.Value)
            : _context.ComponentCategories.AsQueryable();

        if (query.ParentId.HasValue)
        {
            q = q.Where(x => x.ParentId == query.ParentId.Value);
        }

        q = q.AsNoTracking().OrderBy(x => x.Name);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResponse<ComponentCategoryResponse>
        {
            Items = _mapper.Map<List<ComponentCategoryResponse>>(rows),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
