using LeaveManagement.Core.DTOs.Auth;

namespace LeaveManagement.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<UserDto?> GetUserProfileAsync(int userId);
    Task<IReadOnlyList<UserDto>> GetTeamMembersAsync(int managerId);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync();
}
