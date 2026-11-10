using System.Text.Json;
using System.Text.Json.Serialization;
using GrantTracker.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace GrantTracker.Tests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateOnly Today = new(2026, 3, 15);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 3, 15, 22, 30, 0, TimeSpan.Zero));

    public void DropDatabase() => _connection.Close();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            _connection.Open();
            services.RemoveAll<DbContextOptions<GrantTrackerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GrantTrackerDbContext>>();
            services.RemoveAll<GrantTrackerDbContext>();
            services.AddDbContext<GrantTrackerDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public record ProblemResponse(int Status, string Title, string? Detail, string? Code,
    Dictionary<string, string[]>? Errors);
