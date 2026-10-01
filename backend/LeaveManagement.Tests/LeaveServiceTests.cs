using LeaveManagement.Core.DTOs.Leave;
using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Exceptions;
using LeaveManagement.Infrastructure.Data;
using LeaveManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeaveManagement.Tests;

public class LeaveServiceTests
{
    private readonly AppDbContext _context;
    private readonly LeaveService _leaveService;
    private readonly int _year;

    public LeaveServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _leaveService = new LeaveService(_context);
        _year = DateTime.UtcNow.Year;
    }

    private async Task<(User manager, User employee, LeaveType annualLeave)> SetupInitialDataAsync()
    {
        var manager = new User
        {
            Name = "Manager Bob",
            Email = "manager@example.com",
            PasswordHash = "hash",
            Role = UserRole.Manager,
            IsActive = true
        };
        _context.Users.Add(manager);
        await _context.SaveChangesAsync();

        var employee = new User
        {
            Name = "Employee Alice",
            Email = "employee@example.com",
            PasswordHash = "hash",
            Role = UserRole.Employee,
            ManagerId = manager.Id,
            IsActive = true
        };
        _context.Users.Add(employee);

        var leaveType = new LeaveType
        {
            Name = "Annual Leave",
            DefaultAnnualQuota = 20,
            RequiresApproval = true
        };
        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();

        var balance = new LeaveBalance
        {
            UserId = employee.Id,
            LeaveTypeId = leaveType.Id,
            Year = _year,
            TotalDays = 20,
            UsedDays = 0,
            LastUpdatedAt = DateTime.UtcNow
        };
        _context.LeaveBalances.Add(balance);
        await _context.SaveChangesAsync();

        return (manager, employee, leaveType);
    }

    [Fact]
    public async Task CreateLeaveRequestAsync_ValidInput_CreatesRequest()
    {
        // Arrange
        var (_, employee, leaveType) = await SetupInitialDataAsync();
        var nextMonday = DateTime.UtcNow.Date.AddDays(7);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        var nextFriday = nextMonday.AddDays(4);

        var dto = new CreateLeaveRequestDto
        {
            LeaveTypeId = leaveType.Id,
            StartDate = nextMonday,
            EndDate = nextFriday,
            Reason = "Trip with family"
        };

        // Act
        var result = await _leaveService.CreateLeaveRequestAsync(employee.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.DaysRequested);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(employee.Id, result.UserId);
    }

    [Fact]
    public async Task CreateLeaveRequestAsync_ExceedsBalance_ThrowsValidationAppException()
    {
        // Arrange
        var (_, employee, leaveType) = await SetupInitialDataAsync();

        // Reduce balance
        var balance = await _context.LeaveBalances.FirstAsync(b => b.UserId == employee.Id);
        balance.UsedDays = 18; // Only 2 days remaining
        await _context.SaveChangesAsync();

        var nextMonday = DateTime.UtcNow.Date.AddDays(7);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        var nextFriday = nextMonday.AddDays(4); // 5 working days

        var dto = new CreateLeaveRequestDto
        {
            LeaveTypeId = leaveType.Id,
            StartDate = nextMonday,
            EndDate = nextFriday,
            Reason = "Too many days"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(() => _leaveService.CreateLeaveRequestAsync(employee.Id, dto));
        Assert.Contains("Insufficient leave balance", ex.Message);
    }

    [Fact]
    public async Task CreateLeaveRequestAsync_OverlappingDates_ThrowsValidationAppException()
    {
        // Arrange
        var (_, employee, leaveType) = await SetupInitialDataAsync();
        var baseDate = DateTime.UtcNow.Date.AddDays(10);

        // Create existing approved leave
        _context.LeaveRequests.Add(new LeaveRequest
        {
            UserId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = baseDate,
            EndDate = baseDate.AddDays(3),
            DaysRequested = 3,
            Reason = "Existing",
            Status = LeaveRequestStatus.Approved
        });
        await _context.SaveChangesAsync();

        var dto = new CreateLeaveRequestDto
        {
            LeaveTypeId = leaveType.Id,
            StartDate = baseDate.AddDays(1),
            EndDate = baseDate.AddDays(5),
            Reason = "Overlapping leave"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(() => _leaveService.CreateLeaveRequestAsync(employee.Id, dto));
        Assert.Contains("already have a pending or approved leave request", ex.Message);
    }

    [Fact]
    public async Task ApproveLeaveRequestAsync_ValidManager_DeductsBalanceAndApproves()
    {
        // Arrange
        var (manager, employee, leaveType) = await SetupInitialDataAsync();
        var startDate = DateTime.UtcNow.Date.AddDays(14);
        while (startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday)
        {
            startDate = startDate.AddDays(1);
        }
        var endDate = startDate.AddDays(1);
        if (endDate.DayOfWeek == DayOfWeek.Saturday) endDate = endDate.AddDays(2);

        var request = new LeaveRequest
        {
            UserId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = startDate,
            EndDate = endDate,
            DaysRequested = 2,
            Reason = "Taking off",
            Status = LeaveRequestStatus.Pending
        };
        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        var decisionDto = new LeaveDecisionDto { ManagerComment = "Approved! Have fun." };

        // Act
        var result = await _leaveService.ApproveLeaveRequestAsync(request.Id, manager.Id, decisionDto);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.Equal("Approved! Have fun.", result.ManagerComment);

        var balance = await _context.LeaveBalances.FirstAsync(b => b.UserId == employee.Id && b.LeaveTypeId == leaveType.Id);
        Assert.Equal(2, balance.UsedDays);
        Assert.Equal(18, balance.RemainingDays);
    }

    [Fact]
    public async Task CancelLeaveRequestAsync_ApprovedRequest_RestoresBalance()
    {
        // Arrange
        var (_, employee, leaveType) = await SetupInitialDataAsync();
        var startDate = DateTime.UtcNow.Date.AddDays(14);

        var balance = await _context.LeaveBalances.FirstAsync(b => b.UserId == employee.Id && b.LeaveTypeId == leaveType.Id);
        balance.UsedDays = 3; // Previously deducted
        await _context.SaveChangesAsync();

        var request = new LeaveRequest
        {
            UserId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = startDate,
            EndDate = startDate.AddDays(3),
            DaysRequested = 3,
            Reason = "Need to cancel",
            Status = LeaveRequestStatus.Approved
        };
        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _leaveService.CancelLeaveRequestAsync(request.Id, employee.Id);

        // Assert
        Assert.Equal("Cancelled", result.Status);

        var updatedBalance = await _context.LeaveBalances.FirstAsync(b => b.UserId == employee.Id && b.LeaveTypeId == leaveType.Id);
        Assert.Equal(0, updatedBalance.UsedDays);
        Assert.Equal(20, updatedBalance.RemainingDays);
    }
}
