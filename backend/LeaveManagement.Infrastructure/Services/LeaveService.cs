using LeaveManagement.Core.DTOs.Leave;
using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using LeaveManagement.Core.Exceptions;
using LeaveManagement.Core.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Services;

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _context;

    public LeaveService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetUserBalancesAsync(int userId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        await EnsureUserBalancesForYearAsync(userId, targetYear);

        var balances = await _context.LeaveBalances
            .Include(b => b.User)
            .Include(b => b.LeaveType)
            .Where(b => b.UserId == userId && b.Year == targetYear)
            .ToListAsync();

        return balances.Select(MapToBalanceDto).ToList();
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetTeamBalancesAsync(int managerId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var directReportIds = await _context.Users
            .Where(u => u.ManagerId == managerId)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var empId in directReportIds)
        {
            await EnsureUserBalancesForYearAsync(empId, targetYear);
        }

        var balances = await _context.LeaveBalances
            .Include(b => b.User)
            .Include(b => b.LeaveType)
            .Where(b => directReportIds.Contains(b.UserId) && b.Year == targetYear)
            .OrderBy(b => b.User!.Name)
            .ThenBy(b => b.LeaveType!.Name)
            .ToListAsync();

        return balances.Select(MapToBalanceDto).ToList();
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetAllBalancesAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var balances = await _context.LeaveBalances
            .Include(b => b.User)
            .Include(b => b.LeaveType)
            .Where(b => b.Year == targetYear)
            .OrderBy(b => b.User!.Name)
            .ToListAsync();

        return balances.Select(MapToBalanceDto).ToList();
    }

    public async Task EnsureUserBalancesForYearAsync(int userId, int year)
    {
        var leaveTypes = await _context.LeaveTypes.ToListAsync();
        var existingBalances = await _context.LeaveBalances
            .Where(b => b.UserId == userId && b.Year == year)
            .ToListAsync();

        foreach (var type in leaveTypes)
        {
            if (!existingBalances.Any(b => b.LeaveTypeId == type.Id))
            {
                _context.LeaveBalances.Add(new LeaveBalance
                {
                    UserId = userId,
                    LeaveTypeId = type.Id,
                    Year = year,
                    TotalDays = type.DefaultAnnualQuota,
                    UsedDays = 0,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync()
    {
        var types = await _context.LeaveTypes.ToListAsync();
        return types.Select(t => new LeaveTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            DefaultAnnualQuota = t.DefaultAnnualQuota,
            RequiresApproval = t.RequiresApproval
        }).ToList();
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync()
    {
        var depts = await _context.Departments.ToListAsync();
        return depts.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description
        }).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyLeaveRequestsAsync(int userId)
    {
        var requests = await _context.LeaveRequests
            .Include(r => r.User)
                .ThenInclude(u => u!.Department)
            .Include(r => r.LeaveType)
            .Include(r => r.DecidedBy)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToRequestDto).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetTeamLeaveRequestsAsync(int managerId, string? status = null)
    {
        var directReportIds = await _context.Users
            .Where(u => u.ManagerId == managerId)
            .Select(u => u.Id)
            .ToListAsync();

        var query = _context.LeaveRequests
            .Include(r => r.User)
                .ThenInclude(u => u!.Department)
            .Include(r => r.LeaveType)
            .Include(r => r.DecidedBy)
            .Where(r => directReportIds.Contains(r.UserId));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return requests.Select(MapToRequestDto).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllLeaveRequestsAsync(string? status = null)
    {
        var query = _context.LeaveRequests
            .Include(r => r.User)
                .ThenInclude(u => u!.Department)
            .Include(r => r.LeaveType)
            .Include(r => r.DecidedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return requests.Select(MapToRequestDto).ToList();
    }

    public async Task<LeaveRequestDto> GetLeaveRequestByIdAsync(int id)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.User)
                .ThenInclude(u => u!.Department)
            .Include(r => r.LeaveType)
            .Include(r => r.DecidedBy)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            throw new NotFoundException($"Leave request with ID {id} not found.");
        }

        return MapToRequestDto(request);
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(int userId, CreateLeaveRequestDto dto)
    {
        var startDate = dto.StartDate.Date;
        var endDate = dto.EndDate.Date;

        if (endDate < startDate)
        {
            throw new ValidationAppException("End date cannot be earlier than start date.");
        }

        // Calculate business days (excluding weekends)
        decimal requestedDays = CalculateWorkingDays(startDate, endDate);
        if (requestedDays <= 0)
        {
            throw new ValidationAppException("Selected date range has no working days (e.g. only weekends).");
        }

        var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (leaveType == null)
        {
            throw new NotFoundException($"Leave type with ID {dto.LeaveTypeId} not found.");
        }

        // Check for overlapping requests for this user
        var hasOverlap = await _context.LeaveRequests.AnyAsync(r =>
            r.UserId == userId &&
            (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved) &&
            startDate <= r.EndDate.Date &&
            endDate >= r.StartDate.Date
        );

        if (hasOverlap)
        {
            throw new ValidationAppException("You already have a pending or approved leave request during this date range.");
        }

        // Check leave balance for current year
        var year = startDate.Year;
        await EnsureUserBalancesForYearAsync(userId, year);

        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == dto.LeaveTypeId && b.Year == year);

        if (balance == null)
        {
            throw new ValidationAppException("No leave balance record found for this leave type and year.");
        }

        if (balance.RemainingDays < requestedDays)
        {
            throw new ValidationAppException($"Insufficient leave balance. You have {balance.RemainingDays} days remaining, but requested {requestedDays} days.");
        }

        var request = new LeaveRequest
        {
            UserId = userId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            DaysRequested = requestedDays,
            Reason = dto.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        return await GetLeaveRequestByIdAsync(request.Id);
    }

    public async Task<LeaveRequestDto> ApproveLeaveRequestAsync(int requestId, int managerId, LeaveDecisionDto dto)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new NotFoundException($"Leave request with ID {requestId} not found.");
        }

        if (request.Status != LeaveRequestStatus.Pending)
        {
            throw new ValidationAppException($"Cannot approve request with status '{request.Status}'. Only Pending requests can be approved.");
        }

        // Validate manager authority: either direct manager or admin
        var managerUser = await _context.Users.FindAsync(managerId);
        if (managerUser == null)
        {
            throw new UnauthorizedException("User not recognized.");
        }

        if (managerUser.Role != UserRole.Admin && request.User?.ManagerId != managerId)
        {
            throw new ForbiddenException("You are not authorized to approve requests for this employee.");
        }

        var year = request.StartDate.Year;
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.LeaveTypeId == request.LeaveTypeId && b.Year == year);

        if (balance == null)
        {
            throw new ValidationAppException("Leave balance record not found.");
        }

        if (balance.RemainingDays < request.DaysRequested)
        {
            throw new ValidationAppException($"Employee has insufficient leave balance ({balance.RemainingDays} days remaining) to approve {request.DaysRequested} days.");
        }

        // Deduct balance
        balance.UsedDays += request.DaysRequested;
        balance.LastUpdatedAt = DateTime.UtcNow;

        // Update request status
        request.Status = LeaveRequestStatus.Approved;
        request.DecidedById = managerId;
        request.ManagerComment = dto.ManagerComment;
        request.DecidedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetLeaveRequestByIdAsync(requestId);
    }

    public async Task<LeaveRequestDto> RejectLeaveRequestAsync(int requestId, int managerId, LeaveDecisionDto dto)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new NotFoundException($"Leave request with ID {requestId} not found.");
        }

        if (request.Status != LeaveRequestStatus.Pending)
        {
            throw new ValidationAppException($"Cannot reject request with status '{request.Status}'. Only Pending requests can be rejected.");
        }

        var managerUser = await _context.Users.FindAsync(managerId);
        if (managerUser == null)
        {
            throw new UnauthorizedException("User not recognized.");
        }

        if (managerUser.Role != UserRole.Admin && request.User?.ManagerId != managerId)
        {
            throw new ForbiddenException("You are not authorized to reject requests for this employee.");
        }

        request.Status = LeaveRequestStatus.Rejected;
        request.DecidedById = managerId;
        request.ManagerComment = dto.ManagerComment;
        request.DecidedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetLeaveRequestByIdAsync(requestId);
    }

    public async Task<LeaveRequestDto> CancelLeaveRequestAsync(int requestId, int userId)
    {
        var request = await _context.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new NotFoundException($"Leave request with ID {requestId} not found.");
        }

        if (request.UserId != userId)
        {
            throw new ForbiddenException("You cannot cancel another employee's leave request.");
        }

        if (request.Status == LeaveRequestStatus.Cancelled || request.Status == LeaveRequestStatus.Rejected)
        {
            throw new ValidationAppException($"Request is already {request.Status}.");
        }

        // If previously approved, restore the used balance
        if (request.Status == LeaveRequestStatus.Approved)
        {
            var year = request.StartDate.Year;
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.LeaveTypeId == request.LeaveTypeId && b.Year == year);

            if (balance != null)
            {
                balance.UsedDays = Math.Max(0, balance.UsedDays - request.DaysRequested);
                balance.LastUpdatedAt = DateTime.UtcNow;
            }
        }

        request.Status = LeaveRequestStatus.Cancelled;
        await _context.SaveChangesAsync();

        return await GetLeaveRequestByIdAsync(requestId);
    }

    public async Task<IReadOnlyList<TeamCalendarEventDto>> GetTeamCalendarAsync(int managerId, DateTime? start = null, DateTime? end = null)
    {
        var startDate = start ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = end ?? DateTime.UtcNow.AddMonths(3);

        var directReportIds = await _context.Users
            .Where(u => u.ManagerId == managerId)
            .Select(u => u.Id)
            .ToListAsync();

        var query = _context.LeaveRequests
            .Include(r => r.User)
                .ThenInclude(u => u!.Department)
            .Include(r => r.LeaveType)
            .Where(r => (directReportIds.Contains(r.UserId) || r.UserId == managerId) &&
                        (r.Status == LeaveRequestStatus.Approved || r.Status == LeaveRequestStatus.Pending) &&
                        r.EndDate >= startDate && r.StartDate <= endDate);

        var requests = await query.ToListAsync();

        return requests.Select(r => new TeamCalendarEventDto
        {
            RequestId = r.Id,
            UserId = r.UserId,
            UserName = r.User?.Name ?? "Unknown",
            DepartmentName = r.User?.Department?.Name ?? "N/A",
            LeaveTypeName = r.LeaveType?.Name ?? "General Leave",
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            DaysRequested = r.DaysRequested,
            Status = r.Status.ToString()
        }).ToList();
    }

    public static decimal CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        decimal count = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }
        }
        return count;
    }

    private static LeaveBalanceDto MapToBalanceDto(LeaveBalance b)
    {
        return new LeaveBalanceDto
        {
            Id = b.Id,
            UserId = b.UserId,
            UserName = b.User?.Name ?? "",
            UserEmail = b.User?.Email ?? "",
            LeaveTypeId = b.LeaveTypeId,
            LeaveTypeName = b.LeaveType?.Name ?? "",
            Year = b.Year,
            TotalDays = b.TotalDays,
            UsedDays = b.UsedDays,
            RemainingDays = b.RemainingDays,
            LastUpdatedAt = b.LastUpdatedAt
        };
    }

    private static LeaveRequestDto MapToRequestDto(LeaveRequest r)
    {
        return new LeaveRequestDto
        {
            Id = r.Id,
            UserId = r.UserId,
            UserName = r.User?.Name ?? "",
            UserEmail = r.User?.Email ?? "",
            DepartmentId = r.User?.DepartmentId,
            DepartmentName = r.User?.Department?.Name,
            LeaveTypeId = r.LeaveTypeId,
            LeaveTypeName = r.LeaveType?.Name ?? "",
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            DaysRequested = r.DaysRequested,
            Reason = r.Reason,
            Status = r.Status.ToString(),
            DecidedById = r.DecidedById,
            DecidedByName = r.DecidedBy?.Name,
            ManagerComment = r.ManagerComment,
            CreatedAt = r.CreatedAt,
            DecidedAt = r.DecidedAt
        };
    }
}
