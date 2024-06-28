using FunWithWebSockets.Common;
using FunWithWebSockets.Services;
using FunWithWebSockets.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger());

// Add services to the container.
builder.Services.AddSingleton<TiingoIntegrationService>();
builder.Services.Configure<TiingoConnectionConfiguration>(builder.Configuration.GetSection("TiingoConnectionConfiguration"));
builder.Services.AddHostedService<TiingoWebsocketHostedService>();

// Using in memory cache for demo purposes.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSignalR();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSignalRSwaggerGen();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<NotificationHub>("/notificationHub");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
