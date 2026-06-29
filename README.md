# ⚡ WorldFaith — Hướng Dẫn Cài Đặt Từ A Đến Z

> **WorldFaith** là game simulation thần linh multiplayer — PC & Mobile.  
> Bạn đóng vai một vị thần: thu phục tín đồ, thực hiện phép màu, sáng lập tôn giáo, tiến hóa sinh vật.

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
14. [Câu hỏi thường gặp](#14-câu-hỏi-thường-gặp)

---

## 1. Yêu cầu — Cài gì trước?

Cài **4 công cụ** theo thứ tự này:

### 🔵 .NET SDK 8
Dùng để chạy server (ngôn ngữ C#).

1. Tải tại **https://dotnet.microsoft.com/download** → chọn **.NET 8.0**
2. Cài đặt bình thường

Kiểm tra:
```
dotnet --version   → phải thấy 8.x.x
```

---

### 🐳 Docker Desktop
Dùng để chạy MongoDB + Redis mà không cần cài tay.

1. Tải tại **https://www.docker.com/products/docker-desktop**
2. Cài đặt và **mở Docker Desktop lên** (icon cá voi ở taskbar)
3. Đợi đến khi thấy "Docker Desktop is running"

> ⚠️ **Phải mở Docker Desktop trước khi làm bất kỳ bước nào liên quan đến database.**

Kiểm tra:
```
docker --version   → phải thấy 24.x.x hoặc cao hơn
```

---

### 🟢 Node.js 20 LTS
Dùng để chạy Admin Panel.

1. Tải tại **https://nodejs.org** → chọn bản **LTS** (bên trái)
2. Cài đặt bình thường

Kiểm tra:
```
node --version   → phải thấy v20.x.x hoặc cao hơn
```

---

### 🎮 Unity 2022.3 LTS
Dùng để chạy và build game.

1. Tải **Unity Hub** tại **https://unity.com/download**
2. Mở Unity Hub → đăng nhập tài khoản (miễn phí)
3. Tab **Installs** → **Install Editor** → chọn **Unity 2022.3 LTS**
4. Trong danh sách modules, tích thêm:
   - **Android Build Support** (nếu muốn build Android)
   - **iOS Build Support** (nếu dùng Mac, muốn build iOS)
5. Cài đặt — mất khoảng 15-30 phút

---

## 2. Lấy code về máy

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
```

> Nếu chưa có Git: tải tại **https://git-scm.com** → cài → mở lại terminal.

---

## 3. Cài và chạy Database (MongoDB + Redis)

WorldFaith dùng 2 database:
- **MongoDB** — lưu dữ liệu game (worlds, gods, NPCs, religions, organizations...)
- **Redis** — cache nhanh (leaderboard, session, SignalR backplane)

Không cần cài thủ công — Docker xử lý tất cả.

### Khởi động database

Đảm bảo Docker Desktop đang chạy, sau đó:

```bash
cd Game-new
docker-compose up worldfaith-mongo worldfaith-redis -d
```

Lần đầu sẽ tải về MongoDB + Redis (~vài phút). Những lần sau khởi động ngay.

### Kiểm tra đang chạy

```bash
docker ps
```

Phải thấy 2 dòng trạng thái `Up`:
```
NAMES                 STATUS
worldfaith-mongo      Up 2 minutes
worldfaith-redis      Up 2 minutes
```

### Các lệnh quản lý database

```bash
# Dừng
docker-compose stop worldfaith-mongo worldfaith-redis

# Khởi động lại
docker-compose start worldfaith-mongo worldfaith-redis

# Xem logs MongoDB
docker logs worldfaith-mongo

# Backup dữ liệu
docker exec worldfaith-mongo mongodump --db worldfaith --archive=/tmp/backup.gz --gzip
docker cp worldfaith-mongo:/tmp/backup.gz ./backup-$(date +%Y%m%d).gz

# Khôi phục dữ liệu
docker cp backup-20240101.gz worldfaith-mongo:/tmp/restore.gz
docker exec worldfaith-mongo mongorestore --archive=/tmp/restore.gz --gzip
```

### Xem dữ liệu bằng giao diện (tùy chọn)

Tải **MongoDB Compass** tại https://www.mongodb.com/products/compass  
Kết nối: `mongodb://localhost:27017`  
Sau khi server chạy lần đầu sẽ thấy database `worldfaith`.

---

## 4. Cấu hình Server

File cấu hình: `Game-new/server/WorldFaith.Server/appsettings.json`

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

| Trường | Ý nghĩa | Cần đổi không? |
|--------|---------|--------------|
| `MongoDB` | Địa chỉ database | Không (Docker đã cấu hình) |
| `Redis` | Địa chỉ cache | Không (Docker đã cấu hình) |
| `Jwt.Secret` | Mã bí mật token | **Bắt buộc đổi khi deploy** |
| `Admin.Email` | Email tài khoản admin | Tùy chọn |
| `Admin.Password` | Mật khẩu admin | **Bắt buộc đổi khi deploy** |

> ⚠️ **Production:** Đổi `Jwt.Secret` thành chuỗi ngẫu nhiên ≥ 32 ký tự, ví dụ: `Xy7#mK9$pQ2@nL5&vR8^wJ3!cF6*hD4`

---

## 5. Chạy Server

```bash
cd Game-new/server/WorldFaith.Server
dotnet run
```

Lần đầu compile mất ~30-60 giây. Sau đó thấy:
```
[INF] WorldFaith Server khởi động
[INF] Balance config seeded (61 params)
[INF] Admin account seeded: admin@worldfaith.game
[INF] Now listening on: http://localhost:5000
```

**Kiểm tra:**  
Mở trình duyệt → `http://localhost:5000/health`  
Thấy `{"status":"ok"}` là thành công.

**Dừng server:** Nhấn `Ctrl + C`

**Chạy ngầm (Docker):**
```bash
docker-compose up -d   # khởi động tất cả: DB + Server
docker-compose down    # dừng tất cả
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

**Tạo file `.env.local` trên Windows (Notepad):**  
Mở Notepad → lưu với tên `.env.local` vào thư mục `Game-new/admin-panel/`

**Tạo file trên Mac/Linux:**
```bash
echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > Game-new/admin-panel/.env.local
```

### Chạy

```bash
cd Game-new/admin-panel
npm run dev
```

Mở trình duyệt: **http://localhost:3001**

**Đăng nhập:** `admin@worldfaith.game` / `Admin@WorldFaith2024!`

### 16 Trang Admin Panel

| Trang | Quản lý |
|-------|---------|
| **Dashboard** | Server health, 8 stat cards, active worlds realtime |
| **Events Log** | Toàn bộ sự kiện: crime, marriage, betrayal, miracle, rebellion... |
| **Worlds** | Tạo world, force-end, force-rebirth |
| **Maps & Tiles** | Visual map editor — click tile đổi biome, fertility, temple, Sacred |
| **Scenarios** | Thông tin 6 kịch bản game |
| **Gods** | Faith/Trust/Fear, unlock miracles, eliminate god |
| **NPCs** | 5 tier Commoner→Royalty, loyalty/ambition, kill/exile, Champion |
| **Mobs / Entities** | Evolve, spawn tại tọa độ, kill |
| **Civilizations** | Economy/Military, state, personality, boost, collapse |
| **Religions** | Followers/temples/devotion, force Schism, Erase |
| **Organizations** | Noble Houses, Guild, Underground — power/heat, expose, disband |
| **Players** | Ban/unban, reset password, promote/demote Admin |
| **Leaderboard** | Top players theo rating/wins/followers |
| **Balance Config** | 61 params game theo 8 nhóm, chỉnh inline, hiệu lực sau 60s |

---

## 7. Cài đặt Unity Client (Game)

### Bước 1 — Mở project

1. Mở **Unity Hub** → **Open** → **Add project from disk**
2. Chọn thư mục `Game-new/client-unity/`
3. Chọn **Unity 2022.3 LTS** → **Open**
4. Đợi Unity import (lần đầu 5-10 phút)

> Lỗi đỏ lúc đầu là bình thường — sẽ fix ở bước sau.

---

### Bước 2 — Cài packages

**Window → Package Manager → Unity Registry**

Cài lần lượt:

**TextMeshPro:**
1. Tìm "TextMeshPro" → **Install**
2. Sau khi xong: **Window → TextMeshPro → Import TMP Essential Resources**

**Newtonsoft Json:**
1. Nhấn **+** → **Add package by name**
2. Nhập: `com.unity.nuget.newtonsoft-json` → **Add**

**Mobile Notifications** (cho Android/iOS push notifications):
1. Nhấn **+** → **Add package by name**
2. Nhập: `com.unity.mobile.notifications` → **Add**

---

### Bước 3 — Cài SignalR (kết nối realtime với server)

SignalR không có trong Package Manager, phải cài thủ công.

**Cách tự động (Mac/Linux):**

Tạo file `install-signalr.sh`:
```bash
#!/bin/bash
DST="Game-new/client-unity/Assets/Plugins/SignalR"
mkdir -p "$DST"
cd /tmp && mkdir signalr_tmp && cd signalr_tmp
dotnet new console -n sr && cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release
cp bin/Release/net8.0/publish/Microsoft.AspNetCore.SignalR.*.dll "../../../../$DST/"
cp bin/Release/net8.0/publish/Microsoft.AspNetCore.Http.Connections*.dll "../../../../$DST/"
cd /tmp && rm -rf signalr_tmp
echo "✅ SignalR installed!"
```
```bash
bash install-signalr.sh
```

**Cách thủ công (Windows):**

1. Tải từ: `https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/8.0.0`
2. Đổi đuôi `.nupkg` → `.zip` rồi giải nén
3. Vào `lib/netstandard2.1/` bên trong
4. Copy các file `.dll` vào `Game-new/client-unity/Assets/Plugins/SignalR/`:
   - `Microsoft.AspNetCore.SignalR.Client.dll`
   - `Microsoft.AspNetCore.SignalR.Client.Core.dll`
   - `Microsoft.AspNetCore.SignalR.Common.dll`
   - `Microsoft.AspNetCore.Http.Connections.Client.dll`
   - `Microsoft.AspNetCore.Http.Connections.Common.dll`

---

### Bước 4 — Liên kết Shared Library

Shared Library chứa code dùng chung giữa server và Unity.

**Windows (PowerShell):**
```powershell
$src = "Game-new/shared/WorldFaith.Shared"
$dst = "Game-new/client-unity/Assets/WorldFaith/Shared"
New-Item -ItemType Directory -Force -Path $dst
Copy-Item "$src/Enums"     $dst -Recurse -Force
Copy-Item "$src/Models"    $dst -Recurse -Force
Copy-Item "$src/Contracts" $dst -Recurse -Force
```

**Mac/Linux:**
```bash
SRC="Game-new/shared/WorldFaith.Shared"
DST="Game-new/client-unity/Assets/WorldFaith/Shared"
mkdir -p "$DST"
cp -r "$SRC/Enums" "$SRC/Models" "$SRC/Contracts" "$DST/"
```

Đợi Unity import xong (thanh loading dưới phải hết).

---

### Bước 5 — Tạo Scenes

**LoginScene:**
1. **File → New Scene → Basic** → lưu tên `LoginScene` vào `Assets/Scenes/`
2. Menu: **WorldFaith → Setup → Create Login Scene Objects**

**LobbyScene:**
1. New Scene → lưu `LobbyScene`
2. **WorldFaith → Setup → Create Lobby Scene Objects**

**GameScene:**
1. New Scene → lưu `GameScene`
2. **WorldFaith → Setup → Create Game Scene Objects**

> Menu **WorldFaith** xuất hiện sau khi Shared Library import thành công.

---

### Bước 6 — Cấu hình Server URL

Trong mỗi Scene, tìm các GameObjects và điền địa chỉ server:

| GameObject | Field | Giá trị (development) |
|------------|-------|----------------------|
| `WorldFaithClient` | Server Url | `http://localhost:5000/hubs/world` |
| `LobbyClient` | Server Url | `http://localhost:5000/hubs/lobby` |
| `ChatClient` | Server Url | `http://localhost:5000/hubs/chat` |

> Khi deploy production: đổi `localhost:5000` thành domain thật.

---

### Bước 7 — Validate setup

**WorldFaith → Validate → Check All Managers**

Tất cả ✅ là setup đúng. Nếu có ⚠️, đọc Console để biết thiếu gì.

---

## 8. Chuẩn bị Asset

> 📄 **Xem danh sách đầy đủ trong file [ASSETS.md](./ASSETS.md)**

Đây là các asset **bắt buộc** để game hoạt động bình thường:

### 🔴 Ưu tiên cao (game không đẹp/hoạt động thiếu)

**Tile Textures (8 file) — `Assets/WorldFaith/World/Tiles/`**
```
tile_grassland.png    tile_forest.png    tile_mountain.png   tile_desert.png
tile_tundra.png       tile_water.png     tile_volcano.png    tile_sacred.png
```
Kích thước: 64×64 hoặc 128×128 px. Màu sắc tham chiếu trong ASSETS.md.

**SFX (47 file) — `Assets/WorldFaith/Audio/SFX/`**  
Gán vào `AudioManager.sfxClips[]` theo thứ tự `SfxId` enum.  
Danh sách đầy đủ trong [ASSETS.md](./ASSETS.md#1-audio--sfx-47-file).

**Nhạc Nền (5 file) — `Assets/WorldFaith/Audio/Music/`**
```
music_base.mp3   music_religion.mp3   music_war.mp3   music_apocalypse.mp3   music_victory.mp3
```
Gán vào AudioManager: `musicBase`, `musicReligion`, `musicWar`, `musicApocalypse`, `musicVictory`.

### 🟡 Ưu tiên trung bình (game chạy được, thiếu visual)

**VFX Prefabs (28 file) — `Assets/WorldFaith/VFX/Prefabs/`**  
Tạo Particle System prefab cho mỗi VfxId. Danh sách trong ASSETS.md.

**Archetype Icons (8 file) — `Assets/WorldFaith/UI/Sprites/`**  
`arch_order.png`, `arch_chaos.png`, `arch_light.png`, `arch_darkness.png`,  
`arch_nature.png`, `arch_death.png`, `arch_knowledge.png`, `arch_war.png`

**Miracle Icons (15 file) — `Assets/WorldFaith/UI/Sprites/`**  
`miracle_rain.png` đến `miracle_holy_war.png` — xem danh sách trong ASSETS.md.

**Fonts (4 file) — `Assets/WorldFaith/UI/Fonts/`**  
Sau khi copy .ttf vào: **Window → TextMeshPro → Font Asset Creator** → tạo font asset.

### 🟢 Ưu tiên thấp (cosmetic, có thể làm sau)

VFX NPC events, animation clips, ScriptableObject configs, entity icons.

### Nguồn tải miễn phí

| Loại | Nguồn |
|------|-------|
| SFX | https://freesound.org / https://kenney.nl / https://zapsplat.com |
| Nhạc | https://incompetech.com / https://freemusicarchive.org |
| Sprites/Icons | https://kenney.nl / https://game-icons.net |
| Fonts | https://fonts.google.com (Cinzel, Nunito, Rajdhani) |

---

## 9. Build Game Ra File

### Build PC (Windows/Mac/Linux)

1. **File → Build Settings**
2. Kéo 3 scenes vào **Scenes In Build** theo thứ tự:
   ```
   0: Assets/Scenes/LoginScene
   1: Assets/Scenes/LobbyScene
   2: Assets/Scenes/GameScene
   ```
3. Platform: **PC, Mac & Linux Standalone**
4. **Build** → chọn thư mục xuất

### Build Android

1. **File → Build Settings** → chọn **Android** → **Switch Platform**
2. **Player Settings**:
   - **Company Name:** tên của bạn
   - **Product Name:** WorldFaith
   - **Package Name:** `com.yourname.worldfaith`
   - **Minimum API Level:** Android 8.0 (API 26)
3. **Build** → tạo file `.apk`

**Cài lên điện thoại:**
- Settings → About Phone → nhấn **Build Number** 7 lần → bật Developer Mode
- Settings → Developer Options → bật **USB Debugging**
- Kết nối USB → **Build And Run** trong Unity

### Build iOS (chỉ trên Mac)

1. **File → Build Settings** → **iOS** → **Switch Platform**
2. **Build** → Unity tạo Xcode project
3. Mở Xcode → chọn Apple Developer Team → **Build & Run**

---

## 10. Kiểm tra Hoạt Động

Kiểm tra theo thứ tự:

**① Database đang chạy:**
```bash
docker ps   # phải thấy worldfaith-mongo và worldfaith-redis đều Up
```

**② Server đang chạy:**  
Trình duyệt → `http://localhost:5000/health` → thấy `{"status":"ok"}`

**③ Admin Panel đang chạy:**  
Trình duyệt → `http://localhost:3001` → thấy trang đăng nhập

**④ Unity kết nối được:**
1. Nhấn **Play** (▶️) trong Unity Editor
2. Thấy màn hình Login → thử đăng ký tài khoản mới
3. Đăng nhập thành công → vào được Lobby

**⑤ Tạo phòng và chơi thử:**
1. Tạo phòng (chọn scenario Standard)
2. Dùng 2 tab/máy để join cùng phòng
3. Start game → world sinh ra → kiểm tra Gods, Civs trong Admin Panel

---

## 11. Deploy Lên Server Thật

### Yêu cầu VPS

| Người chơi | CPU | RAM | Băng thông |
|-----------|-----|-----|-----------|
| 2-10 | 2 core | 4 GB | 10 Mbps |
| 10-30 | 4 core | 8 GB | 20 Mbps |
| 30+ | 8 core | 16 GB | 50 Mbps |

OS khuyên dùng: **Ubuntu 22.04 LTS**

### Cài Docker trên VPS

```bash
sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER
# Đăng xuất và đăng nhập lại
```

### Upload và chạy

```bash
# Upload code
scp -r Game-new/ user@YOUR_SERVER_IP:/opt/worldfaith/

# SSH vào server
ssh user@YOUR_SERVER_IP
cd /opt/worldfaith

# Tạo file biến môi trường production
cat > .env << 'EOF'
JWT_SECRET=thay_bang_chuoi_ngau_nhien_32_ky_tu_tro_len
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=MatKhauManh@2024!
EOF

# Khởi động
docker-compose up -d

# Kiểm tra
curl http://localhost:5000/health
```

### Cài Nginx (reverse proxy + WebSocket)

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
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_read_timeout 86400;
    }
}
EOF

sudo ln -s /etc/nginx/sites-available/worldfaith /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

### Cài HTTPS miễn phí

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
```

### Cập nhật Server URL trong Unity

Sau khi có domain, đổi trong Unity:
```
http://localhost:5000/hubs/world  →  https://api.yourdomain.com/hubs/world
```

---

## 12. Tài Khoản Mặc Định

Server tự tạo tài khoản admin khi khởi động lần đầu:

| | Mặc định |
|--|--|
| **Email** | `admin@worldfaith.game` |
| **Mật khẩu** | `Admin@WorldFaith2024!` |
| **Quyền** | Admin (toàn quyền) |

> ⚠️ **Đổi mật khẩu trước khi cho người khác chơi!**  
> Sửa trong `appsettings.json` → `Admin.Password` → restart server.

---

## 13. Tính Năng Đầy Đủ

### Gameplay

**Faith Economy**
- Faith tăng từ followers (Tier 1-5), temples, devotion, Fear (dark gods)
- 8 archetype với bonus riêng: Light (HealFollower miễn phí), Darkness (Curse x2), War (HolyWar -30%)...
- Max Faith, rate cấu hình qua Admin Panel → Balance Config

**15 Miracles — 3 Tier**

| Tier | Miracles | Faith |
|------|---------|-------|
| 1 | Omen, Dream, Rain, HealFollower, BlessHarvest | 3-15 |
| 2 | DivineVoice, Curse, Storm, Earthquake, Portal | 20-50 |
| 3 | Revelation, DivineBeast, Volcano, DemonInvasion, HolyWar | 60-150 |

**Counter System** — Rival god có N giây để phản phép (N cấu hình được)

**God Commandment (8 loại lệnh)**  
Phát lệnh cho Civilization: ExpandTerritory, BuildTemple, SpreadFaith, MakeWar, MakePeace, FocusEconomy, FocusMilitary, Worship  
Civ nghe hay không phụ thuộc Trust level.

**Religion System**
- Sáng lập tôn giáo public hoặc secret cult
- Tự spread theo devotion + temples
- Schism (tách đôi), Heresy (dị giáo), Crusade (thánh chiến)

**Evolution System — 3 nhánh, 9 stages**
```
WildAnimal (100pts) → DivineBeast → CelestialGuardian
HumanHero  (150pts) → Saint       → FallenDemonLord
Monster    (120pts) → Titan       → ApocalypticEntity
```
Apex entities ảnh hưởng bán kính 15-20 tiles quanh vị trí.

**NPC Social Hierarchy (v3)**
- 5 tầng: Commoner → Servant → Adventurer → Noble → Royalty
- Named NPCs (Tier 3-5) với loyalty, ambition, piety, relationships
- Adventurer có thể trở thành Champion của god (Trust ≥ 70)

**NPC Interaction Events (mỗi 10 ticks)**
- Crime: Theft, Corruption Scandal, Assassination, Extortion
- Accidents: Crop Failure, Disease, Building Collapse
- Social: Marriage, Betrayal, Secret discovered
- Political: Rebellion, Coronation
- Luck: Lucky/Unlucky events với devotion bonus

**6 Organizations**
- Kingdom, Royal Court (Chancellor/General/High Priest/Spymaster)
- Noble Houses (Head + family + servants)
- Adventure Guild (cross-kingdom, Champion factory)
- Religious Institutions (High Priest → heresy trial)
- Underground Orgs (Fear generation for dark gods)

**AI Civilization — 5 personality, lifecycle Tribal→Empire→Fallen**

**Procedural World Generation** — Perlin Noise 3-layer, 8 biomes, island gradient

**6 Scenarios:** Standard, TheLastLight, ReligionWars, EvolutionRace, FaithCrisis, Apocalypse

### Multiplayer

- 2-8 gods per world, indirect PvP
- Lobby: tạo phòng, private room, ready system, countdown
- In-game God Chat: message, whisper, 8 reactions
- Leaderboard ELO: rating, wins, followers, crusades, schisms

### Admin Panel — 16 trang

Dashboard, Events Log, Worlds, Maps & Tiles, Scenarios, Gods, NPCs, Mobs, Civilizations, Religions, Organizations, Players, Leaderboard, Balance Config

---

## 14. Câu Hỏi Thường Gặp

**Q: Lỗi "Cannot connect to Docker daemon"?**  
A: Mở Docker Desktop lên, đợi icon cá voi hết loading, thử lại.

**Q: Server báo "Unable to connect to MongoDB"?**  
A: Database chưa chạy. Chạy: `docker-compose up worldfaith-mongo worldfaith-redis -d`

**Q: Unity báo lỗi "HubConnection not found"?**  
A: Chưa cài SignalR DLLs. Xem lại [Bước 3 cài đặt Unity](#bước-3--cài-signalr-kết-nối-realtime-với-server).

**Q: Menu WorldFaith không xuất hiện trong Unity?**  
A: Đợi Unity compile xong (thanh loading dưới phải). Nếu vẫn không: **Assets → Refresh** (`Ctrl+R`).

**Q: Admin Panel báo 401 Unauthorized?**  
A: Token hết hạn → đăng xuất và đăng nhập lại. Hoặc kiểm tra file `.env.local`.

**Q: Chỉnh Balance Config trong Admin Panel có hiệu lực ngay không?**  
A: Có, sau tối đa 60 giây (cache TTL). Không cần restart server.

**Q: Chơi nhiều người trên mạng LAN?**  
A: Tìm IP máy chủ (Windows: `ipconfig`, Mac: `ifconfig`).  
Đổi Server URL trong Unity thành `http://192.168.x.x:5000/hubs/world`.

**Q: Muốn reset toàn bộ dữ liệu?**
```bash
docker-compose down -v    # ⚠️ XÓA SẠCH dữ liệu, không khôi phục được!
docker-compose up worldfaith-mongo worldfaith-redis -d
```

**Q: Cần bao nhiêu asset để game chạy được?**  
A: Xem file [ASSETS.md](./ASSETS.md) để biết danh sách đầy đủ và thứ tự ưu tiên. Tối thiểu cần: 8 tile textures, 47 SFX, 5 music files.

---

## Thông Tin Kỹ Thuật

| | |
|--|--|
| **Server** | ASP.NET Core 8, C#, SignalR WebSocket |
| **Database** | MongoDB 7.0 + Redis 7.2 |
| **Client** | Unity 2022.3 LTS (C#) |
| **Admin Panel** | Next.js 14, TypeScript, Tailwind CSS |
| **Auth** | JWT Bearer + Refresh Token |
| **Tick Rate** | 500ms/tick (configurable) |
| **Max players/world** | 8 gods |
| **Map size** | 64×64 tiles (configurable) |
| **Server files** | 33 .cs files |
| **Unity scripts** | 27 .cs files |
| **Admin pages** | 16 trang |
| **Unit tests** | 30+ test cases |
| **CI/CD** | 4 GitHub Actions workflows |
| **Balance params** | 61 params runtime-tunable |
| **NPC types** | 5 tầng, 6 personality, 6 org types |
| **Asset cần thiết** | ~204 files (xem ASSETS.md) |

---

*Cần hỗ trợ? Mở issue tại: https://github.com/thanhtinz/Game-new/issues*  
*WorldFaith v1.0 — "Players do not control the world. They influence belief, and belief controls the world."*
