using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using ProductService.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ProductDbContext>(options =>
    // options.UseSqlite("Data Source=products.db"));
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// RabbitMQ Consumer（后台订阅者）
builder.Services.AddHostedService<OrderCreatedConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        // 确保 /app/data/ 目录存在
    Directory.CreateDirectory(
        Path.GetDirectoryName(db.Database.GetDbConnection().DataSource)!);
    db.Database.Migrate();
}

app.MapGet("/__ping", () => Results.Text("pong"));

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
