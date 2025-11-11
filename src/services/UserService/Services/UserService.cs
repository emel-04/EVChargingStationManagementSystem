using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace EVChargingStation.UserService.Services;

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Vehicles)
            .Where(u => u.IsActive)
            .ToListAsync();
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // Check if user already exists
       // var existingUser = await GetUserByEmailAsync(request.Email);
       // if (existingUser != null)
        //{
        //    throw new InvalidOperationException("User with this email already exists");
        //}

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = passwordHash,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create wallet for the user
        var wallet = new Wallet
        {
            UserId = user.Id,
            Balance = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {Email}", user.Email);

        return user;
    }

    public async Task<User> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

        if (!string.IsNullOrEmpty(request.Email))
        {
            // Check if email is already taken by another user
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != id)
            {
                throw new InvalidOperationException("Email is already taken by another user");
            }
            user.Email = request.Email;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {Email}", user.Email);

        return user;
    }

  public async Task<bool> DeleteUserAsync(int id)
{
    var user = await GetUserByIdAsync(id);
    if (user == null)
    {
        return false;
    }

    _context.Users.Remove(user);
    await _context.SaveChangesAsync();

    _logger.LogInformation("User deleted permanently: {Email}", user.Email);

    return true;
}


    public async Task<bool> ActivateUserAsync(int id)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User activated: {Email}", user.Email);

        return true;
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User deactivated: {Email}", user.Email);

        return true;
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _context.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == role && u.IsActive)
            .ToListAsync();
    }
}


