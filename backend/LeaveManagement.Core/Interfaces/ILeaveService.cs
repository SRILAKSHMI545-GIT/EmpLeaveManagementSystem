using LeaveManagement.Core.DTOs.Leave;

namespace LeaveManagement.Core.Interfaces;

public interface ILeaveService
{
    // Leave Balances
    Task<IReadOnlyList<LeaveBalanceDto>> GetUserBalancesAsync(int userId, int? year = null);
    Task<IReadOnlyList<LeaveBalanceDto>> GetTeamBalancesAsync(int managerId, int? year = null);
    Task<IReadOnlyList<LeaveBalanceDto>> GetAllBalancesAsync(int? year = null);
    Task EnsureUserBalancesForYearAsync(int userId, int year);

    // Leave Types & Departments
    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync();
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync();

    // Leave Requests
    Task<IReadOnlyList<LeaveRequestDto>> GetMyLeaveRequestsAsync(int userId);
    Task<IReadOnlyList<LeaveRequestDto>> GetTeamLeaveRequestsAsync(int managerId, string? status = null);
    Task<IReadOnlyList<LeaveRequestDto>> GetAllLeaveRequestsAsync(string? status = null);
    Task<LeaveRequestDto> GetLeaveRequestByIdAsync(int id);
    Task<LeaveRequestDto> CreateLeaveRequestAsync(int userId, CreateLeaveRequestDto dto);
    Task<LeaveRequestDto> ApproveLeaveRequestAsync(int requestId, int managerId, LeaveDecisionDto dto);
    Task<LeaveRequestDto> RejectLeaveRequestAsync(int requestId, int managerId, LeaveDecisionDto dto);
    Task<LeaveRequestDto> CancelLeaveRequestAsync(int requestId, int userId);
    Task<IReadOnlyList<TeamCalendarEventDto>> GetTeamCalendarAsync(int managerId, DateTime? start = null, DateTime? end = null);
}
