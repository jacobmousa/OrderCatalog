using System.Linq;
using System.Threading.Tasks;
using Catalog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.Api.Tests;

public class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>));
            services.Remove(descriptor);

            _connection ??= new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ProductDbContext>(o =>
            {
                o.UseSqlite(_connection);
            });

            // Build provider to initialize schema (EnsureCreated for lightweight schema build)
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new Task DisposeAsync()
    {
        _connection?.Dispose();
        return Task.CompletedTask;
    }
}
