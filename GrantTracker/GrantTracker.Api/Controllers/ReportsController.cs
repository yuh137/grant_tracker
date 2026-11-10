using System.ComponentModel.DataAnnotations;
using GrantTracker.Api.DTO;
using GrantTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrantTracker.Api.Controllers;

[ApiController]
[Route("reports")]
public class ReportsController : ControllerBase
{
    public const int MaxDays = 366;

    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("task-activity")]
    public async Task<ActionResult<TaskActivityReport>> TaskActivity([Range(1, MaxDays)] int days = 14,
        CancellationToken ct = default)
    {
        var result = await _reportService.TaskActivityAsync(days, ct);
        return Ok(result);
    }
}
