using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Helpers.DTOs;

namespace Storage.Controllers.Users;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UserController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> GetUsers([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var usersQuery = _context.Users.AsNoTracking().OrderBy(x => x.Username);

        var total = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResponse<UserResponse>
        {
            Items = _mapper.Map<IReadOnlyList<UserResponse>>(users),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && !string.Equals(currentUserId, id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ErrorResponse { Code = "user_not_found", Message = "User was not found." });
        }

        return Ok(_mapper.Map<UserResponse>(user));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var emailExists = await _context.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
        var usernameExists = await _context.Users.AnyAsync(x => x.Username == request.Username, cancellationToken);

        if (emailExists || usernameExists)
        {
            return Conflict(new ErrorResponse
            {
                Code = "duplicate_user",
                Message = "A user with the same username or email already exists."
            });
        }

        var user = _mapper.Map<User>(request);
        user.Id = Guid.NewGuid();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
        user.CreatedAt = DateTime.UtcNow;
        user.ModifiedAt = DateTime.UtcNow;
        user.CreatedBy = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId) ? actorId : Guid.Empty;
        user.ModifiedBy = user.CreatedBy;

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, _mapper.Map<UserResponse>(user));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ErrorResponse { Code = "user_not_found", Message = "User was not found." });
        }

        var duplicateEmail = await _context.Users.AnyAsync(x => x.Id != id && x.Email == request.Email, cancellationToken);
        if (duplicateEmail)
        {
            return Conflict(new ErrorResponse { Code = "duplicate_email", Message = "Email is already in use." });
        }

        _mapper.Map(request, user);
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId) ? actorId : Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(_mapper.Map<UserResponse>(user));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new ErrorResponse { Code = "user_not_found", Message = "User was not found." });
        }

        user.IsActive = false;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId) ? actorId : Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
