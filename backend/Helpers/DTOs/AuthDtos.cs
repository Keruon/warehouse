namespace Storage.Helpers.DTOs;

public sealed class LoginRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class LoginResponse
{
    public UserResponse User { get; set; } = new();
    public TokenResponse Tokens { get; set; } = new();
}
