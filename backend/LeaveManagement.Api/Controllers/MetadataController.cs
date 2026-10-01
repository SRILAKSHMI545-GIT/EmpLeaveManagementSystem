using LeaveManagement.Core.DTOs.Common;
using LeaveManagement.Core.DTOs.Leave;
using LeaveManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetadataController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public MetadataController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet("leave-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveTypeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var types = await _leaveService.GetLeaveTypesAsync();
        return Ok(ApiResponse<IReadOnlyList<LeaveTypeDto>>.Ok(types));
    }

    [HttpGet("departments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments()
    {
        var depts = await _leaveService.GetDepartmentsAsync();
        return Ok(ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(depts));
    }
}
