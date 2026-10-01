using System.Security.Claims;
using LeaveManagement.Core.Entities;

namespace LeaveManagement.Core.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(int userId);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
