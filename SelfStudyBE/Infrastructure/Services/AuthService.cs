using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;
 
    public AuthService(
        UserManager<AppUser> userManager,
        IJwtService jwtService,
        AppDbContext db,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _db = db;
        _jwtSettings = jwtSettings.Value;
    }
 
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            throw new InvalidOperationException("Email đã được sử dụng.");
 
        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };
 
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }
 
        // Gán role mặc định
        await _userManager.AddToRoleAsync(user, "User");
 
        return await BuildAuthResultAsync(user);
    }
 
    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
 
        var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!valid)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
 
        return await BuildAuthResultAsync(user);
    }
 
    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked)
            ?? throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
 
        if (stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token đã hết hạn.");
 
        var user = await _userManager.FindByIdAsync(stored.UserId)
            ?? throw new UnauthorizedAccessException("Người dùng không tồn tại.");
 
        // Thu hồi token cũ (rotation)
        stored.IsRevoked = true;
        await _db.SaveChangesAsync();
 
        return await BuildAuthResultAsync(user);
    }
 
    public async Task RevokeTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);
 
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }
 
    public async Task<UserInfoDto> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");
 
        var roles = await _userManager.GetRolesAsync(user);
 
        return new UserInfoDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            Roles = roles
        };
    }
 
    // ── Private helpers ──────────────────────────────────────────────────
 
    private async Task<AuthResultDto> BuildAuthResultAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();
 
        // Lưu refresh token vào DB
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });
 
        await _db.SaveChangesAsync();
 
        return new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            User = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                Roles = roles
            }
        };
    }
}