using System.Security.Claims;
using LeaveManagement.Core.DTOs.Common;
using LeaveManagement.Core.DTOs.Leave;
using LeaveManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveBalancesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBalances([FromQuery] int? year)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var balances = await _leaveService.GetUserBalancesAsync(userId, year);
        return Ok(ApiResponse<IReadOnlyList<LeaveBalanceDto>>.Ok(balances));
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamBalances([FromQuery] int? year)
    {
        var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var balances = await _leaveService.GetTeamBalancesAsync(managerId, year);
        return Ok(ApiResponse<IReadOnlyList<LeaveBalanceDto>>.Ok(balances));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBalances([FromQuery] int? year)
    {
        var balances = await _leaveService.GetAllBalancesAsync(year);
        return Ok(ApiResponse<IReadOnlyList<LeaveBalanceDto>>.Ok(balances));
    }
}
