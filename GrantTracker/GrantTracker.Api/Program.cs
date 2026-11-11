using System.Text.Json.Serialization;
using GrantTracker.Api;
using GrantTracker.Api.Errors;
using GrantTracker.Api.Health;
using GrantTracker.Api.Services;
using GrantTracker.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var officeZoneId = builder.Configuration["OfficeTimeZone"];
builder.Services.AddSingleton<TimeProvider>(officeZoneId is null
    ? TimeProvider.System
    : new OfficeTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(officeZoneId)));

builder.Services.AddDbContext<GrantTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("GrantTracker")));

builder.Services.AddScoped<GrantService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Grant Task Tracker API"));
}

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.Write
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.Write
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    var recreate = app.Configuration.GetValue<bool>("RecreateDatabaseOnStartup");
    DatabaseInitializer.Initialize(db, timeProvider, recreate);
}

app.Run();
