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
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GrantSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<GrantSummaryResponse>> List([FromQuery] GrantStatus? status, CancellationToken ct) =>
        grants.ListAsync(status, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GrantDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<GrantDetailResponse> Get(Guid id, CancellationToken ct) =>
        grants.GetAsync(id, ct);

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

    [HttpPut("{id:guid}/close")]
    [ProducesResponseType<GrantSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<GrantSummaryResponse> Close(Guid id, CancellationToken ct) =>
        grants.CloseAsync(id, ct);
}
