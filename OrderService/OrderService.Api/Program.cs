using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Messaging;
using OrderService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<OrderDbContext>(options =>
    // options.UseSqlite("Data Source=orders.db"));
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient: CustomerService
var customerServiceUrl = builder.Configuration["CustomerServiceUrl"]
    ?? "http://customerservice:8080/";
builder.Services.AddHttpClient<ICustomerClient, CustomerClient>(client =>
{
    client.BaseAddress = new Uri(customerServiceUrl);
});

// HttpClient: ProductService
var productServiceUrl = builder.Configuration["ProductServiceUrl"]
    ?? "http://productservice:8080/";
builder.Services.AddHttpClient<IProductClient, ProductClient>(client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

// RabbitMQ Publisher
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

var app = builder.Build();

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        // 确保 /app/data/ 目录存在
    Directory.CreateDirectory(
        Path.GetDirectoryName(db.Database.GetDbConnection().DataSource)!);
    db.Database.Migrate();
}

// Debug endpoints
app.MapGet("/__ping", () => Results.Text("pong"));

app.MapGet("/__routes", (IEnumerable<EndpointDataSource> sources) =>
    Results.Text(string.Join("\n", sources
        .SelectMany(s => s.Endpoints)
        .Select(e => e.DisplayName)
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Distinct()
        .OrderBy(n => n))));

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
