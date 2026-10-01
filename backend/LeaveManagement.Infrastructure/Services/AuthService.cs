using LeaveManagement.Core.DTOs.Auth;
using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Exceptions;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILeaveService _leaveService;

    public AuthService(AppDbContext context, IJwtTokenGenerator jwtTokenGenerator, ILeaveService leaveService)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _leaveService = leaveService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (existingUser)
        {
            throw new ValidationAppException("User with this email already exists");
        }

        if (!Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
        {
            parsedRole = UserRole.Employee;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email.ToLower(),
            PasswordHash = passwordHash,
            Role = parsedRole,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Seed leave balance for current year
        await _leaveService.EnsureUserBalancesForYearAsync(user.Id, DateTime.UtcNow.Year);

        var token = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var department = user.DepartmentId.HasValue ? await _context.Departments.FindAsync(user.DepartmentId.Value) : null;
        var manager = user.ManagerId.HasValue ? await _context.Users.FindAsync(user.ManagerId.Value) : null;

        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = department?.Name,
            ManagerId = user.ManagerId,
            ManagerName = manager?.Name,
            Token = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Your account is deactivated. Please contact an administrator.");
        }

        // Ensure balances exist for current year
        await _leaveService.EnsureUserBalancesForYearAsync(user.Id, DateTime.UtcNow.Year);

        var token = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            ManagerId = user.ManagerId,
            ManagerName = user.Manager?.Name,
            Token = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.Department)
            .Include(t => t.User)
            .ThenInclude(u => u!.Manager)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (tokenEntity == null || !tokenEntity.IsActive)
        {
            throw new UnauthorizedException("Invalid or expired refresh token");
        }

        var user = tokenEntity.User;
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User not found or inactive");
        }

        tokenEntity.IsRevoked = true;
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id);
        tokenEntity.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            ManagerId = user.ManagerId,
            ManagerName = user.Manager?.Name,
            Token = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (tokenEntity == null || !tokenEntity.IsActive)
        {
            return false;
        }

        tokenEntity.IsRevoked = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            ManagerId = user.ManagerId,
            ManagerName = user.Manager?.Name,
            IsActive = user.IsActive
        };
    }

    public async Task<IReadOnlyList<UserDto>> GetTeamMembersAsync(int managerId)
    {
        var users = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Where(u => u.ManagerId == managerId)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.ToString(),
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department?.Name,
            ManagerId = u.ManagerId,
            ManagerName = u.Manager?.Name,
            IsActive = u.IsActive
        }).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.ToString(),
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department?.Name,
            ManagerId = u.ManagerId,
            ManagerName = u.Manager?.Name,
            IsActive = u.IsActive
        }).ToList();
    }
}
