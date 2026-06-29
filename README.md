# ⚡ WorldFaith — Hướng Dẫn Cài Đặt Từ A Đến Z

> **WorldFaith** là game god simulation sandbox multiplayer.  
> Bạn đóng vai một vị thần: thu phục tín đồ, thực hiện phép màu, sáng lập tôn giáo, tiến hóa sinh vật.  
> *"Players do not control the world. They influence belief, and belief controls the world."*

---

## 📋 Mục lục

1. [Yêu cầu — Cài gì trước?](#1-yêu-cầu--cài-gì-trước)
2. [Lấy code về máy](#2-lấy-code-về-máy)
3. [Cài và chạy Database](#3-cài-và-chạy-database-mongodb--redis)
4. [Cấu hình Server](#4-cấu-hình-server)
5. [Chạy Server](#5-chạy-server)
6. [Admin Panel](#6-admin-panel-trang-quản-trị)
7. [Cài đặt Unity Client](#7-cài-đặt-unity-client-game)
8. [Chuẩn bị Asset](#8-chuẩn-bị-asset)
9. [Build game ra file](#9-build-game-ra-file)
10. [Kiểm tra hoạt động](#10-kiểm-tra-hoạt-động)
11. [Deploy production](#11-deploy-lên-server-thật)
12. [Tài khoản mặc định](#12-tài-khoản-mặc-định)
13. [Tính năng đầy đủ](#13-tính-năng-đầy-đủ)
14. [Thông số kỹ thuật](#14-thông-số-kỹ-thuật)
15. [Câu hỏi thường gặp](#15-câu-hỏi-thường-gặp)

---

## 1. Yêu cầu — Cài gì trước?

Cài **4 công cụ** theo thứ tự này:

### 🔵 .NET SDK 8
```
Tải: https://dotnet.microsoft.com/download → chọn .NET 8.0
Kiểm tra: dotnet --version → phải thấy 8.x.x
```

### 🐳 Docker Desktop
```
Tải: https://www.docker.com/products/docker-desktop
Mở Docker Desktop lên và đợi icon cá voi hết loading
Kiểm tra: docker --version → phải thấy 24.x.x+
```

### 🟢 Node.js 20 LTS
```
Tải: https://nodejs.org → chọn bản LTS (bên trái)
Kiểm tra: node --version → phải thấy v20.x.x+
```

### 🎮 Unity 2022.3 LTS
```
Tải Unity Hub: https://unity.com/download
Đăng nhập → Installs → Install Editor → chọn Unity 2022.3 LTS
Tích thêm: Android Build Support (nếu muốn build Android)
```

---

## 2. Lấy code về máy

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
```

---

## 3. Cài và chạy Database (MongoDB + Redis)

```bash
# Đảm bảo Docker Desktop đang chạy, sau đó:
docker-compose up worldfaith-mongo worldfaith-redis -d
```

Lần đầu tải về khoảng vài phút. Kiểm tra:
```bash
docker ps
# Phải thấy 2 dòng trạng thái Up:
# worldfaith-mongo    Up ...
# worldfaith-redis    Up ...
```

### Các lệnh quản lý database
```bash
# Dừng
docker-compose stop worldfaith-mongo worldfaith-redis

# Khởi động lại
docker-compose start worldfaith-mongo worldfaith-redis

# Backup dữ liệu
docker exec worldfaith-mongo mongodump --db worldfaith --archive=/tmp/backup.gz --gzip
docker cp worldfaith-mongo:/tmp/backup.gz ./backup-$(date +%Y%m%d).gz

# Khôi phục dữ liệu
docker cp backup-20240101.gz worldfaith-mongo:/tmp/restore.gz
docker exec worldfaith-mongo mongorestore --archive=/tmp/restore.gz --gzip

# Xem dữ liệu trực quan: MongoDB Compass (https://www.mongodb.com/products/compass)
# Kết nối: mongodb://localhost:27017
```

---

## 4. Cấu hình Server

File: `server/WorldFaith.Server/appsettings.json`

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "WorldFaith_SuperSecret_Key_MustBeAtLeast32Chars!",
    "Issuer": "WorldFaith",
    "Audience": "WorldFaithPlayers",
    "AccessTokenMinutes": "60",
    "RefreshTokenDays": "30"
  },
  "Admin": {
    "Email": "admin@worldfaith.game",
    "Password": "Admin@WorldFaith2024!"
  }
}
```

> ⚠️ **Production:** Đổi `Jwt.Secret` thành chuỗi ngẫu nhiên ≥ 32 ký tự và đổi `Admin.Password`.

---

## 5. Chạy Server

```bash
cd Game-new/server/WorldFaith.Server
dotnet run
```

Khi thấy các dòng sau là thành công:
```
[INF] WorldFaith Server khởi động
[INF] Balance config seeded (90 params)
[INF] Admin account seeded: admin@worldfaith.game
[INF] Now listening on: http://localhost:5000
```

Kiểm tra: mở trình duyệt → `http://localhost:5000/health` → thấy `{"status":"ok"}`

**Chạy bằng Docker (production):**
```bash
docker-compose up -d        # khởi động tất cả: DB + Server
docker-compose down         # dừng tất cả
docker-compose logs -f      # xem logs
```

---

## 6. Admin Panel (Trang Quản Trị)

### Cài đặt
```bash
cd Game-new/admin-panel
npm install
```

Tạo file `.env.local` trong thư mục `admin-panel/`:
```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Chạy
```bash
npm run dev
```

Mở: **http://localhost:3001** → đăng nhập với `admin@worldfaith.game` / `Admin@WorldFaith2024!`

### 16 Trang Admin Panel

| Trang | Quản lý |
|-------|---------|
| **Dashboard** | Server health realtime, 8 stat cards, active worlds với tick/cycle/god count |
| **Events Log** | Feed realtime 3s — Crime, Marriage, Betrayal, Miracle, Rebellion... lọc theo loại |
| **Worlds** | Tạo/xem/force-end/force-rebirth các world |
| **Maps & Tiles** | Visual map editor — click tile đổi biome, fertility, temple, Sacred |
| **Scenarios** | Thông tin 6 kịch bản game |
| **Gods** | Faith/Trust/Fear/Rank, unlock miracles, eliminate god |
| **NPCs** | 5 tier Commoner→Royalty, loyalty/ambition/piety, kill/exile, promote Champion |
| **Mobs / Entities** | Evolve dropdown, spawn tại tọa độ, kill, xem stats |
| **Civilizations** | Economy/Military/Food/Stability/Government/Race, collapse, quick boost |
| **Religions** | Followers/temples/devotion, 5 Doctrine Axes (sliders), Believer types, Schism |
| **Organizations** | Noble Houses, Guild, Underground — power/heat/loyalty, expose, disband |
| **Players** | Ban/unban, reset password, promote/demote Admin, pagination + search |
| **Leaderboard** | Top players theo rating/wins/followers |
| **Balance Config** | 90 params trong 9 nhóm, inline edit, hiệu lực sau 60s |

---

## 7. Cài đặt Unity Client (Game)

### Bước 1 — Mở project
```
Unity Hub → Open → Add project from disk → chọn Game-new/client-unity/
Chọn Unity 2022.3 LTS → Open
Đợi import (lần đầu 5-10 phút, lỗi đỏ ban đầu là bình thường)
```

### Bước 2 — Cài packages

**Window → Package Manager → Unity Registry:**

| Package | Cách cài |
|---------|---------|
| TextMeshPro | Tìm → Install → sau đó: Window → TextMeshPro → Import TMP Essential Resources |
| Newtonsoft Json | Nhấn + → Add package by name → `com.unity.nuget.newtonsoft-json` |
| Mobile Notifications | Nhấn + → Add package by name → `com.unity.mobile.notifications` |

### Bước 3 — Cài SignalR (kết nối realtime)

**Mac/Linux:**
```bash
DST="Game-new/client-unity/Assets/Plugins/SignalR"
mkdir -p "$DST"
cd /tmp && mkdir signalr_tmp && cd signalr_tmp
dotnet new console -n sr && cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release
cp bin/Release/net8.0/publish/Microsoft.AspNetCore.SignalR.*.dll "../../../../$DST/"
cp bin/Release/net8.0/publish/Microsoft.AspNetCore.Http.Connections*.dll "../../../../$DST/"
cd /tmp && rm -rf signalr_tmp
```

**Windows (thủ công):**
1. Tải từ: `https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/8.0.0`
2. Đổi `.nupkg` → `.zip` rồi giải nén → vào `lib/netstandard2.1/`
3. Copy các `.dll` vào `Game-new/client-unity/Assets/Plugins/SignalR/`

### Bước 4 — Link Shared Library

**Mac/Linux:**
```bash
SRC="Game-new/shared/WorldFaith.Shared"
DST="Game-new/client-unity/Assets/WorldFaith/Shared"
mkdir -p "$DST"
cp -r "$SRC/Enums" "$SRC/Models" "$SRC/Contracts" "$DST/"
```

**Windows (PowerShell):**
```powershell
$src = "Game-new/shared/WorldFaith.Shared"
$dst = "Game-new/client-unity/Assets/WorldFaith/Shared"
New-Item -ItemType Directory -Force -Path $dst
Copy-Item "$src/Enums","$src/Models","$src/Contracts" $dst -Recurse -Force
```

### Bước 5 — Tạo Scenes

```
File → New Scene → lưu LoginScene → WorldFaith → Setup → Create Login Scene Objects
File → New Scene → lưu LobbyScene → WorldFaith → Setup → Create Lobby Scene Objects
File → New Scene → lưu GameScene  → WorldFaith → Setup → Create Game Scene Objects
```

### Bước 6 — Cấu hình Server URL

| GameObject | Field | Giá trị |
|------------|-------|---------|
| `WorldFaithClient` | Server Url | `http://localhost:5000/hubs/world` |
| `LobbyClient` | Server Url | `http://localhost:5000/hubs/lobby` |
| `ChatClient` | Server Url | `http://localhost:5000/hubs/chat` |

### Bước 7 — Validate
```
WorldFaith → Validate → Check All Managers → tất cả ✅ là đúng
```

---

## 8. Chuẩn bị Asset

Xem danh sách đầy đủ tại **[ASSETS.md](./ASSETS.md)** (~204 files, chia 3 mức ưu tiên).

### 🔴 Ưu tiên cao (bắt buộc)

**8 Tile Textures** → `Assets/WorldFaith/World/Tiles/` (64×64 px)
```
tile_grassland.png  tile_forest.png   tile_mountain.png  tile_desert.png
tile_tundra.png     tile_water.png    tile_volcano.png   tile_sacred.png
```

**47 SFX** → `Assets/WorldFaith/Audio/SFX/` — gán vào `AudioManager.sfxClips[]`

**5 Music** → `Assets/WorldFaith/Audio/Music/`
```
music_base.mp3  music_religion.mp3  music_war.mp3  music_apocalypse.mp3  music_victory.mp3
```

### 🟡 Ưu tiên trung bình
- 28 VFX Prefabs (Particle System)
- 8 Archetype Icons + 15 Miracle Icons
- 4 Fonts (Cinzel, Nunito, Rajdhani từ fonts.google.com)

### Nguồn tải miễn phí
```
SFX:    freesound.org / kenney.nl / zapsplat.com
Music:  incompetech.com / freemusicarchive.org
Sprites: kenney.nl / game-icons.net
Fonts:  fonts.google.com
```

---

## 9. Build Game Ra File

### PC (Windows/Mac/Linux)
```
File → Build Settings → kéo 3 scenes vào theo thứ tự:
  0: Assets/Scenes/LoginScene
  1: Assets/Scenes/LobbyScene
  2: Assets/Scenes/GameScene
Platform: PC, Mac & Linux Standalone → Build
```

### Android
```
File → Build Settings → Android → Switch Platform
Player Settings → Package Name: com.yourname.worldfaith
                → Minimum API Level: Android 8.0 (API 26)
Build → tạo file .apk
```

---

## 10. Kiểm tra Hoạt Động

```bash
# 1. Database đang chạy
docker ps   # phải thấy worldfaith-mongo và worldfaith-redis đều Up

# 2. Server đang chạy
curl http://localhost:5000/health   # → {"status":"ok"}

# 3. Admin Panel
# Mở http://localhost:3001 → đăng nhập

# 4. Unity → Play (▶️) → đăng ký tài khoản → vào Lobby → tạo phòng → Start game
# Kiểm tra Gods, Civs, Events trong Admin Panel khi game đang chạy
```

---

## 11. Deploy Lên Server Thật

### Yêu cầu VPS

| Người chơi | CPU | RAM | Băng thông |
|-----------|-----|-----|-----------|
| 2-10 | 2 core | 4 GB | 10 Mbps |
| 10-30 | 4 core | 8 GB | 20 Mbps |
| 30+ | 8 core | 16 GB | 50 Mbps |

OS: Ubuntu 22.04 LTS

### Cài Docker và deploy
```bash
# Cài Docker trên VPS
sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER

# Upload và chạy
scp -r Game-new/ user@YOUR_SERVER_IP:/opt/worldfaith/
ssh user@YOUR_SERVER_IP
cd /opt/worldfaith

# Tạo biến môi trường production
cat > .env << 'EOF'
JWT_SECRET=thay_bang_chuoi_ngau_nhien_32_ky_tu_tro_len
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=MatKhauManh@2024!
EOF

docker-compose up -d
curl http://localhost:5000/health   # kiểm tra
```

### Nginx + HTTPS
```bash
sudo apt install -y nginx
sudo tee /etc/nginx/sites-available/worldfaith << 'EOF'
server {
    listen 80;
    server_name api.yourdomain.com;
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 86400;
    }
}
EOF
sudo ln -s /etc/nginx/sites-available/worldfaith /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# HTTPS miễn phí
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
```

Sau đó cập nhật Server URL trong Unity:
```
http://localhost:5000/hubs/world  →  https://api.yourdomain.com/hubs/world
```

---

## 12. Tài Khoản Mặc Định

| | |
|--|--|
| **Email** | `admin@worldfaith.game` |
| **Mật khẩu** | `Admin@WorldFaith2024!` |
| **Quyền** | Admin (toàn quyền) |

> ⚠️ Đổi mật khẩu trong `appsettings.json` → `Admin.Password` trước khi deploy.

---

## 13. Tính Năng Đầy Đủ

### Faith Economy (GDD §8)
```
Faith/tick = (Followers × Devotion × RaceAffinity × Trust × Institution × Event) × ArchetypeBonus × GodRank
```
- 5 believer types: Casual 0.5x / Devout 1.0x / Fanatic 2.0x / Cultist 1.5x / Heretic 0.3x
- Shift believers qua events: MiracleSuccess → Casual→Devout, HolyWar → Devout→Fanatic, Persecution → Devout→Cultist

### Race Faith Affinity (GDD §9)
| Race | Top Affinity | Low Affinity |
|------|-------------|-------------|
| Human | Order/Light/War 120-140% | — |
| Elf | Nature 160%, Light 140% | Darkness 40% |
| Dwarf | Order 160%, Knowledge 140% | Chaos 40% |
| Orc | War 160%, Chaos 140% | Knowledge 40% |
| Beastfolk | Nature 160%, Chaos 130% | Order 50% |
| Demon | Darkness 160%, Chaos 150% | Light 20% (Taboo) |
| Angel | Light 160%, Order 150% | Darkness 10% (Deep Taboo) |
| Undead | Death 160%, Darkness 140% | Nature 20% |

- Personal trait overrides: Genius Orc → Knowledge+50%, Fanatic amplifies base×1.3
- Environmental memory: events ảnh hưởng lâu dài (god floods orc camp → long-term trust loss)

### God Rank System (GDD §7)
| Rank | Faith Threshold | Multiplier | Miracle Unlocks |
|------|----------------|-----------|----------------|
| Forgotten | 0 followers | 0.1x | Survive via relics/cults |
| Nascent | 0 | 1.0x | Dream, Rain, BlessHarvest |
| Awakened | 5,000 | 1.2x | +Omen, HealFollower, Storm |
| Established | 25,000 | 1.5x | +Curse, DivineVoice, Earthquake, Portal |
| Revered | 100,000 | 1.8x | +Volcano, Revelation, DemonInvasion |
| Exalted | 400,000 | 2.2x | +DivineBeastCreation, HolyWar |
| Ancient | 1,000,000 | 3.0x | Full power |

### Miracles — 15 phép màu, 3 tier
| Tier | Phép màu | Faith |
|------|---------|-------|
| 1 | Omen, Dream, Rain, HealFollower, BlessHarvest | 3-15 |
| 2 | DivineVoice, Curse, Storm, Earthquake, Portal | 20-50 |
| 3 | Revelation, DivineBeast, Volcano, DemonInvasion, HolyWar | 60-150 |

Counter System: rival god có N giây phản phép (N cấu hình được)

### NPC Social Hierarchy (GDD §10)
| Tier | Class | Faith/tick | Trust Req | Đặc điểm |
|------|-------|-----------|-----------|----------|
| 1 | Commoner | 0.01 | 20 | Dễ convert, oral tradition |
| 2 | Servant | 0.02 | 30 | Spy, blackmail, secret spreader |
| 3 | Adventurer | 0.05 | 40 | Champion path (Trust≥70 → Hero) |
| 4 | Noble | 0.15 | 60 | Territory, betrayal, marriage |
| 5 | Royalty | 0.50 | 80 | Kingdom-wide conversion |

### Government Types (GDD §11)
| Type | Điểm mạnh | Faith behavior |
|------|----------|---------------|
| Monarchy | Fast policy (1.5x) | Royal faith spreads by decree |
| Theocracy | Unity +60% | Priests dominate; schism risk 15% |
| Noble Council | Economic (1.2x) | Factional doctrine conflict |
| Tribal Clan | Military (1.4x) | Chief/Shaman authority |
| Merchant State | Economy (1.5x) | Faith follows profit |
| Monster Horde | Military (1.8x) | Strength gods spread instantly |

### Religion System (GDD §13)
**Doctrine Axes** (−100 đến +100, thay đổi theo events):
- Mercy ↔ Punishment → crime response, heresy trial
- Isolation ↔ Expansion → missionary speed 0.5x → 2.0x
- Harmony ↔ Dominion → race compatibility (Elves love Harmony, Orcs prefer Dominion)
- Freedom ↔ Order → royal/noble support
- Sacrifice ↔ Prosperity → disaster interpretation

**Religion Dynamics:** Schism (25% chance khi devotion<35%), Heresy (8% per 80 ticks), Crusade (devotion>70 + military>60)

### NPC Interaction Events (mỗi 10 ticks)
- Crime: Theft, Corruption Scandal, Assassination, Extortion, Tax Evasion
- Accidents: Crop Failure, Disease Outbreak, Building Collapse
- Social: Noble Marriage, Betrayal (Noble/Servant/Champion), Servant discovers secret
- Political: Rebellion, Coronation, Court deadlock (rival-god advisors)
- Luck: Lucky/Unlucky events với devotion bonus

### Organizations (6 loại)
- Kingdom, Royal Court (Chancellor/General/High Priest/Spymaster)
- Noble Houses (Head + family + servants, 3-7 per kingdom)
- Adventure Guild (cross-kingdom, Champion factory, Dungeon missions)
- Religious Institutions (High Priest → heresy trial, martyrdom)
- Underground Orgs (Fear gen cho dark gods, exposure risk)

### Dungeon & Relic & Memory (GDD §12, §7)
- 5 dungeon types: AncientRuins / MonstersLair / ForbiddenSanctum / LostTemple / DarkPortal
- Guild missions: party strength vs danger roll → success (relic+EXP) or death
- Relics: passive faith gen 2-12/tick, forgotten god survives via relics
- Memory decay: −5%/500 ticks, environmental events remembered by races

### AI Director (GDD §21)
- Age transitions: Early → Kingdom(t100) → Conflict(t300) → Collapse(t600) → Rebirth(t850)
- Age events: dungeons spawn, DarkPortals appear, civs collapse in Collapse Age
- Anti-stagnation: if no war → 15% chance natural disaster injected
- Anti-snowball: if one god >60% followers → weaker gods get faith boost

### Evolution System (GDD §15)
```
Creature: WildAnimal(100pts) → DivineBeast → CelestialGuardian [radius 15 tiles]
Hero:     HumanHero(150pts)  → Saint        → FallenDemonLord   [radius 15 tiles]
Monster:  Monster(120pts)    → Titan         → ApocalypticEntity  [radius 20 tiles]
```
No fixed morality: Demon → Redeemed Guardian (Light) or Archdemon (Dark)

### Multiplayer (GDD §18)
- 2-8 gods per world, indirect PvP qua faith và NPC
- Lobby: tạo phòng, private, ready system, countdown
- In-game Chat: message, whisper, 8 reactions
- Leaderboard ELO: rating, wins, followers, crusades, schisms
- Info visibility: public miracles visible to all, hidden cults unknown

### 6 Scenarios
| Scenario | Điều kiện thắng | Rule đặc biệt |
|----------|----------------|--------------|
| Standard | Flexible | — |
| TheLastLight | Light god survives 3 cycles | Light vs all |
| ReligionWars | First to 70% world followers | — |
| EvolutionRace | First Apex entity | Instant win |
| FaithCrisis | Survive 3 cycles | Faith gen × 0.2 |
| Apocalypse | Survive 2 cycles | Monster power × 3 |

### Conversion Formula (GDD §23)
```
ConversionChance = Openness × RaceAffinity × SocialPressure × TrustDifference
                 × RecentEvents × DoctrinePersonalityMatch
```
- Openness by tier: Commoner 0.8 → Royalty 0.15
- Government spread modifier: Theocracy 1.4x, MerchantState 0.8x

---

## 14. Thông Số Kỹ Thuật

| | |
|--|--|
| **Server** | ASP.NET Core 8, C#, SignalR WebSocket |
| **Database** | MongoDB 7.0 + Redis 7.2 |
| **Client** | Unity 2022.3 LTS (C#) |
| **Admin Panel** | Next.js 14, TypeScript, Tailwind CSS |
| **Auth** | JWT Bearer + Refresh Token Rotation |
| **Tick Rate** | 500ms/tick (configurable) |
| **Max players/world** | 8 gods |
| **Map size** | 64×64 tiles (configurable) |
| **Server .cs files** | 42 files |
| **Server services** | 28 services (interfaces) |
| **Unity scripts** | 27 .cs files |
| **Admin pages** | 16 trang |
| **Admin API calls** | 65 endpoints |
| **Unit tests** | 75 test cases |
| **CI/CD** | 4 GitHub Actions workflows |
| **Balance params** | 90 params runtime-tunable |
| **NPC tiers** | 5 (Commoner → Royalty) |
| **Organization types** | 6 |
| **Government types** | 6 |
| **Race types** | 8 |
| **God archetypes** | 8 |
| **Miracles** | 15 (3 tiers) |
| **Evolution stages** | 9 (3 paths × 3 stages) |
| **Doctrine axes** | 5 |
| **Believer types** | 5 |
| **Dungeon types** | 5 |
| **Relic types** | 8 |
| **God ranks** | 7 (Forgotten → Ancient) |
| **Asset cần thiết** | ~204 files (xem ASSETS.md) |

### Simulation Loop Ticks
| Service | Frequency |
|---------|-----------|
| FaithService | Every tick |
| CivilizationSimulationService | Every tick |
| ReligionService | Every 5 ticks |
| EvolutionService | Every 3 ticks |
| NPCInteractionService | Every 10 ticks |
| MemoryService (relic faith) | Every 10 ticks |
| OrganizationService | Every 20 ticks |
| AiDirectorService | Every 20 ticks |
| DungeonService | Every 50 ticks |
| GodRankService | Every 100 ticks |

---

## 15. Câu Hỏi Thường Gặp

**Q: Lỗi "Cannot connect to Docker daemon"?**  
A: Mở Docker Desktop lên, đợi icon cá voi hết loading.

**Q: Server báo "Unable to connect to MongoDB"?**  
A: `docker-compose up worldfaith-mongo worldfaith-redis -d`

**Q: Unity báo lỗi "HubConnection not found"?**  
A: Chưa cài SignalR DLLs — xem lại Bước 3 cài đặt Unity.

**Q: Menu WorldFaith không xuất hiện trong Unity?**  
A: Đợi Unity compile xong. Nếu vẫn không: Assets → Refresh (`Ctrl+R`).

**Q: Admin Panel báo 401 Unauthorized?**  
A: Token hết hạn → đăng xuất và đăng nhập lại. Kiểm tra file `.env.local`.

**Q: Chỉnh Balance Config có hiệu lực ngay không?**  
A: Có, sau tối đa 60 giây (cache TTL). Không cần restart server.

**Q: Chơi nhiều người trên mạng LAN?**  
A: Tìm IP máy chủ (Windows: `ipconfig`, Mac: `ifconfig`). Đổi Server URL trong Unity thành `http://192.168.x.x:5000/hubs/world`.

**Q: Reset toàn bộ dữ liệu?**
```bash
docker-compose down -v    # ⚠️ XÓA SẠCH không hoàn tác được
docker-compose up worldfaith-mongo worldfaith-redis -d
```

**Q: Cần bao nhiêu asset để chạy?**  
A: Tối thiểu: 8 tile textures + 47 SFX + 5 music. Xem [ASSETS.md](./ASSETS.md).

**Q: Forgotten God là gì?**  
A: Thần có 0 followers nhưng vẫn tồn tại nhờ relics (faith gen thụ động) hoặc hidden cults. Rank = Forgotten, faith gen × 0.1, tối đa 500 Faith. Nếu không còn relic/cult nào → eliminated.

**Q: Doctrine Axes thay đổi như thế nào?**  
A: Tự động qua events: FailedMiracle → shift về Mercy, HolyWarWon → shift về Punishment, Schism → branch chính trở nên rigid hơn. Admin cũng có thể chỉnh thủ công qua trang Religions.

---

*Cần hỗ trợ? Mở issue tại: https://github.com/thanhtinz/Game-new/issues*  
*WorldFaith v1.0 — "Players do not control the world. They influence belief, and belief controls the world."*
