using LeaveManagement.Core.DTOs.Auth;
using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Exceptions;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Infrastructure.Data;
using LeaveManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LeaveManagement.Tests;

public class AuthServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IJwtTokenGenerator> _jwtMock;
    private readonly Mock<ILeaveService> _leaveServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _jwtMock = new Mock<IJwtTokenGenerator>();
        _leaveServiceMock = new Mock<ILeaveService>();

        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("mock_token_123");
        _jwtMock.Setup(j => j.GenerateRefreshToken(It.IsAny<int>())).Returns(new RefreshToken
        {
            Token = "mock_refresh_token_123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        _authService = new AuthService(_context, _jwtMock.Object, _leaveServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsAuthResponseAndEnsuresBalances()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "Password123!",
            Role = "Employee"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Employee", result.Role);
        Assert.Equal("mock_token_123", result.Token);

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
        Assert.NotNull(savedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", savedUser.PasswordHash));
        _leaveServiceMock.Verify(s => s.EnsureUserBalancesForYearAsync(savedUser.Id, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsValidationAppException()
    {
        // Arrange
        var existingUser = new User
        {
            Name = "Existing",
            Email = "duplicate@example.com",
            PasswordHash = "hash",
            Role = UserRole.Employee
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Name = "New",
            Email = "duplicate@example.com",
            Password = "Password123!",
            Role = "Employee"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(() => _authService.RegisterAsync(registerDto));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = new User
        {
            Name = "Alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Employee,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "alice@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("mock_token_123", result.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            Name = "Alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = UserRole.Employee,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "alice@example.com",
            Password = "WrongPassword"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(loginDto));
    }
}
