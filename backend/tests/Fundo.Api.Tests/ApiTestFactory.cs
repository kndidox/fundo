using Fundo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fundo.Api.Tests;

/// <summary>
/// Runs the real API pipeline against an in-memory SQLite database instead of Postgres, and
/// without the outbox background worker (irrelevant to these HTTP-endpoint tests, and SQLite
/// can't translate the worker's DateTimeOffset ordering, so leaving it in would just add noise).
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiTestFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // AddDbContext registers several services keyed to FundoDbContext (DbContextOptions<T>,
            // IDbContextOptionsConfiguration<T>, ...) — remove every one of them, not just
            // DbContextOptions<T>, or the Npgsql and Sqlite providers end up registered together.
            var fundoDbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(FundoDbContext)
                    || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(FundoDbContext))))
                .ToList();
            foreach (var descriptor in fundoDbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<IHostedService>();

            services.AddDbContext<FundoDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<FundoDbContext>().Database.EnsureCreated();
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
