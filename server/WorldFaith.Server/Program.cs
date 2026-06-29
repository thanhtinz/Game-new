using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;
using WorldFaith.Server.Hubs;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Auth;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Server.Services.Lobby;
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

// ─── JWT Authentication ──────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "WorldFaith_SuperSecret_Key_MustBeAtLeast32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WorldFaith";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WorldFaithPlayers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };

        // SignalR cần JWT qua query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/world") || path.StartsWithSegments("/hubs/lobby")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Repositories ────────────────────────────────────────
builder.Services.AddSingleton<IWorldRepository, WorldRepository>();
builder.Services.AddSingleton<IGodRepository, GodRepository>();
builder.Services.AddSingleton<ICivilizationRepository, CivilizationRepository>();
builder.Services.AddSingleton<IReligionRepository, ReligionRepository>();
builder.Services.AddSingleton<IMiracleEventRepository, MiracleEventRepository>();
builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
builder.Services.AddSingleton<IRoomRepository, RoomRepository>();

// ─── Services ────────────────────────────────────────────
builder.Services.AddSingleton<IFaithService, FaithService>();
builder.Services.AddSingleton<IMiracleService, MiracleService>();
builder.Services.AddSingleton<ICivilizationSimulationService, CivilizationSimulationService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();

// ─── Background Simulation Loop ──────────────────────────
builder.Services.AddHostedService<WorldSimulationLoop>();

// ─── SignalR ─────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024;
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
                "http://localhost:3000",
                "https://worldfaith.game"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("WorldFaithPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ───────────────────────────────────────────
app.MapControllers();
app.MapHub<WorldHub>("/hubs/world");
app.MapHub<LobbyHub>("/hubs/lobby");

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

Log.Information("WorldFaith Server khởi động");
app.Run();
