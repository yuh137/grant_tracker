using GrantTracker.Api.DTO;
using GrantTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrantTracker.Api.Controllers;

[ApiController]
[Route("tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<IReadOnlyList<OverdueTaskResponse>>> Overdue(CancellationToken ct)
    {
        var result = await _taskService.OverdueAsync(ct);
        return Ok(result);
    }

    [HttpPut("{id}/complete")]
    public async Task<ActionResult<TaskResponse>> Complete(Guid id, CancellationToken ct)
    {
        var result = await _taskService.CompleteAsync(id, ct);
        return Ok(result);
    }
}
