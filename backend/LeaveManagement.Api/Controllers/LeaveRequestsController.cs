using System.Security.Claims;
using LeaveManagement.Core.DTOs.Common;
using LeaveManagement.Core.DTOs.Leave;
using LeaveManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveRequestsController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var requests = await _leaveService.GetMyLeaveRequestsAsync(userId);
        return Ok(ApiResponse<IReadOnlyList<LeaveRequestDto>>.Ok(requests));
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamRequests([FromQuery] string? status)
    {
        var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var requests = await _leaveService.GetTeamLeaveRequestsAsync(managerId, status);
        return Ok(ApiResponse<IReadOnlyList<LeaveRequestDto>>.Ok(requests));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status)
    {
        var requests = await _leaveService.GetAllLeaveRequestsAsync(status);
        return Ok(ApiResponse<IReadOnlyList<LeaveRequestDto>>.Ok(requests));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _leaveService.GetLeaveRequestByIdAsync(id);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(request));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var created = await _leaveService.CreateLeaveRequestAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<LeaveRequestDto>.Ok(created, "Leave request submitted successfully"));
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(int id, [FromBody] LeaveDecisionDto dto)
    {
        var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _leaveService.ApproveLeaveRequestAsync(id, managerId, dto);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Leave request approved successfully"));
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(int id, [FromBody] LeaveDecisionDto dto)
    {
        var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _leaveService.RejectLeaveRequestAsync(id, managerId, dto);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Leave request rejected"));
    }

    [HttpPut("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _leaveService.CancelLeaveRequestAsync(id, userId);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(result, "Leave request cancelled"));
    }

    [HttpGet("team/calendar")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TeamCalendarEventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamCalendar([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var events = await _leaveService.GetTeamCalendarAsync(managerId, start, end);
        return Ok(ApiResponse<IReadOnlyList<TeamCalendarEventDto>>.Ok(events));
    }
}
