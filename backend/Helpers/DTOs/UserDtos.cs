namespace Storage.Helpers.DTOs;

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public sealed class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? ActiveProjectLocationId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}
