using Microsoft.AspNetCore.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Api.Entities;
using Orders.Api.Dtos;
using Orders.Api.Exceptions;
using Orders.Api.Infrastructure;
using Orders.Api.Services;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Using built-in ProblemDetails registration (will produce RFC7807 on automatic responses)
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<OrderDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("OrdersDb") ?? builder.Configuration["ORDERS_DB" ] ?? "Server=localhost;Database=OrdersDb;Trusted_Connection=False;User Id=sa;Password=Your_password123;TrustServerCertificate=True";
    opt.UseSqlServer(cs, sql => sql.EnableRetryOnFailure());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Orders API", Version = "v1", Description = "Order management service" });
    var xml = Path.Combine(AppContext.BaseDirectory, "Orders.Api.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml, includeControllerXmlComments: true);
});
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("catalog", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["CATALOG_BASE_URL"] ?? "http://localhost:5000"; // will override in docker compose
    client.BaseAddress = new Uri(baseUrl);
});
// Outbound correlation propagation via delegating handler
builder.Services.AddTransient<CorrelationPropagationHandler>();
builder.Services.AddHttpClient("catalog-with-correlation", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["CATALOG_BASE_URL"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<CorrelationPropagationHandler>();
builder.Services.AddScoped<IOrderService, OrderService>();
// CORS for local frontend consumers
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
    // Added :3002 for orders-remote standalone dev server
    .WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-correlation-id")
        .AllowCredentials());
});

    var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("Frontend");
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(); // default ProblemDetails formatting
}

// Apply EF Core migrations automatically (OrdersDb) with retry (helps when SQL container still starting)
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var provider = db.Database.ProviderName ?? string.Empty;
            var conn = db.Database.GetDbConnection();
            bool isInMemorySqlite = provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) && conn.ConnectionString.Contains("DataSource=:memory:", StringComparison.OrdinalIgnoreCase);
            if (db.Database.IsRelational() && !isInMemorySqlite)
            {
                var maxAttempts = 20;
                var delay = TimeSpan.FromSeconds(2);
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        if (await db.Database.CanConnectAsync())
                        { logger.LogInformation("OrdersDb connectivity established on attempt {Attempt}/{Max}.", attempt, maxAttempts); break; }
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    { logger.LogWarning(ex, "OrdersDb connectivity attempt {Attempt} failed; retrying in {Delay}s...", attempt, delay.TotalSeconds); }
                    if (attempt == maxAttempts)
                    { logger.LogError("OrdersDb connectivity could not be established after {Max} attempts; proceeding (migrations likely to fail)", maxAttempts); }
                    await Task.Delay(delay);
                }
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        logger.LogInformation("Applying OrdersDb migrations (attempt {Attempt}/{Max})...", attempt, maxAttempts);
                        db.Database.Migrate();
                        break;
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    { logger.LogWarning(ex, "OrdersDb migration attempt {Attempt} failed. Retrying in {Delay}s...", attempt, delay.TotalSeconds); await Task.Delay(delay); }
                    catch (Exception ex)
                    { logger.LogError(ex, "OrdersDb migration failed after {Max} attempts - continuing without ensuring schema", maxAttempts); }
                }
                // Dev-friendly safety net: if Orders table is still missing (e.g., migration assembly mismatch), call EnsureCreated.
                try
                {
                    _ = await db.Orders.AsNoTracking().AnyAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Orders table check failed; attempting EnsureCreated() as a development fallback.");
                    await db.Database.EnsureCreatedAsync();
                }
            }
            else
            {
                logger.LogInformation("Using in-memory provider for OrdersDb; calling EnsureCreated().");
                db.Database.EnsureCreated();
            }
        }

var swaggerEnabled = app.Environment.IsDevelopment() || string.Equals(app.Configuration["SWAGGER__ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

// Orders endpoints migrated to OrdersController; minimal API mappings removed.

app.MapControllers();

        app.Run();
    }
}

// Correlation middleware & handler moved to Infrastructure directory
