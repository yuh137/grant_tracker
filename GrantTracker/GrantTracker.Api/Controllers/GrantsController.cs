using GrantTracker.Api.DTO;
using GrantTracker.Api.Services;
using GrantTracker.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GrantTracker.Api.Controllers;

[ApiController]
[Route("grants")]
public class GrantsController : ControllerBase
{
    private readonly GrantService _grantService;

    public GrantsController(GrantService grantService)
    {
        _grantService = grantService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GrantSummaryResponse>>> List(GrantStatus? status, CancellationToken ct)
    {
        var result = await _grantService.ListAsync(status, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GrantDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await _grantService.GetAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{id}/tasks")]
    public async Task<ActionResult<TaskResponse>> AddTask(Guid id, CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _grantService.AddTaskAsync(id, request, ct);
        return CreatedAtAction(nameof(Get), new { id = id }, task);
    }

    [HttpPut("{id}/close")]
    public async Task<ActionResult<GrantSummaryResponse>> Close(Guid id, CancellationToken ct)
    {
        var result = await _grantService.CloseAsync(id, ct);
        return Ok(result);
    }
}
