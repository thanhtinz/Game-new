# ⚡ WorldFaith — Hướng Dẫn Cài Đặt Từ A Đến Z

> **WorldFaith** là game simulation thần linh multiplayer — PC & Mobile.  
> Bạn đóng vai một vị thần: thu phục tín đồ, thực hiện phép màu, sáng lập tôn giáo, tiến hóa sinh vật, và đối đầu với các thần khác.

---

## 📋 Mục lục

1. [Trước khi bắt đầu — Cài những gì?](#1-trước-khi-bắt-đầu--cài-những-gì)
2. [Lấy code về máy](#2-lấy-code-về-máy)
3. [Cài và chạy Database (MongoDB + Redis)](#3-cài-và-chạy-database-mongodb--redis)
4. [Cấu hình Server](#4-cấu-hình-server)
5. [Chạy Server](#5-chạy-server)
6. [Cài đặt Admin Panel (trang quản trị)](#6-cài-đặt-admin-panel-trang-quản-trị)
7. [Cài đặt Unity Client (game)](#7-cài-đặt-unity-client-game)
8. [Build game ra file chạy được](#8-build-game-ra-file-chạy-được)
9. [Kiểm tra toàn bộ hoạt động](#9-kiểm-tra-toàn-bộ-hoạt-động)
10. [Deploy lên server thật (production)](#10-deploy-lên-server-thật-production)
11. [Tài khoản mặc định](#11-tài-khoản-mặc-định)
12. [Tính năng game](#12-tính-năng-game)
13. [Câu hỏi thường gặp](#13-câu-hỏi-thường-gặp)

---

## 1. Trước khi bắt đầu — Cài những gì?

Bạn cần cài **4 công cụ** này trước. Làm theo thứ tự:

---

### 🔵 Bước 1.1 — Cài .NET SDK 8

**.NET SDK** là bộ công cụ để chạy code C# (ngôn ngữ của server WorldFaith).

1. Truy cập: **https://dotnet.microsoft.com/download**
2. Tải bản **.NET 8.0** → chọn đúng hệ điều hành (Windows/Mac/Linux)
3. Chạy file cài đặt, bấm Next → Next → Install

**Kiểm tra đã cài xong chưa:** Mở **Command Prompt** (Windows) hoặc **Terminal** (Mac/Linux), gõ:
```
dotnet --version
```
Nếu thấy số bắt đầu bằng `8.` là thành công. Ví dụ: `8.0.101`

---

### 🐳 Bước 1.2 — Cài Docker Desktop

**Docker** giúp chạy MongoDB và Redis mà không cần cài tay — chỉ cần 1 lệnh.

1. Truy cập: **https://www.docker.com/products/docker-desktop**
2. Tải về và cài đặt
3. Sau khi cài, **mở Docker Desktop lên** (icon cá voi ở taskbar/menu bar)
4. Đợi đến khi thấy "Docker Desktop is running" (icon cá voi không còn loading)

> ⚠️ **Quan trọng:** Docker Desktop phải đang chạy trước khi làm bước 3.

**Kiểm tra:**
```
docker --version
```
Thấy `Docker version 24.x.x` hoặc cao hơn là ổn.

---

### 🟢 Bước 1.3 — Cài Node.js

**Node.js** dùng để chạy trang Admin Panel (trang web quản trị game).

1. Truy cập: **https://nodejs.org**
2. Tải bản **LTS** (bên trái, khuyên dùng)
3. Cài đặt bình thường

**Kiểm tra:**
```
node --version
```
Thấy `v20.x.x` hoặc cao hơn là ổn.

---

### 🎮 Bước 1.4 — Cài Unity

**Unity** là phần mềm để tạo và chạy game WorldFaith.

1. Truy cập: **https://unity.com/download**
2. Tải **Unity Hub** về cài
3. Mở Unity Hub → đăng nhập hoặc tạo tài khoản miễn phí
4. Vào tab **Installs** → nhấn **Install Editor**
5. Chọn **Unity 2022.3 LTS** → cài đặt (chờ ~15-30 phút)
   - Tích thêm: **Android Build Support** (nếu muốn build mobile)
   - Tích thêm: **iOS Build Support** (nếu dùng Mac và muốn build iOS)

---

## 2. Lấy code về máy

Mở **Command Prompt** hoặc **Terminal**, chạy lệnh sau:

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
```

> Nếu chưa có Git: tải tại **https://git-scm.com** → cài → mở lại terminal.

Sau khi chạy xong, bạn sẽ thấy thư mục `Game-new` xuất hiện. Đây là toàn bộ code của WorldFaith.

---

## 3. Cài và chạy Database (MongoDB + Redis)

WorldFaith dùng 2 loại database:
- **MongoDB** — lưu dữ liệu game (người chơi, thế giới, tôn giáo...)
- **Redis** — lưu dữ liệu tạm thời nhanh (bảng xếp hạng, chat...)

Bạn **không cần cài MongoDB hay Redis thủ công**. Docker sẽ lo tất cả.

### Chạy database bằng Docker

Đảm bảo Docker Desktop đang chạy, sau đó vào thư mục project:

```bash
cd Game-new
docker-compose up worldfaith-mongo worldfaith-redis -d
```

Lệnh này sẽ:
1. Tự tải MongoDB và Redis về (lần đầu mất vài phút)
2. Khởi động cả hai
3. Chạy ngầm trong background (`-d` = detached)

**Kiểm tra đang chạy:**
```bash
docker ps
```

Bạn sẽ thấy 2 dòng với tên `worldfaith-mongo` và `worldfaith-redis` có trạng thái `Up`:
```
CONTAINER ID   IMAGE        STATUS         NAMES
abc123...      mongo:7.0    Up 2 minutes   worldfaith-mongo
def456...      redis:7.2    Up 2 minutes   worldfaith-redis
```

### Dừng database (khi không dùng)
```bash
docker-compose stop worldfaith-mongo worldfaith-redis
```

### Khởi động lại database
```bash
docker-compose start worldfaith-mongo worldfaith-redis
```

### Xem dữ liệu trong MongoDB (tùy chọn)

Nếu muốn xem dữ liệu bằng giao diện đẹp, tải **MongoDB Compass** miễn phí:
1. Tải tại: **https://www.mongodb.com/products/compass**
2. Mở lên, nhập địa chỉ: `mongodb://localhost:27017`
3. Nhấn Connect → sẽ thấy database `worldfaith` sau khi server chạy lần đầu

---

## 4. Cấu hình Server

File cấu hình server nằm tại:
```
Game-new/server/WorldFaith.Server/appsettings.json
```

Mở file này bằng **Notepad** (Windows) hoặc **TextEdit** (Mac) hoặc bất kỳ text editor nào.

### Nội dung mặc định (đã sẵn sàng dùng):

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

### Giải thích từng phần:

| Phần | Ý nghĩa | Có cần đổi không? |
|------|---------|------------------|
| `MongoDB` | Địa chỉ kết nối MongoDB | Không (giữ nguyên khi dùng Docker) |
| `Redis` | Địa chỉ kết nối Redis | Không (giữ nguyên khi dùng Docker) |
| `Jwt.Secret` | Mã bí mật để tạo token đăng nhập | **Phải đổi khi deploy thật** |
| `Admin.Email` | Email tài khoản admin | Tùy chọn |
| `Admin.Password` | Mật khẩu tài khoản admin | **Phải đổi khi deploy thật** |

> ⚠️ **Khi deploy production:** Đổi `Jwt.Secret` thành chuỗi ngẫu nhiên ít nhất 32 ký tự. Ví dụ: `Xy7#mK9$pQ2@nL5&vR8^wJ3!cF6*` hoặc dùng tool tạo password ngẫu nhiên.

---

## 5. Chạy Server

### Cách 1 — Chạy từ terminal (khuyên dùng khi phát triển)

```bash
cd Game-new/server/WorldFaith.Server
dotnet run
```

Lần đầu chạy sẽ mất 30-60 giây để compile. Sau đó sẽ thấy:
```
[HH:MM:SS INF] WorldFaith Server khởi động
[HH:MM:SS INF] Balance config seeded
[HH:MM:SS INF] Admin account seeded: admin@worldfaith.game
[HH:MM:SS INF] Now listening on: http://localhost:5000
```

**Server đang chạy tại:** http://localhost:5000

### Kiểm tra server hoạt động

Mở trình duyệt, truy cập: **http://localhost:5000/health**

Bạn sẽ thấy:
```json
{"status":"ok","time":"2024-01-01T00:00:00Z"}
```

### Dừng server
Nhấn `Ctrl + C` trong terminal.

### Cách 2 — Chạy tất cả bằng Docker (server + database cùng lúc)

```bash
cd Game-new
docker-compose up -d
```

Phù hợp khi muốn server chạy ngầm, không cần mở terminal.

---

## 6. Cài đặt Admin Panel (trang quản trị)

Admin Panel là trang web để quản lý game: xem thống kê, quản lý người chơi, chỉnh cân bằng game...

### Bước 6.1 — Cài dependencies

```bash
cd Game-new/admin-panel
npm install
```

Lệnh này tải về tất cả thư viện cần thiết. Mất 1-2 phút, chỉ cần làm 1 lần.

### Bước 6.2 — Tạo file cấu hình

Tạo file tên `.env.local` trong thư mục `admin-panel/` với nội dung:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

**Cách tạo file:**

- **Windows:** Mở Notepad → Lưu file với tên `.env.local` vào thư mục `Game-new/admin-panel/`
- **Mac/Linux:** `echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > Game-new/admin-panel/.env.local`

### Bước 6.3 — Chạy Admin Panel

```bash
cd Game-new/admin-panel
npm run dev
```

Mở trình duyệt: **http://localhost:3001**

**Đăng nhập với:**
- Email: `admin@worldfaith.game`
- Password: `Admin@WorldFaith2024!`

### Các trang trong Admin Panel

| Trang | Chức năng |
|-------|-----------|
| Dashboard | Thống kê realtime: số world, người chơi, thần, entity... |
| Worlds | Xem, dừng, rebirth các world đang chạy |
| Players | Tìm kiếm, ban/unban người chơi |
| Leaderboard | Bảng xếp hạng theo ELO, wins, followers |
| Scenarios | Thông tin 6 kịch bản game |
| Balance Config | Chỉnh 45 thông số game ngay lập tức, không cần restart |

---

## 7. Cài đặt Unity Client (game)

Đây là phần phức tạp nhất. Làm từng bước, đừng bỏ qua.

### Bước 7.1 — Mở project trong Unity

1. Mở **Unity Hub**
2. Nhấn **Open** → **Add project from disk**
3. Tìm đến thư mục `Game-new/client-unity/`
4. Nhấn **Open** / **Select Folder**
5. Unity Hub sẽ hỏi version → chọn **Unity 2022.3 LTS**
6. Nhấn **Open** và đợi Unity load (lần đầu mất 5-10 phút)

> Bạn có thể thấy nhiều lỗi đỏ lúc đầu — bình thường, sẽ fix ở các bước sau.

### Bước 7.2 — Cài packages trong Unity

Trong Unity, vào menu: **Window → Package Manager**

Cài lần lượt các package sau:

**Package 1: TextMeshPro** (hiển thị chữ đẹp trong game)
1. Trong Package Manager, bên trái chọn **Unity Registry**
2. Tìm kiếm: `TextMeshPro`
3. Chọn → nhấn **Install**
4. Sau khi install xong, Unity sẽ hỏi có muốn import resources không → nhấn **Import TMP Essentials**

**Package 2: Newtonsoft Json** (xử lý dữ liệu JSON)
1. Trong Package Manager, nhấn dấu **+** góc trên trái
2. Chọn **Add package by name**
3. Nhập: `com.unity.nuget.newtonsoft-json`
4. Nhấn **Add**

**Package 3: Mobile Notifications** (thông báo điện thoại, tùy chọn)
1. Nhấn **+** → **Add package by name**
2. Nhập: `com.unity.mobile.notifications`
3. Nhấn **Add**

### Bước 7.3 — Cài SignalR (kết nối với server)

SignalR là thư viện giúp game kết nối realtime với server. Phải cài thủ công:

**Cách A — Tự động (dùng script, khuyên dùng)**

Nếu bạn dùng Mac hoặc Linux, tạo file `install-signalr.sh`:
```bash
#!/bin/bash
UNITY_PATH="client-unity/Assets/Plugins/SignalR"
mkdir -p "$UNITY_PATH"
cd /tmp
dotnet new console -n SignalRTemp
cd SignalRTemp
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release
cp bin/Release/net8.0/publish/Microsoft.AspNetCore.SignalR.*.dll "../../$UNITY_PATH/"
cp bin/Release/net8.0/publish/Microsoft.AspNetCore.Http.Connections*.dll "../../$UNITY_PATH/"
cd /tmp && rm -rf SignalRTemp
echo "✅ SignalR đã cài xong!"
```
Chạy: `bash install-signalr.sh`

**Cách B — Thủ công (Windows hoặc nếu Cách A không chạy)**

1. Tải file tại: `https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/8.0.0`
2. Nhấn **Download package** (tải về file `.nupkg`)
3. **Đổi đuôi** file từ `.nupkg` thành `.zip`
4. **Giải nén** file zip đó
5. Vào thư mục `lib/netstandard2.1/` bên trong
6. **Copy** tất cả file `.dll` vào `Game-new/client-unity/Assets/Plugins/SignalR/`
   - Tạo thư mục `Plugins/SignalR/` nếu chưa có

Các file `.dll` cần copy:
```
Microsoft.AspNetCore.SignalR.Client.dll
Microsoft.AspNetCore.SignalR.Client.Core.dll
Microsoft.AspNetCore.SignalR.Common.dll
Microsoft.AspNetCore.Http.Connections.Client.dll
Microsoft.AspNetCore.Http.Connections.Common.dll
```

> Quay lại Unity, nó sẽ tự phát hiện file mới và import.

### Bước 7.4 — Liên kết Shared Library

Shared Library chứa code dùng chung giữa server và Unity (các kiểu dữ liệu, enum...).

**Cách đơn giản nhất — copy thủ công:**

Copy 3 thư mục từ `Game-new/shared/WorldFaith.Shared/` vào `Game-new/client-unity/Assets/WorldFaith/Shared/`:

```
shared/WorldFaith.Shared/Enums/      →  client-unity/Assets/WorldFaith/Shared/Enums/
shared/WorldFaith.Shared/Models/     →  client-unity/Assets/WorldFaith/Shared/Models/
shared/WorldFaith.Shared/Contracts/  →  client-unity/Assets/WorldFaith/Shared/Contracts/
```

**Cách tự động (chạy 1 lần):**

Windows (PowerShell):
```powershell
$src = "Game-new/shared/WorldFaith.Shared"
$dst = "Game-new/client-unity/Assets/WorldFaith/Shared"
New-Item -ItemType Directory -Force -Path $dst
Copy-Item "$src/Enums" $dst -Recurse -Force
Copy-Item "$src/Models" $dst -Recurse -Force
Copy-Item "$src/Contracts" $dst -Recurse -Force
```

Mac/Linux:
```bash
SRC="Game-new/shared/WorldFaith.Shared"
DST="Game-new/client-unity/Assets/WorldFaith/Shared"
mkdir -p "$DST"
cp -r "$SRC/Enums" "$DST/"
cp -r "$SRC/Models" "$DST/"
cp -r "$SRC/Contracts" "$DST/"
```

> Sau khi copy xong, Unity sẽ tự import. Chờ Unity xong rồi mới làm bước tiếp.

### Bước 7.5 — Tạo Scenes trong Unity

Game WorldFaith có 3 màn hình (scene):
- **LoginScene** — màn hình đăng nhập/đăng ký
- **LobbyScene** — lobby chọn phòng, tạo phòng
- **GameScene** — màn hình chơi game chính

**Tạo LoginScene:**
1. Trong Unity, vào **File → New Scene → Basic (Built-in)** → nhấn Create
2. Vào **File → Save As** → đặt tên `LoginScene` → lưu vào `Assets/Scenes/`
3. Trong Unity menu bar: **WorldFaith → Setup → Create Login Scene Objects**
4. Unity sẽ tự tạo các GameObject cần thiết

**Tạo LobbyScene:**
1. **File → New Scene → Basic (Built-in)**
2. Lưu tên `LobbyScene`
3. **WorldFaith → Setup → Create Lobby Scene Objects**

**Tạo GameScene:**
1. **File → New Scene → Basic (Built-in)**
2. Lưu tên `GameScene`
3. **WorldFaith → Setup → Create Game Scene Objects**

> Menu **WorldFaith** xuất hiện trên Unity menu bar sau khi Shared Library được import thành công.

### Bước 7.6 — Cấu hình địa chỉ server trong Unity

Mỗi Scene vừa tạo có các GameObject cần điền địa chỉ server:

1. Trong **GameScene**, tìm GameObject tên `WorldFaithClient` trong Hierarchy
2. Nhìn sang bên phải (Inspector panel)
3. Tìm trường **Server Url**, đổi thành:
   ```
   http://localhost:5000/hubs/world
   ```
4. Tương tự với `LobbyClient` → **Server Url**: `http://localhost:5000/hubs/lobby`
5. Tương tự với `ChatClient` → **Server Url**: `http://localhost:5000/hubs/chat`

> Khi deploy production, đổi `localhost:5000` thành domain thật của bạn.

### Bước 7.7 — Kiểm tra setup Unity

Vào menu: **WorldFaith → Validate → Check All Managers**

Nếu thấy toàn bộ `✅` trong Console là setup đúng.  
Nếu có `⚠️`, đọc thông báo để biết thiếu gì.

---

## 8. Build game ra file chạy được

### Build cho PC (Windows/Mac/Linux)

1. Vào **File → Build Settings**
2. Kéo 3 scene vào danh sách **Scenes In Build** theo thứ tự:
   ```
   0: Assets/Scenes/LoginScene
   1: Assets/Scenes/LobbyScene
   2: Assets/Scenes/GameScene
   ```
3. Chọn platform: **PC, Mac & Linux Standalone**
4. Nhấn **Switch Platform** (nếu cần, đợi vài phút)
5. Nhấn **Build** → chọn thư mục để xuất file

File game sẽ nằm trong thư mục bạn chọn. Chạy file `.exe` (Windows) hoặc file app (Mac).

### Build cho Android

**Chuẩn bị:**
- Phải cài Android Build Support khi cài Unity (xem Bước 1.4)
- Cần cài **Android SDK** (Unity tự cài nếu bạn tích vào khi cài Unity)

**Các bước:**
1. **File → Build Settings** → chọn **Android**
2. Nhấn **Switch Platform**
3. Nhấn **Player Settings** (góc dưới trái) → đổi:
   - **Company Name**: tên công ty của bạn
   - **Product Name**: `WorldFaith`
   - **Package Name**: `com.yourname.worldfaith` (ví dụ: `com.john.worldfaith`)
4. Trong **Player Settings → Other Settings**:
   - **Minimum API Level**: Android 8.0 (API 26) hoặc cao hơn
5. Nhấn **Build** → chọn thư mục → Unity tạo file `.apk`

**Cài lên điện thoại Android:**
- Bật **Developer Mode** trên điện thoại:  
  Settings → About Phone → nhấn **Build Number** 7 lần
- Bật **USB Debugging**: Settings → Developer Options → USB Debugging
- Kết nối điện thoại với máy tính qua USB
- Trong Unity: **Build And Run** (thay vì Build) — Unity sẽ cài thẳng lên máy

### Build cho iOS (chỉ trên Mac)

1. **File → Build Settings** → chọn **iOS**
2. Nhấn **Switch Platform**
3. Nhấn **Build** → Unity tạo thư mục Xcode project
4. Mở Xcode → chọn Team (Apple Developer Account) → Build

---

## 9. Kiểm tra toàn bộ hoạt động

Trước khi chơi thử, kiểm tra theo thứ tự:

**Bước 1 — Database đang chạy:**
```bash
docker ps
```
Phải thấy `worldfaith-mongo` và `worldfaith-redis` đều `Up`.

**Bước 2 — Server đang chạy:**
Mở trình duyệt → **http://localhost:5000/health**  
Phải thấy `{"status":"ok",...}`

**Bước 3 — Admin Panel đang chạy:**
Mở trình duyệt → **http://localhost:3001**  
Phải thấy trang đăng nhập WorldFaith Admin.

**Bước 4 — Unity kết nối được:**
1. Trong Unity, nhấn nút **Play** (tam giác ▶️ ở giữa màn hình)
2. Màn hình game hiện ra trong Unity Editor
3. Thử đăng nhập với tài khoản bất kỳ (cần đăng ký trước)
4. Nếu kết nối thành công, vào được Lobby

**Bước 5 — Tạo phòng và chơi thử:**
1. Đăng ký 2 tài khoản khác nhau (có thể dùng 2 tab trình duyệt với Admin Panel để tạo)
2. Dùng 2 máy hoặc 2 Unity Editor window để join cùng 1 phòng
3. Bắt đầu game → xem thế giới được sinh ra

---

## 10. Deploy lên server thật (production)

Khi muốn cho nhiều người chơi online, cần thuê server VPS và deploy lên đó.

### Chuẩn bị VPS

Thuê VPS tại: DigitalOcean, Vultr, Linode, hoặc nhà cung cấp Việt Nam như VCCloud, CMC.  
Cấu hình tối thiểu: **2 CPU, 4GB RAM, Ubuntu 22.04**

### Cài Docker trên server

Kết nối SSH vào server, chạy:
```bash
sudo apt update
sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER
```
Đăng xuất và đăng nhập lại để áp dụng quyền.

### Upload code và chạy

```bash
# Upload code lên server
scp -r Game-new/ user@YOUR_SERVER_IP:/opt/worldfaith/

# SSH vào server
ssh user@YOUR_SERVER_IP

# Di chuyển vào thư mục
cd /opt/worldfaith

# Tạo file biến môi trường
cat > .env << 'EOF'
JWT_SECRET=thay_bang_chuoi_ngau_nhien_32_ki_tu_tro_len
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=MatKhauManh@2024!
EOF

# Khởi động tất cả
docker-compose up -d
```

### Cài Nginx (reverse proxy)

Nginx giúp người dùng truy cập qua domain thay vì phải nhớ IP và port.

```bash
sudo apt install -y nginx

# Tạo cấu hình
sudo nano /etc/nginx/sites-available/worldfaith
```

Nội dung file:
```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;

        # Bắt buộc cho WebSocket (SignalR)
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_read_timeout 86400;
    }
}
```

```bash
# Bật site
sudo ln -s /etc/nginx/sites-available/worldfaith /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Cài HTTPS miễn phí

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
```

### Cập nhật địa chỉ server trong Unity

Sau khi có domain, đổi `Server Url` trong Unity từ `http://localhost:5000` thành `https://api.yourdomain.com`.  
Rồi build lại game.

---

## 11. Tài khoản mặc định

Khi server khởi động lần đầu, tự động tạo tài khoản admin:

| | Giá trị mặc định |
|--|--|
| **Email** | `admin@worldfaith.game` |
| **Password** | `Admin@WorldFaith2024!` |
| **Quyền** | Admin (toàn quyền) |

> ⚠️ **Bắt buộc đổi password** trước khi cho người khác chơi! Vào Admin Panel → đổi trong `appsettings.json` → restart server.

---

## 12. Tính năng game

### Gameplay cơ bản

**Faith (Niềm Tin)**  
Nguồn năng lượng của bạn. Tăng tự động từ tín đồ, đền thờ, và độ sùng bái. Dùng để thực hiện Miracle.

**15 Loại Miracle (Phép Màu)**

| Tier | Tên | Faith cần | Hiệu quả |
|------|-----|-----------|----------|
| 1 | Rain (Mưa) | 10 | Tăng độ màu mỡ đất |
| 1 | Dream (Giấc Mơ) | 5 | Tăng lòng tin của civ |
| 1 | BlessHarvest (Ban Phước) | 15 | Kinh tế +20, Dân số +5% |
| 1 | HealFollower (Chữa Lành) | 8 | Hồi phục tín đồ |
| 1 | Omen (Điềm Báo) | 3 | Tăng nhẹ lòng tin |
| 2 | Storm (Bão) | 30 | Giảm quân sự civ lân cận |
| 2 | Earthquake (Động Đất) | 40 | Phá hủy công trình |
| 2 | Curse (Nguyền Rủa) | 25 | Kinh tế -15, tin tưởng -10 |
| 2 | Portal (Cổng Thần) | 50 | Tăng thương mại |
| 2 | DivineVoice (Tiếng Thần) | 20 | Tin tưởng +15 |
| 3 | Volcano (Núi Lửa) | 100 | Phá hủy khu vực |
| 3 | DemonInvasion (Quỷ Xâm) | 120 | Triệu hồi quỷ tấn công |
| 3 | DivineBeast (Thần Thú) | 80 | Tạo Divine Beast |
| 3 | Revelation (Khải Thị) | 60 | Faith tín đồ +50 |
| 3 | HolyWar (Thánh Chiến) | 150 | Kích hoạt thánh chiến |

**Counter System (Phản Phép)**  
Khi rival god dùng miracle, bạn có N giây để phản phép. Phản thành công → miracle đối thủ bị hủy!

**8 Loại Thần (Archetype)**  
Mỗi loại có bonus riêng:
- **Light**: HealFollower miễn phí, BlessHarvest rẻ hơn
- **Darkness**: Curse rẻ hơn 50%, hiệu quả x2
- **War**: HolyWar rẻ hơn 30%
- **Order**: Revelation rẻ hơn, temple tăng Faith nhanh hơn
- **Chaos**: Hiệu quả ngẫu nhiên x0.8-1.6
- **Knowledge**: DivineVoice rẻ hơn 30%, hiệu quả x1.5
- **Nature**: Rain rẻ hơn 50%, DivineBeast rẻ hơn
- **Death**: Thu Faith từ Fear tương đương Darkness

**God Commandment (Lệnh Thần)**  
Phát lệnh trực tiếp cho civilization. Civ nghe hay không phụ thuộc vào Trust level:
- MakeWar, ExpandTerritory, BuildTemple, SpreadFaith
- MakePeace, FocusEconomy, FocusMilitary, Worship

**Religion System (Tôn Giáo)**
- Sáng lập tôn giáo công khai hoặc bí mật
- Tự spread sang civ lân cận
- **Schism**: tách ra khi devotion thấp
- **Heresy**: cult ẩn hình thành
- **Crusade**: thánh chiến giữa các tôn giáo

**Evolution System (Tiến Hóa)**  
3 nhánh tiến hóa:
```
WildAnimal → DivineBeast → CelestialGuardian
HumanHero  → Saint       → FallenDemonLord
Monster    → Titan       → ApocalypticEntity
```

### Multiplayer

- **2-8 người chơi** trong cùng thế giới
- **6 kịch bản game:**
  - Standard (Tiêu Chuẩn) — game thông thường
  - TheLastLight (Ánh Sáng Cuối) — 1 vs tất cả
  - ReligionWars (Thánh Chiến) — ai chiếm 70% tín đồ thắng
  - EvolutionRace (Đua Tiến Hóa) — evolve Apex đầu tiên thắng
  - FaithCrisis (Khủng Hoảng) — Faith tạo chậm 5x
  - Apocalypse (Tận Thế) — Monster mạnh x3, survive

### Bảng xếp hạng ELO

- Mỗi ván game cập nhật điểm ELO
- 3 bảng xếp hạng: Rating, Wins, Total Followers
- Xem trong game (LeaderboardPanel) và Admin Panel

---

## 13. Câu hỏi thường gặp

---

**Q: Lỗi "Cannot connect to Docker daemon" khi chạy docker-compose?**

A: Docker Desktop chưa chạy. Mở Docker Desktop lên, đợi icon cá voi hết loading, rồi thử lại.

---

**Q: Server báo lỗi "Unable to connect to MongoDB"?**

A: Database chưa chạy. Chạy lệnh:
```bash
docker-compose up worldfaith-mongo worldfaith-redis -d
```
Đợi 10-15 giây rồi thử chạy server lại.

---

**Q: Unity báo lỗi đỏ "The type or namespace 'HubConnection' could not be found"?**

A: Chưa cài SignalR. Quay lại Bước 7.3 và làm theo hướng dẫn cài SignalR.

---

**Q: Unity báo lỗi "The type or namespace 'WorldFaith' could not be found"?**

A: Chưa copy Shared Library. Quay lại Bước 7.4.

---

**Q: Menu "WorldFaith" không xuất hiện trong Unity?**

A: Unity chưa compile xong. Đợi thanh loading ở góc dưới phải của Unity chạy hết. Nếu vẫn không thấy, vào **Assets → Refresh** hoặc nhấn `Ctrl+R`.

---

**Q: Đăng nhập Admin Panel bị lỗi "401 Unauthorized"?**

A: Token đã hết hạn. Đăng xuất → đăng nhập lại. Hoặc kiểm tra file `.env.local` có dòng `NEXT_PUBLIC_API_URL=http://localhost:5000` chưa.

---

**Q: Chỉnh Balance Config trong Admin Panel có hiệu lực ngay không?**

A: Có, nhưng mất tối đa 60 giây (thời gian cache). Không cần restart server.

---

**Q: Làm sao để nhiều người chơi cùng nhau trên mạng LAN?**

A: Tìm địa chỉ IP của máy chạy server (Windows: `ipconfig`, Mac/Linux: `ifconfig`).  
Giả sử IP là `192.168.1.100`, thì trong Unity đổi Server Url thành:
```
http://192.168.1.100:5000/hubs/world
```
Tất cả máy trong cùng mạng Wifi sẽ kết nối được.

---

**Q: Backup dữ liệu game như thế nào?**

A: Chạy lệnh này để backup:
```bash
docker exec worldfaith-mongo mongodump --db worldfaith --archive=/tmp/backup.gz --gzip
docker cp worldfaith-mongo:/tmp/backup.gz ./backup-$(date +%Y%m%d).gz
```

Để khôi phục:
```bash
docker cp backup-20240101.gz worldfaith-mongo:/tmp/restore.gz
docker exec worldfaith-mongo mongorestore --archive=/tmp/restore.gz --gzip
```

---

**Q: Muốn reset toàn bộ dữ liệu, làm sao?**

A: ⚠️ Sẽ xóa tất cả dữ liệu, không khôi phục được!
```bash
docker-compose down -v
docker-compose up worldfaith-mongo worldfaith-redis -d
```

---

**Q: Server chạy chậm, lag nhiều người chơi thì làm sao?**

A: Tăng tài nguyên server VPS. WorldFaith khuyến nghị:
- 10-20 người chơi: 4 CPU, 8GB RAM
- 50+ người chơi: 8 CPU, 16GB RAM, Redis cluster

---

**Q: Build Unity báo lỗi "IL2CPP" là gì?**

A: IL2CPP là chế độ build tối ưu hơn. Nếu lỗi, thử đổi sang **Mono** trong Build Settings:
- **File → Build Settings → Player Settings → Other Settings → Scripting Backend → Mono**

---

*Cần hỗ trợ thêm? Mở issue tại: https://github.com/thanhtinz/Game-new/issues*

*WorldFaith v1.0 — Được phát triển với ❤️*
