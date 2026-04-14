using CustomerService.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========================================================
// REGISTER SERVICES (Dependency Injection Container)
// ========================================================
// Controllers - Enable API controller endpoints
builder.Services.AddControllers();

// Swagger - API documentation UI (localhost:5001/swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================================================
// DATABASE SETUP - Database-per-Service Pattern
// Each service has its OWN DbContext + SQLite file
// ========================================================
builder.Services.AddDbContext<CustomerDbContext>(options =>
    // Hardcoded path replaced with appsettings.json connection string
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ========================================================
// DATABASE INITIALIZATION - Runs ONCE at startup
// ========================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

    // ✅ CRITICAL: Create /app/data directory BEFORE database creation
    // Without this, EF Core EnsureCreated() fails silently
    Directory.CreateDirectory(Path.GetDirectoryName(db.Database.GetDbConnection().DataSource)!);

    // Create database + apply migrations if any
    // customers.db will appear in ./data/customers/ on host machine
    db.Database.EnsureCreated();
}

// ========================================================
// STARTUP DIAGNOSTICS - Print environment info
// ========================================================
Console.WriteLine("ContentRootPath=" + app.Environment.ContentRootPath);  // /app
Console.WriteLine("Environment=" + app.Environment.EnvironmentName);      // Development
Console.WriteLine("Urls=" + string.Join(", ", app.Urls));                 // http://+:8080

// ========================================================
// MIDDLEWARE PIPELINE - Request processing order
// ========================================================
// Swagger UI - ONLY in Development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // Swagger JSON endpoint
    app.UseSwaggerUI();      // Swagger web UI
}

// HTTPS redirect - DISABLED for Docker simplicity
// app.UseHttpsRedirection();   // Causes issues in containers

// Enable controller routing
app.MapControllers();

// ========================================================
// DEBUG ENDPOINTS - For troubleshooting
// ========================================================
// List all API endpoints (helpful for debugging)
app.MapGet("/__routes", (IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> sources) =>
    string.Join("\n", sources.SelectMany(s => s.Endpoints).Select(e => e.DisplayName)));

// Simple health check
app.MapGet("/__ping", () => "pong");

app.Run();  // Start listening on http://+:8080 (container port)
