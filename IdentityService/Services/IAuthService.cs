using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Common.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<bool> ValidateTokenAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, AppDbContext context)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new Common.Exceptions.UnauthorizedException("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Driver";

        var token = JwtHelper.GenerateToken(
            user.Id,
            user.Email,
            role,
            _configuration["Jwt:Secret"],
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"]
        );

        return new LoginResponse
        {
            Token = token,
            RefreshToken = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            UserId = user.Id,
            Email = user.Email,
            Role = role
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Common.Exceptions.CustomException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);

        var token = JwtHelper.GenerateToken(
            user.Id,
            user.Email,
            request.Role,
            _configuration["Jwt:Secret"],
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"]
        );

        return new LoginResponse
        {
            Token = token,
            RefreshToken = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            UserId = user.Id,
            Email = user.Email,
            Role = request.Role
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        // Simple token validation - in production, use proper JWT validation
        return !string.IsNullOrEmpty(token) && token.Length > 10;
    }
}