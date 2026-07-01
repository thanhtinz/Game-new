# WorldFaith — Hướng dẫn cài đặt & chạy (cho người mới)

> Tài liệu này hướng dẫn bạn **cài công cụ, dựng cơ sở dữ liệu, chạy Game Server và Admin Panel** từ con số 0.
> Mỗi bước đều có lệnh cụ thể và cách kiểm tra "đã đúng chưa". Cứ làm tuần tự từ trên xuống.

> 🎮 Muốn build **client Unity** cho người chơi? Đó là tài liệu riêng: **[UNITY_BUILD_GUIDE.md](./UNITY_BUILD_GUIDE.md)**.
> Bạn vẫn nên hoàn thành tài liệu này **trước** (server phải chạy thì client mới kết nối được).

---

## Mục lục

1. [Tổng quan: bạn sắp cài những gì](#1-tổng-quan-bạn-sắp-cài-những-gì)
2. [Cài đặt công cụ cần thiết](#2-cài-đặt-công-cụ-cần-thiết)
3. [Tải mã nguồn về máy](#3-tải-mã-nguồn-về-máy)
4. [Dựng cơ sở dữ liệu (MongoDB + Redis)](#4-dựng-cơ-sở-dữ-liệu-mongodb--redis)
5. [Cấu hình Server](#5-cấu-hình-server)
6. [Chạy Game Server](#6-chạy-game-server)
7. [Chạy Admin Panel](#7-chạy-admin-panel)
8. [Kiểm tra mọi thứ đã chạy đúng](#8-kiểm-tra-mọi-thứ-đã-chạy-đúng)
9. [Tài khoản đăng nhập mặc định](#9-tài-khoản-đăng-nhập-mặc-định)
10. [Công việc hằng ngày (start/stop/backup)](#10-công-việc-hằng-ngày-startstopbackup)
11. [Triển khai production (VPS)](#11-triển-khai-production-vps)
12. [Khắc phục sự cố (FAQ)](#12-khắc-phục-sự-cố-faq)

---

## 1. Tổng quan: bạn sắp cài những gì

WorldFaith gồm 4 phần. Trong tài liệu này bạn sẽ dựng **3 phần phía máy chủ**:

```
   ┌────────────────────────────────────────────────────────┐
   │  ① Cơ sở dữ liệu   →  MongoDB + Redis  (chạy bằng Docker) │
   │  ② Game Server     →  ASP.NET Core 10  (cổng 5000)        │
   │  ③ Admin Panel     →  Next.js          (cổng 3001)        │
   └────────────────────────────────────────────────────────┘
   ④ Client Unity → xem UNITY_BUILD_GUIDE.md (tài liệu riêng)
```

Thứ tự khởi động **luôn luôn** là: **① Database → ② Server → ③ Admin Panel**. Nếu chạy sai thứ tự, server sẽ báo lỗi không kết nối được database.

⏱️ **Thời gian dự kiến:** khoảng 30–45 phút cho lần đầu (phần lớn là chờ tải Docker image và biên dịch).

---

## 2. Cài đặt công cụ cần thiết

Bạn cần **3 công cụ** sau (Unity là ở tài liệu build client, không bắt buộc ở đây). Cài lần lượt, rồi mở terminal kiểm tra phiên bản.

### 2.1. .NET SDK 10 — để chạy Game Server

1. Vào https://dotnet.microsoft.com/download
2. Chọn **.NET 10.0** → tải bản cho hệ điều hành của bạn → cài đặt.
3. Mở terminal (Windows: PowerShell; Mac/Linux: Terminal) và kiểm tra:

```bash
dotnet --version
```

✅ **Đúng khi:** in ra số bắt đầu bằng `10.` (ví dụ `10.0.100`).

### 2.2. Docker Desktop — để chạy MongoDB + Redis

1. Vào https://www.docker.com/products/docker-desktop → tải về → cài đặt.
2. **Mở Docker Desktop** và chờ biểu tượng con cá voi 🐳 ngừng nhấp nháy (báo "Docker is running").
3. Kiểm tra:

```bash
docker --version
```

✅ **Đúng khi:** in ra `Docker version 24.x.x` (hoặc cao hơn).

> ⚠️ **Quan trọng:** Docker Desktop phải đang **mở và chạy** mỗi khi bạn làm việc với database. Nếu tắt, mọi lệnh `docker` sẽ báo lỗi "Cannot connect to the Docker daemon".

### 2.3. Node.js 20 LTS — để chạy Admin Panel

1. Vào https://nodejs.org → bấm nút **LTS** (nút bên trái) → cài đặt.
2. Kiểm tra:

```bash
node --version
```

✅ **Đúng khi:** in ra `v20.x.x` (hoặc cao hơn).

### Bảng tổng kết công cụ

| Công cụ | Phiên bản | Dùng để | Lệnh kiểm tra |
|---|---|---|---|
| .NET SDK | 10.0 | Chạy Game Server | `dotnet --version` |
| Docker Desktop | 24+ | Chạy MongoDB + Redis | `docker --version` |
| Node.js | 20 LTS | Chạy Admin Panel | `node --version` |
| Git | bất kỳ | Tải mã nguồn | `git --version` |

---

## 3. Tải mã nguồn về máy

Mở terminal tại thư mục bạn muốn lưu dự án, rồi chạy:

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
```

✅ **Đúng khi:** lệnh `ls` (Mac/Linux) hoặc `dir` (Windows) hiển thị các thư mục `server`, `client-unity`, `admin-panel`, `shared`…

> Từ giờ, mọi lệnh trong tài liệu này đều chạy **bên trong thư mục `Game-new`** (trừ khi nói khác).

---

## 4. Dựng cơ sở dữ liệu (MongoDB + Redis)

WorldFaith lưu dữ liệu game trong **MongoDB** và dùng **Redis** để cache realtime. Docker sẽ lo cả hai — bạn không cần cài tay.

### Bước 1 — Khởi động database

Đảm bảo Docker Desktop đang chạy, rồi:

```bash
docker-compose up worldfaith-mongo worldfaith-redis -d
```

- Lần đầu sẽ tải image (~3–5 phút). Hãy kiên nhẫn chờ.
- Tham số `-d` nghĩa là chạy nền (detached) — terminal sẽ không bị "khóa".

### Bước 2 — Kiểm tra database đã chạy

```bash
docker ps
```

✅ **Đúng khi:** bạn thấy **cả hai** dòng có trạng thái `Up`:

```
NAMES                STATUS
worldfaith-mongo     Up X minutes
worldfaith-redis     Up X minutes
```

❌ **Nếu thiếu một trong hai:** xem log để biết lỗi:

```bash
docker logs worldfaith-mongo --tail 20
```

> 🔍 **Tùy chọn — xem dữ liệu bằng giao diện:** tải [MongoDB Compass](https://www.mongodb.com/products/compass), kết nối tới `mongodb://localhost:27017`.

---

## 5. Cấu hình Server

Mở file `server/WorldFaith.Server/appsettings.json` bằng bất kỳ trình soạn thảo nào (VS Code, Notepad…). Nội dung mặc định:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "Redis":   "localhost:6379"
  },
  "Jwt": {
    "Secret":             "WorldFaith_SuperSecret_Key_MustBeAtLeast32Chars!",
    "Issuer":             "WorldFaith",
    "Audience":           "WorldFaithPlayers",
    "AccessTokenMinutes": "60",
    "RefreshTokenDays":   "30"
  },
  "Admin": {
    "Email":    "admin@worldfaith.game",
    "Password": "Admin@WorldFaith2024!"
  }
}
```

| Trường | Bạn cần làm gì |
|---|---|
| `MongoDB` / `Redis` | **Giữ nguyên** nếu chạy Docker trên máy local (như Mục 4). |
| `Jwt.Secret` | **Đổi trước khi triển khai thật** — dùng một chuỗi ngẫu nhiên từ 32 ký tự trở lên. |
| `Admin.Email` / `Admin.Password` | **Đổi trước khi triển khai thật.** Khi chạy thử trên máy thì giữ nguyên cũng được. |

> 💡 Nếu chỉ đang chạy thử trên máy mình, bạn **không cần sửa gì** ở bước này — cứ để mặc định và sang Mục 6.

---

## 6. Chạy Game Server

```bash
cd server/WorldFaith.Server
dotnet run
```

- Lần biên dịch đầu mất khoảng **30–60 giây**. Đây là điều bình thường.
- Server sẽ tự tạo (seed) cấu hình cân bằng và tài khoản admin trong lần chạy đầu.

✅ **Đúng khi:** bạn thấy các dòng log như sau:

```
[INF] WorldFaith Server starting up
[INF] Balance config seeded (90 params)
[INF] Admin account seeded: admin@worldfaith.game
[INF] Now listening on: http://localhost:5000
```

### Kiểm tra server còn sống

Mở trình duyệt, vào địa chỉ: **http://localhost:5000/health**

✅ **Đúng khi:** trang hiển thị `{"status":"ok"}`.

### Dừng server

Quay lại terminal đang chạy server, nhấn **`Ctrl + C`**.

> 🐳 **Cách khác — chạy tất cả bằng Docker:** nếu không muốn cài .NET, bạn có thể chạy database + server cùng lúc:
> ```bash
> docker-compose up -d        # khởi động DB + Server ở chế độ nền
> docker-compose logs -f      # xem log (Ctrl+C để thoát xem log, server vẫn chạy)
> docker-compose down         # dừng tất cả
> ```

---

## 7. Chạy Admin Panel

Admin Panel là trang web quản trị (xem thống kê, chỉnh thế giới, quản lý người chơi…).

### Bước 1 — Cài thư viện

Mở **một terminal mới** (giữ server ở Mục 6 vẫn chạy), rồi:

```bash
cd admin-panel
npm install
```

Lần đầu mất 1–3 phút để tải thư viện.

### Bước 2 — Tạo file cấu hình `.env.local`

Admin Panel cần biết địa chỉ server. Tạo file `admin-panel/.env.local`:

**Mac / Linux:**
```bash
echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > .env.local
```

**Windows:** mở Notepad, gõ dòng dưới, rồi **Save As** với tên `.env.local` (chọn "All Files" để không bị thêm đuôi `.txt`):
```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Bước 3 — Chạy Admin Panel

```bash
npm run dev
```

✅ **Đúng khi:** terminal báo `ready - started server on http://localhost:3001`.

Mở trình duyệt: **http://localhost:3001** → đăng nhập bằng tài khoản admin mặc định (xem [Mục 9](#9-tài-khoản-đăng-nhập-mặc-định)).

---

## 8. Kiểm tra mọi thứ đã chạy đúng

Chạy lần lượt 4 bước kiểm tra dưới đây. Nếu cả 4 đều ✅ thì hệ thống máy chủ đã sẵn sàng.

**① Database đang chạy:**
```bash
docker ps
```
✅ Cả `worldfaith-mongo` và `worldfaith-redis` đều `Up`.

**② Server phản hồi:**
```bash
curl http://localhost:5000/health
```
✅ Trả về `{"status":"ok"}`.

**③ Admin Panel mở được:**
- Vào http://localhost:3001 → đăng nhập → trang Dashboard hiện ra với số liệu server.

**④ (Sau khi build client Unity) Game kết nối được:**
- Mở client Unity → đăng ký tài khoản → đăng nhập → tạo phòng → bắt đầu game.
- Quay lại Admin Panel → Dashboard → mục **Active Worlds** hiển thị `1`.

> Bước ④ cần client Unity. Xem **[UNITY_BUILD_GUIDE.md](./UNITY_BUILD_GUIDE.md)** để build client.

---

## 9. Tài khoản đăng nhập mặc định

| | |
|---|---|
| **Email** | `admin@worldfaith.game` |
| **Mật khẩu** | `Admin@WorldFaith2024!` |
| **Quyền** | Admin (toàn quyền truy cập Admin Panel) |

> ⚠️ **Đổi mật khẩu** trong `server/WorldFaith.Server/appsettings.json` → `Admin.Password` **trước khi triển khai production**. Mật khẩu mặc định ai cũng biết.

---

## 10. Công việc hằng ngày (start/stop/backup)

### Bật / tắt database

```bash
docker-compose stop  worldfaith-mongo worldfaith-redis   # tắt
docker-compose start worldfaith-mongo worldfaith-redis   # bật lại
docker logs worldfaith-mongo --tail 20                   # xem log khi gặp lỗi
```

### Sao lưu (backup) dữ liệu

```bash
docker exec worldfaith-mongo mongodump --db worldfaith --archive=/tmp/bk.gz --gzip
docker cp worldfaith-mongo:/tmp/bk.gz ./backup-$(date +%Y%m%d).gz
```

### Phục hồi (restore) dữ liệu

```bash
docker cp my-backup.gz worldfaith-mongo:/tmp/restore.gz
docker exec worldfaith-mongo mongorestore --archive=/tmp/restore.gz --gzip
```

### Xóa sạch toàn bộ dữ liệu (làm lại từ đầu)

```bash
docker-compose down -v   # ⚠️ XÓA VĨNH VIỄN toàn bộ dữ liệu game
docker-compose up worldfaith-mongo worldfaith-redis -d
# Chạy lại server để seed lại tài khoản admin
```

---

## 11. Triển khai production (VPS)

Phần này dành cho khi bạn muốn đưa game lên một máy chủ thật (VPS) cho nhiều người chơi.

### Yêu cầu máy chủ

| Số người chơi | CPU | RAM | Băng thông |
|---|---|---|---|
| 2–10 | 2 nhân | 4 GB | 10 Mbps |
| 10–30 | 4 nhân | 8 GB | 20 Mbps |
| 30+ | 8 nhân | 16 GB | 50 Mbps |

Hệ điều hành khuyến nghị: **Ubuntu 22.04 LTS**.

### Bước 1 — Cài Docker trên VPS

```bash
ssh user@YOUR_VPS_IP
sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER
exit && ssh user@YOUR_VPS_IP   # đăng nhập lại để áp dụng quyền group
docker --version
```

### Bước 2 — Tải mã nguồn lên và cấu hình

```bash
scp -r Game-new/ user@YOUR_VPS_IP:/opt/worldfaith/
ssh user@YOUR_VPS_IP
cd /opt/worldfaith

cat > .env << 'EOF'
JWT_SECRET=Thay_Bang_Chuoi_Ngau_Nhien_Tu_32_Ky_Tu_Tro_Len
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=MatKhauManh@2024!
EOF

docker-compose up -d
docker-compose logs -f   # chờ dòng "Now listening on"
curl http://localhost:5000/health
```

### Bước 3 — Reverse proxy bằng Nginx

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
sudo nginx -t && sudo systemctl reload nginx
```

> Dòng `Upgrade`/`Connection "upgrade"` là **bắt buộc** để SignalR (WebSocket) hoạt động qua Nginx.

### Bước 4 — Bật HTTPS miễn phí (Let's Encrypt)

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
# Làm theo hướng dẫn, chọn "redirect HTTP to HTTPS"
```

### Bước 5 — Cập nhật địa chỉ server trong client Unity

Trong mỗi scene Unity (LoginScene, LobbyScene, GameScene), tìm các GameObject mạng và đổi trường `Server Url`:

| Trước (local) | Sau (production) |
|---|---|
| `http://localhost:5000` | `https://api.yourdomain.com` |
| `http://localhost:5000/hubs/world` | `https://api.yourdomain.com/hubs/world` |
| `http://localhost:5000/hubs/lobby` | `https://api.yourdomain.com/hubs/lobby` |
| `http://localhost:5000/hubs/chat`  | `https://api.yourdomain.com/hubs/chat` |

Sau đó **build lại client** cho nền tảng mục tiêu (xem [UNITY_BUILD_GUIDE.md](./UNITY_BUILD_GUIDE.md)).

---

## 12. Khắc phục sự cố (FAQ)

**Q: Lỗi "Cannot connect to the Docker daemon"?**
A: Docker Desktop chưa mở. Mở nó lên, chờ biểu tượng cá voi 🐳 ngừng nhấp nháy (có thể mất 30–60 giây), rồi thử lại.

**Q: Server báo "Unable to connect to MongoDB"?**
A: Database chưa chạy. Chạy: `docker-compose up worldfaith-mongo worldfaith-redis -d` rồi khởi động lại server.

**Q: Admin Panel báo 401 Unauthorized?**
A: Phiên đăng nhập hết hạn (token sống 60 phút). Đăng xuất rồi đăng nhập lại. Đồng thời kiểm tra file `.env.local` có đúng `NEXT_PUBLIC_API_URL=http://localhost:5000`.

**Q: Sửa Balance Config nhưng không thấy thay đổi?**
A: Chờ tối đa 60 giây để cache hết hạn. Nếu vẫn không được, khởi động lại server.

**Q: Cổng 5000 hoặc 3001 đã bị chiếm?**
A: Có chương trình khác đang dùng cổng. Tắt chương trình đó, hoặc đổi cổng trong cấu hình rồi cập nhật lại địa chỉ tương ứng.

**Q: Chơi trong mạng LAN, máy khác kết nối thế nào?**
A: Tìm địa chỉ IP LAN của máy chạy server: `ipconfig` (Windows) hoặc `ifconfig | grep inet` (Mac/Linux). Trong client Unity, đổi Server URL từ `http://localhost:5000/...` thành `http://192.168.x.x:5000/...` (IP thật của bạn), rồi build lại và chia sẻ.

**Q: Làm sao reset toàn bộ dữ liệu game?**
A: Xem [Mục 10 — "Xóa sạch toàn bộ dữ liệu"](#10-công-việc-hằng-ngày-startstopbackup). Lưu ý `docker-compose down -v` xóa vĩnh viễn, không khôi phục được.

---

<p align="center">
  Cần trợ giúp? Mở issue tại <a href="https://github.com/thanhtinz/Game-new/issues">github.com/thanhtinz/Game-new/issues</a><br>
  Tiếp theo → <a href="./UNITY_BUILD_GUIDE.md">Build client Unity từng bước</a>
</p>
