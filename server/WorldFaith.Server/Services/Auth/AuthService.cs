using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WorldFaith.Server.Models.Auth;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts.Auth;

namespace WorldFaith.Server.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string deviceInfo = "");
    Task<AuthResponse> LoginAsync(LoginRequest request, string deviceInfo = "");
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string playerId, string refreshToken);
    Task<PlayerProfileDto?> GetProfileAsync(string playerId);
    Task<bool> SeedAdminAsync(string email, string password);
}

public class AuthService : IAuthService
{
    private readonly IPlayerRepository _playerRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    private string JwtSecret => _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret chưa cấu hình");
    private string JwtIssuer => _config["Jwt:Issuer"] ?? "WorldFaith";
    private string JwtAudience => _config["Jwt:Audience"] ?? "WorldFaithPlayers";
    private int AccessTokenMinutes => int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60");
    private int RefreshTokenDays => int.Parse(_config["Jwt:RefreshTokenDays"] ?? "30");

    public AuthService(
        IPlayerRepository playerRepo,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _playerRepo = playerRepo;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string deviceInfo = "")
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return Fail("Username phải có ít nhất 3 ký tự");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return Fail("Password phải có ít nhất 6 ký tự");

        if (!request.Email.Contains('@'))
            return Fail("Email not hợp lệ");

        // Check trùng
        if (await _playerRepo.GetByEmailAsync(request.Email) != null)
            return Fail("Email has was sử dụng");

        if (await _playerRepo.GetByUsernameAsync(request.Username) != null)
            return Fail("Username has was sử dụng");

        var player = new PlayerDocument
        {
            Username = request.Username,
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? request.Username : request.DisplayName,
            PasswordHash = HashPassword(request.Password)
        };

        await _playerRepo.CreateAsync(player);
        _logger.LogInformation("Player mới đăng ký: {Username} ({PlayerId})", player.Username, player.Id);

        return await GenerateTokensAsync(player, deviceInfo);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string deviceInfo = "")
    {
        var player = await _playerRepo.GetByEmailAsync(request.Email);

        if (player == null || !VerifyPassword(request.Password, player.PasswordHash))
            return Fail("Email hoặc password not đúng");

        if (!player.IsActive)
            return Fail("Tài khoản has was khóa");

        await _playerRepo.UpdateLastLoginAsync(player.Id);
        _logger.LogInformation("Player đăng nhập: {Username}", player.Username);

        return await GenerateTokensAsync(player, deviceInfo);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var player = await _playerRepo.GetByRefreshTokenAsync(refreshToken);
        if (player == null)
            return Fail("Refresh token not hợp lệ hoặc has hết hạn");

        // Revoke token cũ (rotation)
        await _playerRepo.RevokeRefreshTokenAsync(player.Id, refreshToken);

        return await GenerateTokensAsync(player, "refresh");
    }

    public async Task<bool> LogoutAsync(string playerId, string refreshToken)
    {
        await _playerRepo.RevokeRefreshTokenAsync(playerId, refreshToken);
        return true;
    }

    public async Task<PlayerProfileDto?> GetProfileAsync(string playerId)
    {
        var player = await _playerRepo.GetByIdAsync(playerId);
        return player == null ? null : MapToDto(player);
    }

    public async Task<bool> SeedAdminAsync(string email, string password)
    {
        var existing = await _playerRepo.GetByEmailAsync(email);
        if (existing != null)
        {
            if (!existing.IsAdmin)
            {
                await _playerRepo.SetAdminAsync(existing.Id, true);
                _logger.LogInformation("Promoted existing account to admin: {Email}", email);
            }
            return true;
        }

        var admin = new PlayerDocument
        {
            Username = "admin",
            Email = email.ToLower(),
            DisplayName = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            IsAdmin = true,
            IsActive = true
        };
        await _playerRepo.CreateAsync(admin);
        _logger.LogInformation("Admin account created: {Email}", email);
        return true;
    }

    // ─── Token Generation ────────────────────────────────────

    private async Task<AuthResponse> GenerateTokensAsync(PlayerDocument player, string deviceInfo)
    {
        var accessToken = GenerateAccessToken(player);
        var refreshToken = GenerateRefreshToken();

        await _playerRepo.AddRefreshTokenAsync(player.Id, new RefreshTokenEntry
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            DeviceInfo = deviceInfo
        });

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(AccessTokenMinutes).ToUnixTimeSeconds();

        return new AuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Player = MapToDto(player)
        };
    }

    private string GenerateAccessToken(PlayerDocument player)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, player.Id),
            new Claim(JwtRegisteredClaimNames.Email, player.Email),
            new Claim("username", player.Username),
            new Claim("displayName", player.DisplayName),
            new Claim("role", player.IsAdmin ? "Admin" : "Player"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    private static bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    private static AuthResponse Fail(string error) => new() { Success = false, Error = error };

    private static PlayerProfileDto MapToDto(PlayerDocument p) => new()
    {
        Id = p.Id,
        Username = p.Username,
        DisplayName = p.DisplayName,
        Email = p.Email,
        Level = p.Level,
        TotalWins = p.TotalWins,
        TotalGames = p.TotalGames,
        CreatedAt = new DateTimeOffset(p.CreatedAt).ToUnixTimeSeconds(),
        LastLoginAt = new DateTimeOffset(p.LastLoginAt).ToUnixTimeSeconds()
    };
}
