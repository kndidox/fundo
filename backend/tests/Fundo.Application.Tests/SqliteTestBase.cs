using Fundo.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Application.Tests;

/// <summary>
/// Backs each test with a real relational SQLite connection (kept open for the test's
/// lifetime) instead of the EF Core InMemory provider — SQLite supports real transactions,
/// InMemory does not, and the HU explicitly rules InMemory out for that reason.
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<FundoDbContext> _options;

    protected SqliteTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<FundoDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new FundoDbContext(_options);
        context.Database.EnsureCreated();
    }

    protected FundoDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
