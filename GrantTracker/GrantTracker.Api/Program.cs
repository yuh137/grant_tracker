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

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

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

public partial class Program
{
}
