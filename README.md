# ⚡ WorldFaith — Hướng Dẫn Cài Đặt & Vận Hành

> **WorldFaith** là game god simulation sandbox multiplayer.  
> Bạn đóng vai một vị thần: thu phục tín đồ, thực hiện phép màu, sáng lập tôn giáo, tiến hóa sinh vật.  
> *"Players do not control the world. They influence belief, and belief controls the world."*

---

## 📋 Mục lục

1. [Yêu cầu — Cài gì trước?](#1-yêu-cầu--cài-gì-trước)
2. [Lấy code về máy](#2-lấy-code-về-máy)
3. [Cài và chạy Database](#3-cài-và-chạy-database)
4. [Cấu hình Server](#4-cấu-hình-server)
5. [Chạy Server](#5-chạy-server)
6. [Admin Panel](#6-admin-panel)
7. [Cài đặt Unity Client](#7-cài-đặt-unity-client)
8. [Chuẩn bị Asset](#8-chuẩn-bị-asset)
9. [Build Game](#9-build-game)
10. [Kiểm tra hoạt động](#10-kiểm-tra-hoạt-động)
11. [Deploy Production](#11-deploy-production)
12. [Tài khoản mặc định](#12-tài-khoản-mặc-định)
13. [Hướng dẫn sử dụng Admin Panel](#13-hướng-dẫn-sử-dụng-admin-panel)
14. [Hướng dẫn cơ chế game](#14-hướng-dẫn-cơ-chế-game)
15. [Thông số kỹ thuật](#15-thông-số-kỹ-thuật)
16. [Câu hỏi thường gặp](#16-câu-hỏi-thường-gặp)

---

## 1. Yêu cầu — Cài gì trước?

Cài **4 công cụ** theo đúng thứ tự:

### 🔵 .NET SDK 8
```bash
# Tải: https://dotnet.microsoft.com/download → chọn .NET 8.0 → cài bình thường
dotnet --version   # Kiểm tra: phải thấy 8.x.x
```

### 🐳 Docker Desktop
```bash
# Tải: https://www.docker.com/products/docker-desktop → cài → MỞ LÊN và đợi icon cá voi hết loading
docker --version   # Kiểm tra: phải thấy 24.x.x+
```
> ⚠️ **Phải mở Docker Desktop trước** khi làm bất kỳ bước nào liên quan database.

### 🟢 Node.js 20 LTS
```bash
# Tải: https://nodejs.org → chọn bản LTS (bên trái) → cài bình thường
node --version     # Kiểm tra: phải thấy v20.x.x+
```

### 🎮 Unity 2022.3 LTS
```
1. Tải Unity Hub: https://unity.com/download
2. Mở Unity Hub → đăng nhập → tab Installs → Install Editor
3. Chọn Unity 2022.3 LTS
4. Trong danh sách modules, tích thêm Android Build Support (nếu muốn build Android)
5. Nhấn Install — mất 15-30 phút
```

---

## 2. Lấy code về máy

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
```

Nếu chưa có Git: tải tại **https://git-scm.com** → cài → mở terminal mới → thử lại.

---

## 3. Cài và chạy Database

WorldFaith dùng MongoDB (lưu dữ liệu game) và Redis (cache realtime). Docker xử lý cả hai.

```bash
# Đảm bảo Docker Desktop đang chạy, sau đó:
docker-compose up worldfaith-mongo worldfaith-redis -d
```

Lần đầu sẽ tải image (~3-5 phút). Kiểm tra:
```bash
docker ps
```
Phải thấy **2 dòng** trạng thái `Up`:
```
NAMES                 STATUS
worldfaith-mongo      Up 2 minutes
worldfaith-redis      Up 2 minutes
```

### Lệnh quản lý database hàng ngày
```bash
# Dừng database (khi không dùng)
docker-compose stop worldfaith-mongo worldfaith-redis

# Khởi động lại
docker-compose start worldfaith-mongo worldfaith-redis

# Xem log nếu có lỗi
docker logs worldfaith-mongo --tail 20

# Backup dữ liệu
docker exec worldfaith-mongo mongodump --db worldfaith --archive=/tmp/bk.gz --gzip
docker cp worldfaith-mongo:/tmp/bk.gz ./backup-$(date +%Y%m%d).gz

# Khôi phục từ backup
docker cp my-backup.gz worldfaith-mongo:/tmp/restore.gz
docker exec worldfaith-mongo mongorestore --archive=/tmp/restore.gz --gzip
```

**Xem dữ liệu bằng giao diện (tùy chọn):** Tải MongoDB Compass tại https://www.mongodb.com/products/compass → kết nối `mongodb://localhost:27017`.

---

## 4. Cấu hình Server

Mở file `server/WorldFaith.Server/appsettings.json` và chỉnh các giá trị:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "Redis":   "localhost:6379"
  },
  "Jwt": {
    "Secret":              "WorldFaith_SuperSecret_Key_MustBeAtLeast32Chars!",
    "Issuer":              "WorldFaith",
    "Audience":            "WorldFaithPlayers",
    "AccessTokenMinutes":  "60",
    "RefreshTokenDays":    "30"
  },
  "Admin": {
    "Email":    "admin@worldfaith.game",
    "Password": "Admin@WorldFaith2024!"
  }
}
```

| Trường | Mô tả | Cần đổi? |
|--------|-------|---------|
| `MongoDB` | Địa chỉ database | Không (Docker đã cấu hình) |
| `Redis` | Địa chỉ cache | Không |
| `Jwt.Secret` | Khóa bí mật token | **Bắt buộc đổi khi deploy** |
| `Admin.Email` | Email tài khoản admin | Tùy chọn |
| `Admin.Password` | Mật khẩu admin | **Bắt buộc đổi khi deploy** |

> Tạo Jwt.Secret ngẫu nhiên ví dụ: `Xy7#mK9$pQ2@nL5&vR8^wJ3!cF6*hD4`

---

## 5. Chạy Server

```bash
cd Game-new/server/WorldFaith.Server
dotnet run
```

Compile lần đầu mất 30-60 giây. Khi thấy các dòng này là server đang chạy:
```
[INF] WorldFaith Server khởi động
[INF] Balance config seeded (90 params)
[INF] Admin account seeded: admin@worldfaith.game
[INF] Now listening on: http://localhost:5000
```

**Kiểm tra:** Mở trình duyệt → `http://localhost:5000/health` → thấy `{"status":"ok"}`

**Dừng server:** Nhấn `Ctrl + C`

**Chạy ngầm bằng Docker:**
```bash
docker-compose up -d        # Khởi động DB + Server tất cả
docker-compose logs -f      # Xem log realtime
docker-compose down         # Dừng tất cả
```

---

## 6. Admin Panel

### Cài đặt lần đầu
```bash
cd Game-new/admin-panel
npm install
```

Tạo file `.env.local` trong thư mục `admin-panel/`:

**Mac/Linux:**
```bash
echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > .env.local
```

**Windows (Notepad):** Mở Notepad → gõ `NEXT_PUBLIC_API_URL=http://localhost:5000` → Lưu với tên `.env.local` vào thư mục `admin-panel/`

### Chạy
```bash
cd Game-new/admin-panel
npm run dev
```

Mở trình duyệt: **http://localhost:3001**

Đăng nhập: `admin@worldfaith.game` / `Admin@WorldFaith2024!`

---

## 7. Cài đặt Unity Client

### Bước 1 — Mở project trong Unity
```
1. Mở Unity Hub
2. Nhấn Open → Add project from disk
3. Chọn thư mục: Game-new/client-unity/
4. Chọn Unity 2022.3 LTS → Open
5. Đợi Unity import (lần đầu 5-10 phút — lỗi đỏ ban đầu là bình thường)
```

### Bước 2 — Cài Packages
Vào **Window → Package Manager → Unity Registry**, cài lần lượt:

**TextMeshPro:**
1. Tìm "TextMeshPro" → Install
2. Sau khi xong: **Window → TextMeshPro → Import TMP Essential Resources** → Import

**Newtonsoft JSON:**
1. Nhấn dấu `+` góc trên trái → **Add package by name**
2. Nhập `com.unity.nuget.newtonsoft-json` → Add

**Mobile Notifications** (cho Android/iOS):
1. Nhấn dấu `+` → **Add package by name**
2. Nhập `com.unity.mobile.notifications` → Add

### Bước 3 — Cài SignalR (kết nối realtime với server)

**Mac/Linux — chạy script tự động:**
```bash
cd Game-new
DST="client-unity/Assets/Plugins/SignalR"
mkdir -p "$DST"
cd /tmp && mkdir signalr_tmp && cd signalr_tmp
dotnet new console -n sr --no-restore -o sr && cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release -o pub --no-restore
cp pub/Microsoft.AspNetCore.SignalR.*.dll     "$(cd - && pwd)/$DST/"
cp pub/Microsoft.AspNetCore.Http.Connections*.dll "$(cd - && pwd)/$DST/"
cd /tmp && rm -rf signalr_tmp
echo "SignalR installed!"
```

**Windows — thủ công:**
1. Vào: `https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/8.0.0`
2. Nhấn **Download package** → tải về file `.nupkg`
3. Đổi đuôi `.nupkg` thành `.zip` → giải nén
4. Vào thư mục `lib/netstandard2.1/` bên trong
5. Copy toàn bộ file `.dll` vào `Game-new/client-unity/Assets/Plugins/SignalR/` (tạo thư mục nếu chưa có)

### Bước 4 — Link Shared Library (code dùng chung server và Unity)

**Mac/Linux:**
```bash
cd Game-new
SRC="shared/WorldFaith.Shared"
DST="client-unity/Assets/WorldFaith/Shared"
mkdir -p "$DST"
cp -r "$SRC/Enums" "$SRC/Models" "$SRC/Contracts" "$DST/"
```

**Windows (PowerShell):**
```powershell
$src = "Game-new\shared\WorldFaith.Shared"
$dst = "Game-new\client-unity\Assets\WorldFaith\Shared"
New-Item -ItemType Directory -Force -Path $dst
Copy-Item "$src\Enums","$src\Models","$src\Contracts" $dst -Recurse -Force
```

Đợi Unity import xong (thanh loading ở góc dưới phải hết).

### Bước 5 — Tạo Scenes
```
File → New Scene → Basic (Built-in)
  → Lưu với tên LoginScene vào Assets/Scenes/
  → Menu WorldFaith → Setup → Create Login Scene Objects

File → New Scene → Basic (Built-in)
  → Lưu với tên LobbyScene vào Assets/Scenes/
  → Menu WorldFaith → Setup → Create Lobby Scene Objects

File → New Scene → Basic (Built-in)
  → Lưu với tên GameScene vào Assets/Scenes/
  → Menu WorldFaith → Setup → Create Game Scene Objects
```

> Menu WorldFaith xuất hiện sau khi Shared Library import thành công.

### Bước 6 — Cấu hình địa chỉ server

Trong mỗi Scene, tìm GameObjects và điền địa chỉ server:

| GameObject | Field | Giá trị (development) |
|------------|-------|----------------------|
| `WorldFaithClient` | Server Url | `http://localhost:5000/hubs/world` |
| `LobbyClient` | Server Url | `http://localhost:5000/hubs/lobby` |
| `ChatClient` | Server Url | `http://localhost:5000/hubs/chat` |

Khi deploy lên server thật, đổi `localhost:5000` thành domain của bạn.

### Bước 7 — Kiểm tra setup
```
Menu WorldFaith → Validate → Check All Managers
```
Tất cả ✅ là đúng. Nếu có ⚠️, đọc Console để biết thiếu gì.

---

## 8. Chuẩn bị Asset

Xem danh sách đầy đủ tại **[ASSETS.md](./ASSETS.md)** (~204 files).

### 🔴 Bắt buộc — game không render được nếu thiếu

**8 Tile Textures** — đặt vào `Assets/WorldFaith/World/Tiles/` (64×64 px PNG):
```
tile_grassland.png   tile_forest.png   tile_mountain.png   tile_desert.png
tile_tundra.png      tile_water.png    tile_volcano.png    tile_sacred.png
```
Màu tham khảo: Grassland `#4a9c2f`, Forest `#1a5c1a`, Mountain `#7a7a7a`, Desert `#c8b44a`, Tundra `#b0c8e0`, Water `#2a64c8`, Volcano `#c83210`, Sacred `#c8a832`

**47 SFX** — đặt vào `Assets/WorldFaith/Audio/SFX/`  
Sau khi copy vào, gán vào `AudioManager.sfxClips[]` **theo đúng thứ tự SfxId enum** (xem Inspector → AudioManager → Custom Editor).

**5 Music layers** — đặt vào `Assets/WorldFaith/Audio/Music/`:
```
music_base.mp3        → gán vào AudioManager.musicBase
music_religion.mp3    → gán vào AudioManager.musicReligion
music_war.mp3         → gán vào AudioManager.musicWar
music_apocalypse.mp3  → gán vào AudioManager.musicApocalypse
music_victory.mp3     → gán vào AudioManager.musicVictory
```

### 🟡 Khuyến nghị — game trông đẹp hơn
- **28 VFX Prefabs** → `Assets/WorldFaith/VFX/Prefabs/` — gán vào `VfxManager.catalog[]`
- **8 Archetype Icons** (128×128 px) → `Assets/WorldFaith/UI/Sprites/`
- **15 Miracle Icons** (64×64 px) → `Assets/WorldFaith/UI/Sprites/`
- **4 Fonts** — tải từ fonts.google.com (Cinzel, Nunito, Rajdhani) → copy `.ttf` → tạo Font Asset qua Window → TextMeshPro → Font Asset Creator

### Nguồn tải miễn phí
| Loại | Nguồn |
|------|-------|
| SFX | freesound.org, kenney.nl, zapsplat.com |
| Music | incompetech.com, freemusicarchive.org |
| Sprites & Icons | kenney.nl, game-icons.net |
| Fonts | fonts.google.com |

---

## 9. Build Game

### PC (Windows / Mac / Linux)
```
1. File → Build Settings
2. Kéo 3 scenes vào ô Scenes In Build theo đúng thứ tự:
     0: Assets/Scenes/LoginScene
     1: Assets/Scenes/LobbyScene
     2: Assets/Scenes/GameScene
3. Platform: chọn PC, Mac & Linux Standalone
4. Nhấn Build → chọn thư mục xuất → đợi build xong
```

### Android
```
1. File → Build Settings → chọn Android → Switch Platform (đợi vài phút)
2. Nhấn Player Settings, điền:
     Company Name: tên của bạn
     Product Name: WorldFaith
     Package Name: com.tenban.worldfaith
     Minimum API Level: Android 8.0 (API 26)
3. Cắm điện thoại Android vào máy tính
4. Settings điện thoại → About Phone → nhấn Build Number 7 lần → bật Developer Mode
5. Settings → Developer Options → bật USB Debugging
6. Unity: nhấn Build And Run → chọn thư mục → cài trực tiếp lên điện thoại
```

### iOS (chỉ trên Mac)
```
1. File → Build Settings → iOS → Switch Platform
2. Nhấn Build → Unity tạo Xcode project
3. Mở project trong Xcode → chọn Apple Developer Team → Build & Run
```

---

## 10. Kiểm tra hoạt động

Thực hiện theo thứ tự:

**① Database:**
```bash
docker ps   # Phải thấy worldfaith-mongo và worldfaith-redis đều Up
```

**② Server:**
```bash
curl http://localhost:5000/health   # Phải thấy: {"status":"ok"}
```

**③ Admin Panel:**
```
Mở http://localhost:3001 → phải thấy màn hình đăng nhập → đăng nhập thành công
```

**④ Unity:**
```
1. Nhấn Play (▶️) trong Unity Editor
2. Màn hình Login xuất hiện → đăng ký tài khoản mới → đăng nhập
3. Vào Lobby → tạo phòng → Start game
4. World khởi động → kiểm tra Admin Panel: Dashboard phải thấy Active Worlds = 1
```

---

## 11. Deploy Production

### Yêu cầu VPS

| Số người chơi | CPU | RAM | Băng thông |
|--------------|-----|-----|-----------|
| 2-10 | 2 core | 4 GB | 10 Mbps |
| 10-30 | 4 core | 8 GB | 20 Mbps |
| 30+ | 8 core | 16 GB | 50 Mbps |

OS khuyến nghị: Ubuntu 22.04 LTS

### Bước 1 — Cài Docker trên VPS
```bash
ssh user@YOUR_VPS_IP
sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER
# Đăng xuất và đăng nhập lại để áp dụng group
exit
ssh user@YOUR_VPS_IP
docker --version   # Kiểm tra
```

### Bước 2 — Upload code
```bash
# Từ máy local
scp -r Game-new/ user@YOUR_VPS_IP:/opt/worldfaith/
ssh user@YOUR_VPS_IP
cd /opt/worldfaith
```

### Bước 3 — Tạo file biến môi trường production
```bash
cat > .env << 'EOF'
JWT_SECRET=Thay_Bang_Chuoi_Ngau_Nhien_32_Ky_Tu_Tro_Len_Khong_Dung_Cai_Nay
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=MatKhauManh@2024!
EOF
```

### Bước 4 — Khởi động
```bash
docker-compose up -d
docker-compose logs -f   # Theo dõi log, đợi thấy "Now listening on"
curl http://localhost:5000/health   # Kiểm tra
```

### Bước 5 — Cài Nginx làm reverse proxy (để WebSocket hoạt động)
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
        proxy_read_timeout 86400;
    }
}
EOF

sudo ln -s /etc/nginx/sites-available/worldfaith /etc/nginx/sites-enabled/
sudo nginx -t          # Kiểm tra config không lỗi
sudo systemctl reload nginx
```

### Bước 6 — Cài HTTPS miễn phí (Let's Encrypt)
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
# Làm theo hướng dẫn, nhập email, chọn Yes để redirect HTTP→HTTPS
```

### Bước 7 — Cập nhật Unity
```
Mở Unity → tìm 3 GameObjects WorldFaithClient, LobbyClient, ChatClient
Đổi Server Url từ:
  http://localhost:5000/hubs/world
Thành:
  https://api.yourdomain.com/hubs/world
(tương tự cho lobby và chat)
Build lại game → phát hành
```

---

## 12. Tài khoản mặc định

Server tự tạo khi khởi động lần đầu:

| | |
|--|--|
| **Email** | `admin@worldfaith.game` |
| **Mật khẩu** | `Admin@WorldFaith2024!` |
| **Quyền** | Admin (toàn quyền Admin Panel) |

> ⚠️ **Đổi ngay trước khi deploy** — sửa `Admin.Password` trong `appsettings.json` → restart server.

---

## 13. Hướng dẫn sử dụng Admin Panel

### Dashboard
Trang chủ. Hiển thị:
- **Server health** — đèn xanh nhấp nháy = online, đèn đỏ = down
- **8 stat cards** — Worlds, Gods, Players, Civs, Entities, Religions, NPCs, Orgs (cập nhật mỗi 5 giây)
- **Active Worlds** — mỗi world hiện tick/cycle/số god đang chơi

### Events Log
Feed realtime tất cả sự kiện trong game (cập nhật mỗi 3 giây).

Cách dùng:
1. Chọn World ở góc phải
2. Chọn filter tab: All / Crime / Accidents / Social / Political / Miracle / Evolution
3. Bật/tắt **Auto refresh** để theo dõi live hoặc đóng băng để đọc kỹ
4. Mỗi event hiển thị: loại event (badge màu), mô tả, tác động faith/economy, tick xảy ra

### Worlds
Quản lý các world đang chạy.

- **Force End** — kết thúc world ngay, tính điểm và cập nhật leaderboard
- **Force Rebirth** — reset world về tick 0 nhưng giữ nguyên god với rank hiện tại

### Maps & Tiles
Visual editor bản đồ game.

Cách dùng:
1. Chọn World → bản đồ 64×64 tile hiện ra (có thể zoom 3-16px/tile bằng slider)
2. **Click vào tile** → popup chỉnh sửa
3. Trong popup: đổi Tile Type (Grassland/Forest/Mountain...), chỉnh Fertility (0-1), bật/tắt Temple
4. Nhấn **Place Sacred** để biến tile thành Sacred Site (tăng evolution points cho entities ở đó)
5. Nhấn **Regen Map** (có confirm) để tái tạo toàn bộ bản đồ bằng Perlin Noise

**Chú thích màu:** Grassland=xanh lá, Forest=xanh đậm, Mountain=xám, Desert=vàng, Tundra=xanh nhạt, Water=xanh dương, Volcano=đỏ, Sacred=vàng gold

### Dungeons
Quản lý dungeons — hang động nguy hiểm mà Adventure Guild nhận quest.

**Các trạng thái dungeon:**
- **Active** — đang tồn tại, Guild có thể nhận quest
- **Infested** — bị nhiễm sau 200 ticks không ai clear, nguy hiểm hơn (danger ×1.3)
- **Sealed** — admin đã phong ấn, không ai vào được
- **Cleared** — Guild đã clear thành công

**Cách spawn dungeon mới:**
1. Nhấn **+ Spawn Dungeon**
2. Chọn loại:
   - `AncientRuins` — an toàn nhất, thích hợp để cho Guild luyện tập
   - `LostTemple` — xác suất có relic cao, liên quan forgotten gods
   - `MonstersLair` — danger cao, reward tốt
   - `ForbiddenSanctum` — nguy hiểm nhất, relic mạnh nhất
   - `DarkPortal` — nguy hiểm liên tục, spawn monsters nếu không bị seal ngay
3. Điền tọa độ X, Y (0-63)
4. Nhập God ID nếu muốn dungeon liên kết với god cụ thể (god đó có thể có relic bên trong)
5. Nhấn Spawn

**Quản lý dungeon đang tồn tại:**
- Nhấn **Seal** để phong ấn (ngăn DarkPortal tiếp tục leak energy)
- Nhấn **Clear** để coi như đã clear (guild nhận reward ảo)

### Relics
Quản lý di vật — nguồn sống còn của Forgotten Gods.

**Cơ chế quan trọng cần hiểu:**
- Mỗi relic phát **Faith passive** về origin god mỗi 10 ticks (2-12 faith/tick)
- God với **0 followers** nhưng còn relic → trạng thái **Forgotten** (tồn tại ở dạng yếu, không bị eliminate)
- God với **0 followers + không relic** → **Eliminated vĩnh viễn**
- Civ giữ relic mà ruling religion = origin god → faith bonus **+50%**
- Relic bỏ hoang (không ai giữ) → decay dần (-0.5 FaithBonus mỗi 200 ticks)

**Cách transfer relic:**
1. Click vào relic trong bảng
2. Điền NPC ID (nếu muốn NPC giữ) hoặc Civ ID (nếu muốn cả civilization giữ)
3. Để trống cả hai = relic bị bỏ hoang, sẽ decay
4. Nhấn Lưu

**Khi nào cần dùng trang này:**
- Muốn giúp Forgotten God thoát khỏi tình trạng cô lập → transfer relic về civ có cùng religion
- Muốn test cơ chế god survival → destroy hết relics và xem god có bị eliminate không
- Muốn cân bằng game → chuyển relic mạnh sang god yếu hơn

### Gods
Quản lý trực tiếp các god đang trong game.

- **Bảng chính** — hiển thị Faith (số vàng), Trust bar, Fear, Followers, Archetype badge, Status
- **Click vào god** → modal chỉnh sửa:
  - Điều chỉnh Faith, Trust, Fear, FollowerCount trực tiếp
  - Unlock miracles — ô xanh = đã có, ô xám = nhấn để unlock ngay
- **Eliminate** — loại god khỏi game ngay lập tức (không hoàn tác)

**Khi nào dùng:**
- Debug: god bị bug faith âm → set lại Faith = 100
- Test: unlock toàn bộ miracles để test counter system
- Balance: god một phía quá mạnh → giảm Faith xuống

### NPCs
Quản lý NPC theo 5 tầng xã hội.

- **Filter** — chọn World + chọn Tier để lọc (All/Tier 1-5)
- **Bảng** — hiển thị Tier badge (màu theo tier), Personality, các stat bars (Loyalty/Ambition/Piety), Champion badge
- **Click vào NPC** → chỉnh Loyalty, Ambition, Piety, Wealth, GodTrustLevel (0-100)
- **Exile** — đuổi NPC ra khỏi kingdom, state = Exiled
- **Kill** — NPC chết, state = Dead

**Khi nào dùng:**
- Tạo kịch bản betrayal: set Noble Ambition = 90, Loyalty = 20 → Noble sẽ phản bội sớm
- Tạo Champion: tìm Adventurer, set GodTrustLevel = 75 → đợi NpcSpawnService promote, hoặc dùng nút Promote Champion
- Test event: set Servant Loyalty = 10 → servant sẽ tìm cách extort Noble

### Mobs / Entities
Quản lý sinh vật tiến hóa.

- **4 stat cards** — đếm theo stage WildAnimal/DivineBeast/ApocalypticEntity/CelestialGuardian
- **Evolve dropdown** — chọn target stage ngay trong bảng, áp dụng ngay
- **Spawn** — tạo entity mới tại tọa độ X,Y với stage tùy chọn
- **Kill** — xóa entity

**Khi nào dùng:**
- Test Apex entity: spawn ApocalypticEntity ở giữa map → xem civs phản ứng
- Test Champion path: evolve HumanHero → Saint nhanh thay vì chờ EXP tích lũy
- Clear laggy entities: kill bớt WildAnimals nếu map quá đông

### Civilizations
Quản lý toàn bộ AI civilizations.

- **Bảng** — Race (cyan), Government badge (màu theo loại), State badge, Economy/Military/Food/Stability bars, Population, War indicator
- **Quick boost buttons** (trong bảng): `+E` (+30 Economy), `+M` (+30 Military), `+F` (+30 Food) — click ngay không cần mở modal
- **Collapse** — đẩy civ vào state Collapsing ngay
- **Click vào civ** → modal chỉnh đầy đủ 8 stats + Government dropdown + Personality + State + War toggle

**Khi nào dùng:**
- Civ đang chết đói → nhấn `+F` (Food) vài lần
- Muốn test Theocracy behavior → đổi Government = Theocracy trong modal
- Test Collapse Age events → đẩy một civ về Stability = 5, Collapse ngay

### Religions
Quản lý tôn giáo và doctrine.

- **Bảng** — Loại (Public/Secret Cult), Followers, Temples, Devotion bar, 5 giá trị Doctrine (M/I/H/F/S), Believer type breakdown (C/D/F/Cu/H)
- **Schism** — kích hoạt schism ngay (1/3 followers tách ra thành sect mới)
- **Erase** — xóa tôn giáo (có confirm)
- **Click vào religion** → modal 2 tab:

**Tab Thông tin:** Chỉnh Name, FollowerCount, TempleCount, DevotionLevel (0-1), toggle Secret Cult. Xem breakdown Believer types.

**Tab Doctrine Axes:** 5 sliders từ -100 đến +100:
  - *Mercy ↔ Punishment*: -100 = tha thứ tất cả, +100 = xử tử heretics ngay
  - *Isolation ↔ Expansion*: -100 = bảo vệ tín đồ cũ, +100 = truyền đạo mạnh mẽ (missionary speed 0.5x→2.0x)
  - *Harmony ↔ Dominion*: -100 = hòa hợp thiên nhiên/Elves thích, +100 = chinh phục/Orcs thích
  - *Freedom ↔ Order*: -100 = cá nhân tự do/Commoners thích, +100 = trật tự nghiêm ngặt/Nobles/Royals thích
  - *Sacrifice ↔ Prosperity*: -100 = đau khổ có giá trị/Undead thích, +100 = thịnh vượng chứng minh đức tin

**Doctrine tự động thay đổi theo events** — FailedMiracle → shift Mercy, HolyWarWon → shift Punishment.

### Organizations
Quản lý 6 loại tổ chức.

- **Type badges** — mỗi loại màu riêng: Kingdom=xanh, RoyalCourt=vàng, NobleHouse=cam, Guild=xanh lá, Religious=tím, Underground=đỏ
- **Power/Wealth/Loyalty bars**
- **Heat Level** (chỉ Underground) — càng cao càng dễ bị phát hiện
- **Expose** (Underground org) — lộ tổ chức ngầm, kingdom trấn áp ngay
- **Disband** — giải tán tổ chức

**Khi nào dùng:**
- Test betrayal chain: tìm NobleHouse, set Loyalty = 10, Power = 80 → Noble sẽ phản bội sớm
- Control underground: nếu một god đang dùng UndergroundOrg để tích Fear quá nhiều → Expose
- Test Court deadlock: check RoyalCourt xem members có GodInfluenceId khác nhau không → nếu có = deadlock đang xảy ra

### Players
Quản lý tài khoản người dùng.

- **Search** — tìm theo email hoặc username (realtime)
- **Click vào player** → modal với 3 section:
  - **Ban/Unban** — ban cần điền lý do, unban ngay
  - **Reset Mật Khẩu** — điền mật khẩu mới, áp dụng ngay
  - **Phân Quyền** — Promote thành Admin hoặc Demote xuống Player

### Leaderboard
Xem top players, reset leaderboard (có confirm).

### Balance Config
Chỉnh 90 tham số game mà **không cần restart server**.

Cách dùng:
1. Chọn category tab để lọc (faith/miracle/religion/evolution/civ/npc/org/gov/age/rank/dungeon/director)
2. Hoặc dùng ô **Tìm tham số** để search theo tên
3. Click vào ô giá trị → sửa trực tiếp → nhấn Enter hoặc nút **Lưu**
4. Ô viền vàng = có thay đổi chưa lưu, ô viền xanh nhấp nháy = đã lưu thành công
5. Thay đổi có hiệu lực sau tối đa 60 giây (cache TTL)
6. Nhấn **↺ Reset Default** (có confirm) để đặt lại tất cả về giá trị ban đầu

**Các params quan trọng hay cần chỉnh:**

| Param | Mô tả | Default |
|-------|-------|---------|
| `faith.tick_interval` | Tốc độ tick (ms) — thấp = nhanh hơn | 500 |
| `miracle.cost_rain` | Faith cost của Rain miracle | 10 |
| `civ.famine_threshold` | Food level gây nạn đói | 10 |
| `npc.champion_trust_required` | Trust cần để Adventurer → Champion | 70 |
| `rank.awakened_threshold` | Cumulative Faith để đạt Awakened | 5000 |
| `dungeon.relic_drop_chance` | Xác suất dungeon có relic | 0.4 |
| `director.stagnation_disaster_chance` | Xác suất AI Director inject disaster | 0.15 |

---

## 14. Hướng dẫn cơ chế game

### Cách Faith hoạt động

Faith là tài nguyên chính để thực hiện miracles. Tăng mỗi tick (500ms mặc định) theo công thức:

```
Faith/tick = (Followers × Devotion × RaceAffinity × Trust × Institution × Event)
           × ArchetypeBonus × GodRankMultiplier
```

**Giải thích từng yếu tố:**
- **Followers** — tổng số NPC đang theo đạo. Tier cao hơn = faith nhiều hơn (Royalty = 0.5/tick, Commoner = 0.01/tick)
- **Devotion** — độ sâu đức tin: Casual 0.5x, Devout 1.0x, Fanatic 2.0x, Cultist 1.5x
- **RaceAffinity** — race phù hợp với archetype của god → bonus lên đến 1.6x (Elf theo Nature god 160%)
- **Trust** — god đã làm gì cho civ này: miracle thành công → tăng, miracle thất bại/gây hại → giảm
- **ArchetypeBonus** — bonus riêng theo archetype (War god +10% khi civ đang chiến, Light god HealFollower miễn phí...)
- **GodRankMultiplier** — rank cao faith nhiều hơn: Nascent 1.0x → Ancient 3.0x

**Cách tăng Faith nhanh:**
1. Convert NPC tier cao (Noble, Royalty) — 1 Royalty = 50 Commoners về faith
2. Xây temples — mỗi temple +0.5 faith/tick bất kể followers
3. Giữ Trust cao — miracle thành công → trust tăng → faith multiplier tăng
4. Target race phù hợp với archetype của bạn

### Cách Race Affinity ảnh hưởng Conversion

Conversion chance mỗi lần tương tác:
```
Chance = Openness × RaceAffinity × SocialPressure × TrustDiff × RecentEvents × DoctrineMatch
```

- NPC tier thấp chuyển đạo dễ hơn (Commoner openness 0.8, Royalty 0.15)
- Elf trong civilization theo Nature god → RaceAffinity 1.6x → convert rất dễ
- Demon theo Light god → RaceAffinity 0.2x → gần như không thể convert bình thường (cần extraordinary event)
- Ruling religion của civ = religion của bạn → SocialPressure 1.5x bonus

**Để convert được những race khó:**
- Gửi Dream nhiều lần để tăng GodTrustLevel từ từ
- Chờ disaster (Crop Failure, Disease) → devout surge → cơ hội conversion tăng +25%
- Thực hiện miracle thành công ngay trước khi NPC gặp biến cố

### Cách God Rank hoạt động

Rank tăng theo **tổng cumulative Faith** đã kiếm (không phải Faith hiện tại). Rank cao hơn:
- Mở khóa thêm miracles
- Tăng faith gen multiplier
- Mở rộng tầm ảnh hưởng của Divine Voice

**Nếu god về 0 followers:**
- Còn relic hoặc hidden cult → trạng thái **Forgotten** (vẫn sống, faith gen yếu 10%)
- Không còn gì → **Eliminated vĩnh viễn**

→ Để tránh bị eliminate: cố giữ ít nhất 1 relic hoặc 1 hidden cult trước khi followers về 0.

### Cách Doctrine ảnh hưởng Religion

5 doctrine axes thay đổi hành vi AI của religion:

| Axis | -100 (Low) | +100 (High) | Gameplay |
|------|-----------|------------|---------|
| Mercy/Punishment | Tha thứ tất cả | Xử tử heretics ngay | Crime response, heresy trial |
| Isolation/Expansion | Bảo vệ tín đồ cũ | Truyền đạo tích cực | Missionary speed ×0.5→×2.0 |
| Harmony/Dominion | Hài hòa thiên nhiên | Chinh phục thế giới | Elves thích -100, Orcs thích +100 |
| Freedom/Order | Cá nhân tự do | Trật tự nghiêm | Nobles/Royals support +60% khi Order cao |
| Sacrifice/Prosperity | Đau khổ có ý nghĩa | Thịnh vượng = đức tin | Disaster interpretation |

**Doctrine tự động thay đổi** qua events — admin có thể chỉnh thủ công qua trang Religions → Doctrine Axes tab.

### Cách AI Director hoạt động

AiDirectorService chạy mỗi 20 ticks để kiểm soát pacing:

**Age Transitions** (tự động theo tick):
- Tick 100 → **Kingdom Age** — dungeons AncientRuins spawn quanh civs
- Tick 300 → **Conflict Age** — ForbiddenSanctum xuất hiện, holy wars có thể xảy ra
- Tick 600 → **Collapse Age** — DarkPortals xuất hiện, civs yếu nhất bắt đầu Collapsing
- Tick 850 → **Rebirth Age** — thế giới tái sinh, civs mới nảy sinh từ đống đổ nát

**Anti-stagnation** (mỗi 80 ticks): Nếu không có war nào → 15% chance inject disaster vào civ yếu nhất.

**Anti-snowball** (mỗi 150 ticks): Nếu một god có >60% tổng followers → gods yếu hơn nhận faith boost +50.

### Cách Dungeon & Relic kết nối với nhau

```
God thực hiện miracle "Create Dungeon"
         ↓
DungeonService spawn dungeon tại tọa độ (có thể có Relic bên trong 40%)
         ↓
Adventure Guild nhận quest → party vào dungeon
         ↓
Success: relic được phát hiện, adventurers nhận EXP
         ↓
Adventurer đủ 150 EXP + GodTrust ≥ 70 → promote thành Champion
         ↓
Champion spread faith khắp world, có thể trở thành Saint hoặc FallenDemonLord
```

---

## 15. Thông số kỹ thuật

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
| **Server .cs files** | 42 files, 28 service interfaces |
| **Unity scripts** | 27 .cs files |
| **Admin pages** | 18 trang, 65+ API endpoints |
| **Unit tests** | 75 test cases |
| **CI/CD** | 4 GitHub Actions workflows |
| **Balance params** | 90 params runtime-tunable |
| **God archetypes** | 8 |
| **God ranks** | 7 (Forgotten → Ancient) |
| **Race types** | 8 với affinity matrix 8×8 |
| **NPC tiers** | 5 (Commoner → Royalty) |
| **Organization types** | 6 |
| **Government types** | 6 |
| **Miracles** | 15 (3 tiers) |
| **Doctrine axes** | 5 |
| **Believer types** | 5 |
| **Evolution stages** | 9 (3 paths × 3 stages) |
| **Dungeon types** | 5 |
| **Relic types** | 8 |
| **World Ages** | 5 (Early → Rebirth) |
| **Asset cần thiết** | ~204 files (xem ASSETS.md) |

### Simulation Loop Ticks
| Service | Tần suất | Chức năng |
|---------|---------|----------|
| FaithService | Mỗi tick | Faith gen, archetype × race × rank multiplier |
| CivilizationSimulationService | Mỗi tick | AI personalities, food cycle, government, rebellion |
| ReligionService | Mỗi 5 ticks | Spread, schism, heresy, crusade |
| EvolutionService | Mỗi 3 ticks | EXP tích lũy, stage transition, apex effects |
| NPCInteractionService | Mỗi 10 ticks | Crime, marriage, betrayal, luck events |
| MemoryService | Mỗi 10 ticks | Relic faith gen, forgotten god survival |
| OrganizationService | Mỗi 20 ticks | Noble Houses, Guild missions, Court intrigue, Underground |
| AiDirectorService | Mỗi 20 ticks | Age transitions, anti-stagnation, anti-snowball |
| DungeonService | Mỗi 50 ticks | Natural spawn, infest check, DarkPortal warning |
| GodRankService | Mỗi 100 ticks | Rank update, forgotten state check |

---

## 16. Câu hỏi thường gặp

**Q: Lỗi "Cannot connect to Docker daemon"?**  
A: Mở Docker Desktop lên, đợi icon cá voi ở taskbar hết loading (thường 30-60 giây), thử lại.

**Q: Server báo "Unable to connect to MongoDB"?**  
A: Database chưa chạy. Chạy: `docker-compose up worldfaith-mongo worldfaith-redis -d`

**Q: Unity báo lỗi "The type or namespace HubConnection could not be found"?**  
A: Chưa cài SignalR DLLs. Làm lại Bước 3 cài đặt Unity. Nếu đã copy DLL rồi thì nhấn Assets → Refresh (`Ctrl+R`).

**Q: Menu WorldFaith không xuất hiện trong Unity?**  
A: Đợi Unity compile xong (thanh loading ở góc dưới phải phải hết). Nếu vẫn không: Assets → Refresh. Nếu vẫn không: kiểm tra Console xem có lỗi compile không.

**Q: Admin Panel báo 401 Unauthorized?**  
A: Token đăng nhập hết hạn (60 phút). Đăng xuất và đăng nhập lại. Hoặc kiểm tra file `.env.local` có đúng API URL không.

**Q: Chỉnh Balance Config nhưng không thấy thay đổi?**  
A: Chờ tối đa 60 giây (cache TTL). Nếu vẫn không có hiệu lực, thử restart server.

**Q: Chơi nhiều người trên mạng LAN?**  
A: Tìm IP máy chủ — Windows: `ipconfig`, Mac: `ifconfig | grep inet`. Đổi Server URL trong Unity thành `http://192.168.x.x:5000/hubs/world` (thay bằng IP tìm được).

**Q: Reset toàn bộ dữ liệu để chạy lại từ đầu?**
```bash
docker-compose down -v    # ⚠️ XÓA SẠCH database — không hoàn tác được!
docker-compose up worldfaith-mongo worldfaith-redis -d
# Sau đó restart server để seed lại admin account
```

**Q: Forgotten God là gì và khi nào god bị eliminate?**  
A: Khi god về 0 followers:
- Còn ít nhất 1 relic đang active **hoặc** 1 hidden cult → trạng thái **Forgotten** (sống sót ở dạng yếu)
- Không còn gì → **Eliminated vĩnh viễn** (không thể hồi phục)

Để kiểm tra: vào Admin → Relics → xem god đó còn relic không. Vào Admin → Religions → lọc IsHidden để xem còn cult không.

**Q: Doctrine tự động thay đổi, có cách nào khóa lại không?**  
A: Hiện tại chưa có khóa — doctrine được thiết kế để evolve theo events (đây là feature). Nếu muốn giữ nguyên, admin có thể chỉnh lại thủ công qua trang Religions → Doctrine Axes sau mỗi event.

**Q: Tại sao Conversion chance của một NPC quá thấp dù god đang mạnh?**  
A: Kiểm tra 3 yếu tố chính:
1. Race của NPC có thấp affinity với archetype của god không? (vd: Demon theo Light god → 20% = rất khó)
2. NPC tier cao (Noble/Royalty) → openness thấp (0.15-0.3)
3. Doctrine của religion có phù hợp với personality của NPC không?

---

*Cần hỗ trợ? Mở issue tại: https://github.com/thanhtinz/Game-new/issues*  
*WorldFaith v1.0 — "Players do not control the world. They influence belief, and belief controls the world."*
