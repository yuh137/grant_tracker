using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrantTracker.Api.Health;

public static class HealthResponseWriter
{
    public static Task Write(HttpContext context, HealthReport report)
    {
        var body = new HealthResponse(
            report.Status.ToString(),
            Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            report.Entries.Select(e => new HealthCheckEntry(
                e.Key,
                e.Value.Status.ToString(),
                e.Value.Description,
                Math.Round(e.Value.Duration.TotalMilliseconds, 1))).ToList());

        return context.Response.WriteAsJsonAsync(body, context.RequestAborted);
    }
}

public record HealthResponse(string Status, double TotalDurationMs, IReadOnlyList<HealthCheckEntry> Checks);

public record HealthCheckEntry(string Name, string Status, string? Description, double DurationMs);
