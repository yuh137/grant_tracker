using System.Text.Json.Serialization;
using GrantTracker.Api;
using GrantTracker.Api.Errors;
using GrantTracker.Api.Services;
using GrantTracker.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Every error, including unhandled ones, comes back as RFC 7807 ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Time is injected (framework TimeProvider) so tests can pin the date. "Today" is the office's
// calendar day, from Office:TimeZone, not the machine's; without it a UTC container shifts the day.
var officeZoneId = builder.Configuration["Office:TimeZone"];
builder.Services.AddSingleton<TimeProvider>(officeZoneId is null
    ? TimeProvider.System
    : new OfficeTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(officeZoneId)));

builder.Services.AddDbContext<GrantTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("GrantTracker")));

builder.Services.AddScoped<GrantService>();
builder.Services.AddScoped<TaskService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    var recreate = app.Configuration.GetValue<bool>("Database:RecreateOnStartup");
    DatabaseInitializer.Initialize(db, timeProvider, recreate);
}

app.Run();

/// <summary>Makes the entry point visible to WebApplicationFactory in the integration tests.</summary>
public partial class Program
{
}
