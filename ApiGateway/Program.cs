using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 关键：用 UseWhen 让 /api/aggregate 走 Controller，其他走 Ocelot
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/aggregate"),
    appBuilder => appBuilder.UseRouting().UseEndpoints(e => e.MapControllers())
);

await app.UseOcelot();

app.Run();