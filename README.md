# WorldFaith - Game Project Base

God simulation multiplayer game. PC & Mobile.

## Tech Stack

| Layer | Tech |
|-------|------|
| Server | ASP.NET Core 8, SignalR, MongoDB, Redis |
| Client | Unity 2022 LTS |
| Shared | .NET Standard 2.1 |
| Transport | WebSocket (SignalR) |
| Database | MongoDB + Redis |

## Cấu trúc monorepo

```
WorldFaith/
├── WorldFaith.sln
├── docker-compose.yml
├── Dockerfile
│
├── shared/
│   └── WorldFaith.Shared/          # .NET Standard 2.1
│       ├── Enums/GameEnums.cs       # Tất cả enums game
│       ├── Models/GameModels.cs     # DTOs trao đổi client-server
│       └── Contracts/SignalRContracts.cs  # Request/Event contracts
│
├── server/
│   └── WorldFaith.Server/           # ASP.NET Core 8
│       ├── Program.cs               # DI + App setup
│       ├── Hubs/WorldHub.cs         # SignalR Hub chính
│       ├── Models/Documents.cs      # MongoDB documents
│       ├── Repositories/            # Data access layer
│       └── Services/
│           ├── Faith/FaithService.cs
│           └── Simulation/
│               ├── CivilizationSimulationService.cs
│               ├── MiracleService.cs
│               └── WorldSimulationLoop.cs  # Background tick engine
│
└── client-unity/
    └── Assets/WorldFaith/
        ├── Network/
        │   ├── WorldFaithClient.cs  # SignalR connection manager
        │   └── MainThreadDispatcher.cs
        ├── Managers/
        │   └── GameManager.cs       # Game state coordinator
        ├── UI/
        │   └── GodPanel.cs          # HUD + MiraclePanel
        └── WorldRenderer.cs         # Tile rendering
```

## Setup Server

### Local Development

```bash
# Khởi động MongoDB và Redis
docker-compose up worldfaith-mongo worldfaith-redis -d

# Chạy server
cd server/WorldFaith.Server
dotnet run
```

Server chạy tại `http://localhost:5000`

### Docker toàn bộ

```bash
docker-compose up -d
```

## Setup Unity Client

### Packages cần cài (Package Manager)

1. **Microsoft SignalR Client**
   - Download: https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client
   - Giải nén lấy các .dll trong `lib/netstandard2.1/`
   - Thêm vào `Assets/Plugins/SignalR/`

2. **TextMeshPro** - có sẵn trong Package Manager

3. **Newtonsoft.Json cho Unity**
   - Package: `com.unity.nuget.newtonsoft-json`

### Shared Library trong Unity

Copy thư mục `shared/WorldFaith.Shared/` vào `client-unity/Assets/WorldFaith/Shared/`
Hoặc build thành DLL và thêm vào `Assets/Plugins/`

### Scene Setup

Tạo scene `GameScene` với hierarchy:
```
- [Network] (Empty GameObject)
  - WorldFaithClient
  - MainThreadDispatcher
- [Managers] (Empty GameObject)
  - GameManager
- [World] (Empty GameObject)
  - WorldRenderer
- Canvas
  - GodPanel
  - MiraclePanel
- Camera
```

## Hệ thống hiện có

### Server
- [x] Simulation loop (500ms/tick)
- [x] Faith Economy (generation, consumption)
- [x] AI Civilization (5 personalities)
- [x] Miracle System (15 miracles, counter system)
- [x] SignalR Hub (join, miracle, world state sync)
- [x] MongoDB repositories
- [x] Rebirth cycle system

### Client
- [x] SignalR connection với auto-reconnect
- [x] GameManager (state coordinator)
- [x] WorldRenderer (tile + civ rendering)
- [x] GodPanel HUD
- [x] MiraclePanel UI

### Chưa có (TODO)
- [ ] Religion spread system
- [ ] Evolution system
- [ ] Authentication (JWT)
- [ ] World generation (procedural tiles)
- [ ] Sound effects và particle effects
- [ ] Mobile input (touch controls)
- [ ] Lobby / matchmaking UI
- [ ] Admin panel

## Simulation Tick Rate

- 1 tick = 500ms
- Rebirth cycle = mỗi 1000 ticks (~8 phút/cycle)
- Faith generation = mỗi tick
- AI behavior = mỗi 5-12 ticks tùy personality
