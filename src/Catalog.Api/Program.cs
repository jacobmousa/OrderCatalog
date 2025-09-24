// Global usings reference EF Core & Serilog
using Microsoft.AspNetCore.Mvc;
using Catalog.Api.Entities;
using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Catalog.Api.Infrastructure;
using Catalog.Api.Services;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Serilog configuration (basic for now)
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<ProductDbContext>(opt =>
{
    // Ensure fallback uses the mapped host port 14333 when running outside container
    var cs = builder.Configuration.GetConnectionString("CatalogDb")
             ?? builder.Configuration["CATALOG_DB"]
             ?? "Server=localhost,14333;Database=CatalogDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true";
    opt.UseSqlServer(cs, sql => sql.EnableRetryOnFailure());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Catalog API", Version = "v1", Description = "Product catalog service" });
    var xml = Path.Combine(AppContext.BaseDirectory, "Catalog.Api.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml, includeControllerXmlComments: true);
});
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IProductService, ProductService>();
// CORS for local frontend (web-shell at :3000 and catalog-remote at :3001)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
    // Added :3002 to allow orders-remote (or other future remotes) to call catalog if needed
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
// In Development show detailed exceptions; in other envs use ProblemDetails default handler automatically.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(); // default pipeline, will emit ProblemDetails
}

Log.Logger.Information("Catalog.Api starting in {Environment} (IsDevelopment={IsDev})", app.Environment.EnvironmentName, app.Environment.IsDevelopment());

// Apply EF Core migrations automatically only for relational providers (skip for InMemory tests)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    if (db.Database.IsRelational())
    {
        // Detect in-memory SQLite used by integration tests: single shared connection kept open by test factory.
        var provider = db.Database.ProviderName ?? string.Empty;
        var conn = db.Database.GetDbConnection();
        bool isInMemorySqlite = provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) && conn.ConnectionString.Contains("DataSource=:memory:", StringComparison.OrdinalIgnoreCase);

        if (isInMemorySqlite)
        {
            // For in-memory SQLite we avoid full migration pipeline (multiple open/close cycles break ephemeral DB state).
            logger.LogInformation("Using in-memory SQLite for tests; calling EnsureCreated instead of Migrate.");
            db.Database.EnsureCreated();
        }
        else
        {
            var rawCs = db.Database.GetConnectionString() ?? "<null>";
            string Redact(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"Password=([^;]+)", "Password=***");
            logger.LogInformation("CatalogDb effective connection string: {ConnectionString}", Redact(rawCs));
            var maxAttempts = 20;
            var delay = TimeSpan.FromSeconds(2);
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (await db.Database.CanConnectAsync())
                    { logger.LogInformation("CatalogDb connectivity established (attempt {Attempt}/{Max})", attempt, maxAttempts); break; }
                    logger.LogWarning("CatalogDb connectivity attempt {Attempt}/{Max} failed; retrying in {Delay}s", attempt, maxAttempts, delay.TotalSeconds);
                }
                catch (Exception ex) when (attempt < maxAttempts)
                { logger.LogWarning(ex, "Connectivity attempt {Attempt} failed; retrying", attempt); }
                await Task.Delay(delay);
            }
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    logger.LogInformation("Applying CatalogDb migrations (attempt {Attempt}/{Max})", attempt, maxAttempts);
                    db.Database.Migrate();
                    if (!db.Products.Any())
                    {
                        db.Products.AddRange(new[]
                        {
                            new Product { Sku = "SKU-1001", Name = "Demo Keyboard", Price = 49.99m, Stock = 100 },
                            new Product { Sku = "SKU-1002", Name = "Demo Mouse", Price = 19.99m, Stock = 250 },
                            new Product { Sku = "SKU-1003", Name = "Demo Monitor", Price = 199.99m, Stock = 40 }
                        });
                        db.SaveChanges();
                        logger.LogInformation("Seeded demo products");
                    }
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                { logger.LogWarning(ex, "Migration attempt {Attempt} failed; retrying", attempt); await Task.Delay(delay); }
                catch (Exception ex)
                { logger.LogError(ex, "Migrations failed after {Max} attempts", maxAttempts); }
            }
        }
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

var swaggerEnabled = app.Environment.IsDevelopment() || string.Equals(app.Configuration["SWAGGER__ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Lightweight diagnostics endpoint (Development only) to inspect applied migrations & pending ones
if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/migrations", (ProductDbContext dbCtx) =>
    {
        var applied = dbCtx.Database.GetAppliedMigrations().ToList();
        var pending = dbCtx.Database.GetPendingMigrations().ToList();
        return Results.Ok(new { applied, pending });
    });

    app.MapGet("/debug/databases", async (IConfiguration cfg) =>
    {
        try
        {
            var cs = cfg.GetConnectionString("CatalogDb") ?? cfg["CATALOG_DB"] ?? "Server=localhost,14333;Database=master;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False";
            // Force master for listing databases
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cs) { InitialCatalog = "master" };
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sys.databases ORDER BY name";
            var list = new List<string>();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
            return Results.Ok(new { databases = list });
        }
        catch (Exception ex)
        {
            return Results.Problem(title: "Database enumeration failed", detail: ex.Message, statusCode: 500);
        }
    });

    app.MapGet("/debug/catalog-tables", async (IConfiguration cfg) =>
    {
        try
        {
            var cs = cfg.GetConnectionString("CatalogDb") ?? cfg["CATALOG_DB"] ?? "Server=localhost,14333;Database=CatalogDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False";
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(cs);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
            var tables = new List<string>();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) tables.Add(rdr.GetString(0));
            return Results.Ok(new { tables });
        }
        catch (Exception ex)
        {
            return Results.Problem(title: "Catalog table enumeration failed", detail: ex.Message, statusCode: 500);
        }
    });
}

// Additional debug endpoints (available in Development OR when DEBUG__ENABLED=true)
bool debugEnabled = app.Environment.IsDevelopment() || string.Equals(app.Configuration["DEBUG__ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
if (debugEnabled)
{
    app.MapGet("/debug/info", (IConfiguration cfg, IHostEnvironment env) =>
    {
        var cs = cfg.GetConnectionString("CatalogDb") ?? cfg["CATALOG_DB"] ?? "<none>";
        var redacted = System.Text.RegularExpressions.Regex.Replace(cs, @"Password=([^;]+)", "Password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return Results.Ok(new
        {
            environment = env.EnvironmentName,
            isDevelopment = env.IsDevelopment(),
            debugEnabled = true,
            swaggerEnabled = env.IsDevelopment() || string.Equals(cfg["SWAGGER__ENABLED"], "true", StringComparison.OrdinalIgnoreCase),
            connectionString = redacted
        });
    });
    app.MapGet("/debug/endpoints", (EndpointDataSource eds) =>
    {
        var list = eds.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new { route = e.RoutePattern.RawText, displayName = e.DisplayName })
            .OrderBy(e => e.route)
            .ToList();
        return Results.Ok(list);
    });
}

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.MapControllers();

        app.Run();
    }
}
// Records moved to dedicated files under Products folder
