using WorldFaith.Server.Services.Admin;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;
using WorldFaith.Server.Hubs;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Middleware;
using WorldFaith.Server.Services.Auth;
using WorldFaith.Server.Services.Achievement;
using WorldFaith.Server.Services.Common;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Server.Services.NPC.Dynasty;
using WorldFaith.Server.Services.Organization;
using WorldFaith.Server.Services.Race;
using WorldFaith.Server.Services.Dungeon;
using WorldFaith.Server.Services.Memory;
using WorldFaith.Server.Services.Chat;
using WorldFaith.Server.Services.Leaderboard;
using WorldFaith.Server.Services.Evolution;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Server.Services.Lobby;
using WorldFaith.Server.Services.Religion;
using WorldFaith.Server.Services.Simulation;
using WorldFaith.Server.Services.WorldGen;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Sink(new WorldFaith.Server.Logging.InMemorySink())
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
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ─── Repositories ────────────────────────────────────────
builder.Services.AddSingleton<IWorldRepository, WorldRepository>();
builder.Services.AddSingleton<IGodRepository, GodRepository>();
builder.Services.AddSingleton<ICivilizationRepository, CivilizationRepository>();
builder.Services.AddSingleton<IReligionRepository, ReligionRepository>();
builder.Services.AddSingleton<IMiracleEventRepository, MiracleEventRepository>();
builder.Services.AddSingleton<IPlayerRepository, PlayerRepository>();
builder.Services.AddSingleton<IRoomRepository, RoomRepository>();
builder.Services.AddSingleton<IEvolutionEntityRepository, EvolutionEntityRepository>();
// v3 Repositories
builder.Services.AddSingleton<INpcRepository, NpcRepository>();
builder.Services.AddSingleton<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddSingleton<INpcEventRepository, NpcEventRepository>();

// ─── Services ────────────────────────────────────────────
builder.Services.AddSingleton<IBalanceConfigService, BalanceConfigService>();
builder.Services.AddSingleton<IFaithService, FaithService>();
builder.Services.AddSingleton<ICommandmentRepository, CommandmentRepository>();
builder.Services.AddSingleton<ICommandmentService, CommandmentService>();
builder.Services.AddSingleton<IMiracleService, MiracleService>();
builder.Services.AddSingleton<ICivilizationSimulationService, CivilizationSimulationService>();
builder.Services.AddSingleton<IReligionService, ReligionService>();
builder.Services.AddSingleton<IEvolutionService, EvolutionService>();
builder.Services.AddSingleton<IScenarioController, ScenarioController>();
builder.Services.AddSingleton<IWorldGeneratorService, WorldGeneratorService>();
// v3 NPC & Organization Services
builder.Services.AddSingleton<INpcSpawnService, NpcSpawnService>();
builder.Services.AddSingleton<INpcInteractionService, NpcInteractionService>();
builder.Services.AddSingleton<IOrganizationService, OrganizationService>();
// v1.0 GDD New Systems
builder.Services.AddSingleton<IRaceRepository, RaceRepository>();
builder.Services.AddSingleton<IDungeonRepository, DungeonRepository>();
builder.Services.AddSingleton<IRelicRepository, RelicRepository>();
builder.Services.AddSingleton<IGuildMissionRepository, GuildMissionRepository>();
builder.Services.AddSingleton<IRaceAffinityService, RaceAffinityService>();
builder.Services.AddSingleton<IGodRankService, GodRankService>();
builder.Services.AddSingleton<IDungeonService, DungeonService>();
builder.Services.AddSingleton<IMemoryService, MemoryService>();
builder.Services.AddSingleton<IDoctrineService, DoctrineService>();
builder.Services.AddSingleton<IGovernmentService, GovernmentService>();
builder.Services.AddSingleton<IBelieverTypeService, BelieverTypeService>();
builder.Services.AddSingleton<IConversionService, ConversionService>();
builder.Services.AddSingleton<IAiDirectorService, AiDirectorService>();
// Add-On v1.1: NPC Achievement & Divine Recognition
builder.Services.AddSingleton<IAchievementService, AchievementService>();
// Add-On v1.2: Doctrine Integrity & Escort System
builder.Services.AddSingleton<IDoctrineIntegrityService, DoctrineIntegrityService>();
builder.Services.AddSingleton<IEscortService, EscortService>();
// NPC Master Spec Phase 2: belief math
builder.Services.AddSingleton<IRandomService, RandomService>();
builder.Services.AddSingleton<INpcFaithDecisionService, NpcFaithDecisionService>();
// NPC Master Spec Phase 5: social influence
builder.Services.AddSingleton<INpcSocialInfluenceService, NpcSocialInfluenceService>();
// NPC Master Spec Phase 6: population-scale grouped simulation
builder.Services.AddSingleton<IPopulationFaithService, PopulationFaithService>();
// NPC Master Spec Phase 8: player-facing risk indicators
builder.Services.AddSingleton<INpcIndicatorService, NpcIndicatorService>();
// Dynasty / Bloodline Spec Phase 1: inheritance
builder.Services.AddSingleton<IBloodlineAffinityService, BloodlineAffinityService>();
builder.Services.AddSingleton<IBloodlineInheritanceService, BloodlineInheritanceService>();
// Dynasty / Bloodline Spec Phase 2: family trees
builder.Services.AddSingleton<IGeneMixingService, GeneMixingService>();
builder.Services.AddSingleton<IFamilyTreeService, FamilyTreeService>();
// Dynasty / Bloodline Spec Phase 3: blessing/curse founding
builder.Services.AddSingleton<IBloodlineFoundingService, BloodlineFoundingService>();
// Dynasty / Bloodline Spec Phase 4: hybrids & awakening
builder.Services.AddSingleton<IReadOnlyList<HybridBloodlineRule>>(HybridBloodlineRules.Default);
builder.Services.AddSingleton<IHybridBloodlineService, HybridBloodlineService>();
builder.Services.AddSingleton<IBloodlineAwakeningService, BloodlineAwakeningService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IAdminService, AdminService>();
builder.Services.AddSingleton<IChatRepository, ChatRepository>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();

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
app.UseWorldFaithRateLimit(opt =>
{
    opt.ApiMaxRequests   = 120;
    opt.ApiWindowSeconds = 60;
    opt.AuthMaxRequests  = 10;
    opt.AuthWindowSeconds= 60;
});
app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ───────────────────────────────────────────
app.MapControllers();
app.MapHub<WorldHub>("/hubs/world");
app.MapHub<LobbyHub>("/hubs/lobby");
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

Log.Information("WorldFaith Server starting up");

// Seed default balance config + admin account
using (var scope = app.Services.CreateScope())
{
    var balanceConfig = scope.ServiceProvider.GetRequiredService<IBalanceConfigService>();
    await balanceConfig.SeedDefaultsAsync();
    Log.Information("Balance config seeded");

    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var adminEmail    = builder.Configuration["Admin:Email"]    ?? "admin@worldfaith.game";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin@WorldFaith2024!";
    await authService.SeedAdminAsync(adminEmail, adminPassword);
    Log.Information("Admin account seeded: {Email}", adminEmail);
}

app.Run();
