using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AutoMapper;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Storage.Data;
using Storage.Helpers;
using Storage.Helpers.DTOs;

namespace Storage.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenHelper _jwtTokenHelper;
    private readonly JwtSettings _jwtSettings;
    private readonly IMapper _mapper;

    public AuthService(
        ApplicationDbContext context,
        JwtTokenHelper jwtTokenHelper,
        IOptions<JwtSettings> jwtSettings,
        IMapper mapper)
    {
        _context = context;
        _jwtTokenHelper = jwtTokenHelper;
        _jwtSettings = jwtSettings.Value;
        _mapper = mapper;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = request.UsernameOrEmail.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username.ToLower() == normalized.ToLower() || x.Email.ToLower() == normalized.ToLower(),
                cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.ModifiedAt = DateTime.UtcNow;

        var tokenResponse = await CreateAndStoreTokensAsync(user, request.DeviceFingerprint, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            User = _mapper.Map<UserResponse>(user),
            Tokens = tokenResponse
        };
    }

    public async Task<TokenResponse?> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = _jwtTokenHelper.HashToken(request.RefreshToken);

        var existing = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Token == refreshTokenHash && !x.IsRevoked,
                cancellationToken);

        if (existing is null || existing.User is null)
        {
            return null;
        }

        if (existing.ExpiresAt <= DateTime.UtcNow)
        {
            existing.IsRevoked = true;
            existing.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(existing.DeviceFingerprint) &&
            !string.Equals(existing.DeviceFingerprint, request.DeviceFingerprint, StringComparison.Ordinal))
        {
            return null;
        }

        existing.IsRevoked = true;
        existing.RevokedAt = DateTime.UtcNow;

        var tokenResponse = await CreateAndStoreTokensAsync(existing.User, request.DeviceFingerprint, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tokenResponse;
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = _jwtTokenHelper.HashToken(refreshToken);

        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.Token == refreshTokenHash && !x.IsRevoked,
                cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.IsRevoked = true;
        existing.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            tokenHandler.ValidateToken(token, validationParameters, out _);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task<TokenResponse> CreateAndStoreTokensAsync(User user, string? deviceFingerprint, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenHelper.GenerateAccessToken(user);
        var refreshToken = _jwtTokenHelper.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _jwtTokenHelper.HashToken(refreshToken),
            DeviceFingerprint = deviceFingerprint,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        await _context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }
}
