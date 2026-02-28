using CustomerService.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core + SQLite (database-per-service)
builder.Services.AddDbContext<CustomerDbContext>(options =>
    // options.UseSqlite("Data Source=customers.db"));
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

    Directory.CreateDirectory(Path.GetDirectoryName(db.Database.GetDbConnection().DataSource)!);

    db.Database.EnsureCreated();
}


// Startup self-check logs (optional but useful)
Console.WriteLine("ContentRootPath=" + app.Environment.ContentRootPath);
Console.WriteLine("Environment=" + app.Environment.EnvironmentName);
Console.WriteLine("Urls=" + string.Join(", ", app.Urls));

// Dev-only Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Dev phase: keep this off to avoid https redirect issues
// app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

// Debug endpoints (optional)
app.MapGet("/__routes", (IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> sources) =>
    string.Join("\n", sources.SelectMany(s => s.Endpoints).Select(e => e.DisplayName)));

app.MapGet("/__ping", () => "pong");

app.Run();
