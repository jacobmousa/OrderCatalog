using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Infrastructure;
using Xunit;

namespace Orders.Api.Tests;

public class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration (SqlServer)
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
            services.Remove(descriptor);

            _connection ??= new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<OrderDbContext>(o =>
            {
                o.UseSqlite(_connection);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _connection?.Dispose();
        return Task.CompletedTask;
    }
}
