using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RazorShop.Data;

namespace RazorShop.Tests;

// Real SQLite in-memory DB on a kept-open connection, schema + seed data built
// from the app's own OnModelCreating. Closing the connection drops the DB.
internal sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public RazorShopDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RazorShopDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new RazorShopDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
