using GrantTracker.Api.Contracts;
using GrantTracker.Api.Services;
using GrantTracker.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GrantTracker.Api.Controllers;

[ApiController]
[Route("grants")]
[Produces("application/json")]
public sealed class GrantsController(GrantService grants) : ControllerBase
{
    /// <summary>List grants, optionally filtered by status, with the number of pending tasks on each.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GrantSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<GrantSummaryResponse>> List([FromQuery] GrantStatus? status, CancellationToken ct) =>
        grants.ListAsync(status, ct);

    /// <summary>One grant with its tasks, ordered by due date.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GrantDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<GrantDetailResponse> Get(Guid id, CancellationToken ct) =>
        grants.GetAsync(id, ct);

    /// <summary>Add a task to a grant (BR-1, BR-2, BR-3).</summary>
    [HttpPost("{id:guid}/tasks")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskResponse>> AddTask(Guid id, CreateTaskRequest request, CancellationToken ct)
    {
        var task = await grants.AddTaskAsync(id, request, ct);
        return CreatedAtAction(nameof(Get), new { id }, task);
    }

    /// <summary>Close a grant (BR-5). Closing a closed grant is a safe no-op.</summary>
    [HttpPut("{id:guid}/close")]
    [ProducesResponseType<GrantSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<GrantSummaryResponse> Close(Guid id, CancellationToken ct) =>
        grants.CloseAsync(id, ct);
}
