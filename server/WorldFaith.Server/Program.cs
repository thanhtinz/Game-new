using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;
using WorldFaith.Server.Hubs;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Server.Services.Simulation;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// ─── MongoDB ────────────────────────────────────────────
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDb = mongoClient.GetDatabase("worldfaith");
builder.Services.AddSingleton<IMongoDatabase>(mongoDb);

// ─── Redis ──────────────────────────────────────────────
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "worldfaith:";
});

// ─── Repositories ────────────────────────────────────────
builder.Services.AddSingleton<IWorldRepository, WorldRepository>();
builder.Services.AddSingleton<IGodRepository, GodRepository>();
builder.Services.AddSingleton<ICivilizationRepository, CivilizationRepository>();
builder.Services.AddSingleton<IReligionRepository, ReligionRepository>();
builder.Services.AddSingleton<IMiracleEventRepository, MiracleEventRepository>();

// ─── Services ────────────────────────────────────────────
builder.Services.AddSingleton<IFaithService, FaithService>();
builder.Services.AddSingleton<IMiracleService, MiracleService>();
builder.Services.AddSingleton<ICivilizationSimulationService, CivilizationSimulationService>();

// ─── Background Simulation Loop ──────────────────────────
builder.Services.AddHostedService<WorldSimulationLoop>();

// ─── SignalR ─────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
})
.AddStackExchangeRedis(redisConnectionString, options =>
{
    options.Configuration.ChannelPrefix = RedisChannel.Literal("worldfaith");
});

// ─── CORS ────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("WorldFaithPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",   // Dev web client
                "https://worldfaith.game"  // Production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Bắt buộc cho SignalR
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("WorldFaithPolicy");
app.UseRouting();

// ─── SignalR Hub endpoint ────────────────────────────────
app.MapHub<WorldHub>("/hubs/world");
app.MapControllers();

// ─── Health check ────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

Log.Information("WorldFaith Server khởi động tại {Urls}", string.Join(", ", app.Urls));
app.Run();
