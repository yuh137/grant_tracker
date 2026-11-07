using GrantTracker.Api.Contracts;
using GrantTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrantTracker.Api.Controllers;

[ApiController]
[Route("tasks")]
[Produces("application/json")]
public sealed class TasksController(TaskService tasks) : ControllerBase
{
    [HttpGet("overdue")]
    [ProducesResponseType<IReadOnlyList<OverdueTaskResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<OverdueTaskResponse>> Overdue(CancellationToken ct) =>
        tasks.OverdueAsync(ct);

    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Complete(Guid id, CancellationToken ct) =>
        tasks.CompleteAsync(id, ct);
}
