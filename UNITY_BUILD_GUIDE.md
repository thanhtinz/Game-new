# WorldFaith — Hướng dẫn build Client Unity (cho người mới)

> Tài liệu này hướng dẫn bạn **mở project Unity, cài thư viện, tạo scene, gán tài nguyên và build game** cho PC / Android / iOS / WebGL.
> Mỗi bước đều ghi rõ: **làm gì → bấm vào đâu → kết quả mong đợi**. Nếu là lần đầu chạm vào Unity, cứ làm tuần tự, đừng bỏ bước.

> 🖥️ **Trước khi bắt đầu:** Game Server phải chạy được thì client mới kết nối. Hãy hoàn thành **[SETUP.md](./SETUP.md)** trước.

---

## ⚠️ Đọc cái này trước (rất quan trọng)

Có **3 thứ thủ công** mà Unity *không* tự làm cho bạn. Thiếu một trong ba là client sẽ không chạy:

| # | Việc thủ công | Vì sao cần | Ở bước nào |
|---|---|---|---|
| 1 | Cài **DLL SignalR** | Thư viện kết nối realtime với server | [Bước 4](#bước-4--cài-signalr-kết-nối-realtime) |
| 2 | Sao chép **Shared Library** vào Unity | Enum/model dùng chung server ↔ client | [Bước 5](#bước-5--sao-chép-shared-library) |
| 3 | Tự **tạo 3 scene** bằng menu WorldFaith | Project không kèm sẵn scene | [Bước 6](#bước-6--tạo-3-scene) |

Nhớ 3 thứ này — khi gặp lỗi "not found" sau này, 90% là do một trong ba bước trên chưa làm xong.

---

## Mục lục

- [Phần A — Chuẩn bị](#phần-a--chuẩn-bị)
  - [Bước 1 — Cài Unity và công cụ](#bước-1--cài-unity-và-công-cụ)
  - [Bước 2 — Mở project](#bước-2--mở-project)
  - [Bước 3 — Cài các package Unity](#bước-3--cài-các-package-unity)
  - [Bước 4 — Cài SignalR (kết nối realtime)](#bước-4--cài-signalr-kết-nối-realtime)
  - [Bước 5 — Sao chép Shared Library](#bước-5--sao-chép-shared-library)
- [Phần B — Dựng scene & cấu hình](#phần-b--dựng-scene--cấu-hình)
  - [Bước 6 — Tạo 3 scene](#bước-6--tạo-3-scene)
  - [Bước 7 — Cấu hình địa chỉ Server](#bước-7--cấu-hình-địa-chỉ-server)
  - [Bước 8 — Gán tài nguyên (ảnh ô đất, âm thanh…)](#bước-8--gán-tài-nguyên-ảnh-ô-đất-âm-thanh)
  - [Bước 9 — Kiểm tra setup bằng Validator](#bước-9--kiểm-tra-setup-bằng-validator)
  - [Bước 10 — Chạy thử trong Editor](#bước-10--chạy-thử-trong-editor)
- [Phần C — Build cho từng nền tảng](#phần-c--build-cho-từng-nền-tảng)
  - [Bước 11 — Build cho PC (Windows/Mac/Linux)](#bước-11--build-cho-pc)
  - [Bước 12 — Build cho Android](#bước-12--build-cho-android)
  - [Bước 13 — Build cho iOS (chỉ trên Mac)](#bước-13--build-cho-ios-chỉ-trên-mac)
  - [Bước 14 — Build cho WebGL (trình duyệt)](#bước-14--build-cho-webgl-trình-duyệt)
- [Phần D — Tra cứu](#phần-d--tra-cứu)
  - [Khắc phục sự cố](#khắc-phục-sự-cố)
  - [Checklist trước khi build](#checklist-trước-khi-build)

---

# Phần A — Chuẩn bị

## Bước 1 — Cài Unity và công cụ

Bạn cần các công cụ sau **trước khi mở Unity**:

| Công cụ | Phiên bản | Tải ở đâu |
|---|---|---|
| Unity Hub | Mới nhất | https://unity.com/download |
| Unity Editor | **6.3 LTS** | Cài qua Unity Hub |
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Git | bất kỳ | https://git-scm.com |

### Cài Unity Editor 6.3 LTS

1. Mở **Unity Hub** → đăng nhập → tab **Installs** → bấm **Install Editor**.
2. Chọn **Unity 6.3 LTS** → bấm **Install**. (Mất 15–30 phút.)
3. Khi được hỏi chọn **modules** (các thành phần bổ sung), tick những cái bạn cần:
   - ☑️ **Android Build Support** (gồm Android SDK & NDK + OpenJDK) — nếu muốn build Android.
   - ☑️ **iOS Build Support** — *chỉ có trên Mac* — nếu muốn build iPhone/iPad.
   - ☑️ **WebGL Build Support** — nếu muốn build chạy trên trình duyệt.

> 💡 Quên tick module? Vào lại sau bằng: Unity Hub → **Installs** → bấm ⚙️ (bánh răng) bên cạnh bản 6.3 → **Add Modules**.

✅ **Hoàn thành bước này khi:** Unity Hub hiển thị bản **6.3 LTS** trong tab Installs.

---

## Bước 2 — Mở project

```
1. Mở Unity Hub
2. Bấm  Open  →  Add project from disk
3. Trỏ tới thư mục:  Game-new/client-unity/
4. CHỌN CHÍNH thư mục đó (đừng đi vào bên trong nó)
5. Bấm  Add Project
6. Bấm vào project vừa thêm để mở
7. Nếu hỏi phiên bản Editor → chọn  Unity 6.3 LTS
```

⏱️ **Lần mở đầu mất 5–15 phút** để Unity import. Trong lúc này, **Console sẽ hiện lỗi đỏ — đây là điều BÌNH THƯỜNG.** Lỗi sẽ tự hết sau khi bạn hoàn tất Bước 3, 4, 5.

✅ **Hoàn thành bước này khi:** giao diện Unity Editor đã mở, thanh tiến trình góc dưới-phải đã chạy xong.

---

## Bước 3 — Cài các package Unity

> ℹ️ **4 package bên dưới đã được khai báo sẵn trong `client-unity/Packages/manifest.json`.** Unity sẽ tự tải chúng khi bạn mở project lần đầu (Bước 2) — không cần bấm Install thủ công cho từng cái. Các bước 3a–3d dưới đây chỉ còn **một hành động thủ công thật sự**: import **TMP Essential Resources** ở Bước 3b (đây là import tài nguyên, không phải cài package, nên `manifest.json` không tự làm được). Phần còn lại chỉ để bạn **kiểm tra** package đã có trong **In Project**.

Mở **Window → Package Management → Package Manager**.

> 📝 Trong Unity 6, menu này nằm ở **Window → Package Management → Package Manager**. (Ở Unity 2022 cũ hơn thì nó nằm thẳng ở **Window → Package Manager**.)

Kiểm tra/cài lần lượt **4 package** sau:

### 3a. Input System *(bắt buộc)*

Script `CameraController.cs` dùng **Input System** để xử lý kéo/zoom/chạm-chọn camera. Thiếu nó thì điều khiển camera sẽ không hoạt động.

1. Trong Package Manager, đổi dropdown từ **In Project** sang **Unity Registry**.
2. Tìm `Input System` → bấm **Install**.
3. Nếu hỏi bật input backend mới và khởi động lại Editor → bấm **Yes**.

### 3b. TextMeshPro *(bắt buộc)*

1. Đổi dropdown sang **Unity Registry** → tìm `TextMeshPro` → **Install**.
2. Cài xong, vào **Window → TextMeshPro → Import TMP Essential Resources** → bấm **Import**.

### 3c. Newtonsoft JSON *(bắt buộc)*

1. Bấm nút **+** (góc trên-trái Package Manager) → **Add package by name**.
2. Gõ chính xác: `com.unity.nuget.newtonsoft-json` → **Add**.

### 3d. Mobile Notifications *(cho thông báo đẩy Android/iOS)*

1. Bấm **+** → **Add package by name**.
2. Gõ: `com.unity.mobile.notifications` → **Add**.

⏳ Chờ thanh tiến trình góc dưới-phải chạy xong giữa mỗi lần cài.

✅ **Hoàn thành bước này khi:** cả 4 package xuất hiện trong danh sách **In Project** của Package Manager, không còn lỗi đỏ liên quan đến chúng.

---

## Bước 4 — Cài SignalR (kết nối realtime)

SignalR là thư viện giúp client nói chuyện realtime với server. Nó **không có** trong Package Manager nên phải cài tay (dùng .NET để tải về rồi chép 6 file DLL vào Unity).

### Mac / Linux

Chạy từ thư mục gốc `Game-new`:

```bash
cd Game-new

# Tạo thư mục chứa plugin
mkdir -p client-unity/Assets/Plugins/SignalR

# Tạo project .NET tạm và tải SignalR về
cd /tmp
mkdir signalr_temp && cd signalr_temp
dotnet new console -n sr -o sr
cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 10.0.9
dotnet publish -c Release -o pub

# Chép 6 DLL vào Unity
TARGET="<ĐƯỜNG_DẪN_TỚI>/Game-new/client-unity/Assets/Plugins/SignalR"
cp pub/Microsoft.AspNetCore.SignalR.Client.dll           "$TARGET/"
cp pub/Microsoft.AspNetCore.SignalR.Client.Core.dll      "$TARGET/"
cp pub/Microsoft.AspNetCore.SignalR.Common.dll           "$TARGET/"
cp pub/Microsoft.AspNetCore.SignalR.Protocols.Json.dll   "$TARGET/"
cp pub/Microsoft.AspNetCore.Http.Connections.Client.dll  "$TARGET/"
cp pub/Microsoft.AspNetCore.Http.Connections.Common.dll  "$TARGET/"

# Dọn dẹp
cd /tmp && rm -rf signalr_temp
echo "Xong. Đã cài 6 DLL."
```

> ⚠️ Thay `<ĐƯỜNG_DẪN_TỚI>` bằng đường dẫn thật tới thư mục `Game-new` trên máy bạn (ví dụ `/home/ban/Game-new`).

### Windows (PowerShell)

```powershell
cd Game-new

# Tạo thư mục chứa plugin
New-Item -ItemType Directory -Force -Path "client-unity\Assets\Plugins\SignalR"

# Tạo project .NET tạm
cd $env:TEMP
mkdir signalr_temp; cd signalr_temp
dotnet new console -n sr -o sr
cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 10.0.9
dotnet publish -c Release -o pub

# Chép 6 DLL (thay đường dẫn cho đúng máy bạn)
$target = "C:\duong\dan\Game-new\client-unity\Assets\Plugins\SignalR"
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Client.dll"           $target
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Client.Core.dll"      $target
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Common.dll"           $target
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Protocols.Json.dll"   $target
Copy-Item "pub\Microsoft.AspNetCore.Http.Connections.Client.dll"  $target
Copy-Item "pub\Microsoft.AspNetCore.Http.Connections.Common.dll"  $target

cd $env:TEMP; Remove-Item -Recurse -Force signalr_temp
Write-Host "Xong."
```

✅ **Hoàn thành bước này khi:** trong Unity, mở **Project window → Assets → Plugins → SignalR**, bạn thấy **đủ 6 file `.dll`**.

---

## Bước 5 — Sao chép Shared Library

Shared Library chứa các enum, model, contract mà **cả server lẫn client đều dùng**. Unity cần một bản sao mã nguồn này.

### Mac / Linux

```bash
cd Game-new

SRC="shared/WorldFaith.Shared"
DST="client-unity/Assets/WorldFaith/Shared"

mkdir -p "$DST"
cp -r "$SRC/Enums"     "$DST/"
cp -r "$SRC/Models"    "$DST/"
cp -r "$SRC/Contracts" "$DST/"

echo "Đã sao chép Shared Library."
```

### Windows (PowerShell)

```powershell
cd Game-new
$src = "shared\WorldFaith.Shared"
$dst = "client-unity\Assets\WorldFaith\Shared"

New-Item -ItemType Directory -Force -Path $dst
Copy-Item "$src\Enums","$src\Models","$src\Contracts" $dst -Recurse -Force
Write-Host "Đã sao chép Shared Library."
```

> 🔄 **Lưu ý đồng bộ:** thư mục Shared trong Unity là **bản sao**, không phải liên kết. Mỗi khi server cập nhật shared library, chạy lại lệnh chép trên.

⏳ **Chờ thanh tiến trình của Unity chạy xong.** Console có thể báo lỗi trong lúc import — lỗi sẽ hết khi biên dịch xong.

✅ **Hoàn thành bước này khi:** Console **không còn lỗi đỏ**, và menu **WorldFaith** xuất hiện trên thanh menu trên cùng của Unity. (Nếu chưa thấy menu, bấm **Assets → Refresh** — phím tắt `Ctrl+R` / `Cmd+R`.)

---

# Phần B — Dựng scene & cấu hình

## Bước 6 — Tạo 3 scene

WorldFaith dùng **3 scene** nạp theo thứ tự cố định. Project không kèm sẵn — bạn phải tự tạo bằng công cụ Setup tích hợp.

> Mỗi scene làm theo đúng 4 thao tác: **tạo scene mới → lưu vào `Assets/Scenes/` → chạy menu Setup tương ứng → lưu lại (Ctrl+S)**.

### Scene 1 — LoginScene

```
1. File → New Scene → Basic (Built-in) → Create
2. File → Save As...  → vào thư mục  Assets/Scenes/  → đặt tên  LoginScene  → Save
3. Menu trên cùng:  WorldFaith → Setup → Create Login Scene Objects
4. File → Save  (Ctrl+S / Cmd+S)
```
*Tạo ra:* `AuthManager`, `MainThreadDispatcher`, `LobbyClient`, Canvas có `LoginUI`.

### Scene 2 — LobbyScene

```
1. File → New Scene → Basic (Built-in) → Create
2. File → Save As...  → Assets/Scenes/  → tên  LobbyScene  → Save
3. WorldFaith → Setup → Create Lobby Scene Objects
4. File → Save
```
*Tạo ra:* `AuthManager`, `LobbyClient`, `MainThreadDispatcher`, Canvas có `LobbyUI`, `GodSelectionScreen`.

### Scene 3 — GameScene

```
1. File → New Scene → Basic (Built-in) → Create
2. File → Save As...  → Assets/Scenes/  → tên  GameScene  → Save
3. WorldFaith → Setup → Create Game Scene Objects
4. File → Save
```
*Tạo ra:* toàn bộ network client, các manager, `WorldRenderer`, `CameraController`, mọi panel UI.

> ❓ **Không thấy menu WorldFaith?** Nó chỉ hiện sau khi Shared Library (Bước 5) import xong. Bấm **Assets → Refresh** (`Ctrl+R`). Nếu vẫn không có, kiểm tra Console — sửa hết lỗi đỏ trước đã.

✅ **Hoàn thành bước này khi:** thư mục `Assets/Scenes/` có đủ 3 file: `LoginScene`, `LobbyScene`, `GameScene`.

---

## Bước 7 — Cấu hình địa chỉ Server

Mỗi script mạng có một trường `serverUrl` hiện trong **Inspector**. Bạn cần đặt địa chỉ này trong mọi scene chứa script đó.

> ⚠️ **Đặt qua Inspector — KHÔNG sửa file `.cs` trực tiếp.** Mở scene → chọn GameObject trong Hierarchy → sửa trường trong Inspector.

### Khi chạy local (trên máy mình)

| GameObject | Script | Trường | Giá trị |
|---|---|---|---|
| `AuthManager` | `AuthManager.cs` | Server Url | `http://localhost:5000` |
| `WorldFaithClient` | `WorldFaithClient.cs` | Server Url | `http://localhost:5000/hubs/world` |
| `LobbyClient` | `LobbyClient.cs` | Server Url | `http://localhost:5000/hubs/lobby` |
| `ChatClient` | `ChatClient.cs` | Server Url | `http://localhost:5000/hubs/chat` |

Các GameObject này nằm trong những scene khác nhau — mở từng scene và đặt URL:

| Scene | GameObject cần đặt URL |
|---|---|
| LoginScene | `AuthManager` |
| LobbyScene | `AuthManager`, `LobbyClient` |
| GameScene | `WorldFaithClient`, `ChatClient`, `AuthManager` |

### Khi chơi LAN (nhiều máy cùng mạng)

Thay `localhost` bằng IP LAN của máy chạy server:

```bash
# Tìm IP LAN:
# Mac/Linux:
ifconfig | grep "inet " | grep -v 127.0.0.1
# Windows:
ipconfig | findstr "IPv4"
```
Ví dụ: `http://192.168.1.42:5000/hubs/world`.

### Khi triển khai production

Thay bằng tên miền đã deploy, ví dụ `http://localhost:5000` → `https://api.yourdomain.com`.

> ⚠️ **Đổi URL xong phải build lại client** — URL được "nướng" vào bản build lúc biên dịch.

---

## Bước 8 — Gán tài nguyên (ảnh ô đất, âm thanh…)

Game cần một số tài nguyên tối thiểu để hiển thị. Danh sách đầy đủ (~204 file) ở **[ASSETS.md](./ASSETS.md)**.

### 8a. Ảnh ô đất (bắt buộc — không có thì bản đồ không hiện)

Đặt file PNG 64×64 vào `Assets/WorldFaith/World/Tiles/`. Cần đủ **10 loại**:

| File | Gợi ý màu | | File | Gợi ý màu |
|---|---|---|---|---|
| `tile_grassland.png` | `#4a9c2f` | | `tile_water.png` | `#2a64c8` |
| `tile_forest.png` | `#1a5c1a` | | `tile_volcano.png` | `#c83210` |
| `tile_mountain.png` | `#7a7a7a` | | `tile_sacred.png` | `#c8a832` |
| `tile_desert.png` | `#c8b44a` | | `tile_beach.png` | `#e6d7a0` (bờ cát) |
| `tile_tundra.png` | `#b0c8e0` | | `tile_river.png` | `#468cdc` (sáng hơn Water) |

**Cách import mỗi ảnh ô đất:**
1. Kéo PNG vào `Assets/WorldFaith/World/Tiles/`.
2. Chọn ảnh trong Project window → Inspector → **Texture Type: Sprite (2D and UI)** → **Pixels Per Unit: 64** → **Apply**.
3. Trong Hierarchy của GameScene, chọn `WorldRenderer` → kéo từng sprite vào ô tương ứng (Grassland Sprite, Forest Sprite…).
4. Gán luôn **Temple Sprite** (icon đền) và **City Marker Sprite** (chấm/cờ đánh dấu thành phố).

### 8b. Âm thanh (bắt buộc để có tiếng)

- **47 hiệu ứng (SFX)** → `Assets/WorldFaith/Audio/SFX/`. Trong GameScene chọn `AudioManager` → mở mảng **Sfx Clips[]** → gán từng file theo đúng thứ tự enum `SfxId` (xem `Assets/WorldFaith/Shared/Enums/`).
- **5 nhạc nền** → `Assets/WorldFaith/Audio/Music/`, gán vào `AudioManager`:

| File | Trường trong AudioManager |
|---|---|
| `music_base.mp3` | Music Base |
| `music_religion.mp3` | Music Religion |
| `music_war.mp3` | Music War |
| `music_apocalypse.mp3` | Music Apocalypse |
| `music_victory.mp3` | Music Victory |

### 8c. Camera 2D (thường tự cấu hình sẵn)

Công cụ Setup đã đặt Main Camera ở chế độ **Orthographic** (chiếu thẳng từ trên xuống). Nếu bạn đổi kích thước bản đồ khác `128×128`, chỉnh trên `CameraController`:

| Trường | Mặc định | Ý nghĩa |
|---|---|---|
| Pan Limit Min | `(-2, -2)` | Giới hạn kéo camera nhỏ nhất |
| Pan Limit Max | `(130, 130)` | Giới hạn kéo lớn nhất — nên hơi lớn hơn `kích_thước_bản_đồ × tileSize` |
| Zoom Min / Max | `3` / `60` | Mức zoom gần nhất / xa nhất |

### 8d. Khuyến nghị (có thì đẹp hơn, không có vẫn chạy)

| Loại | Vị trí |
|---|---|
| 28 VFX Prefab (Particle) | `Assets/WorldFaith/VFX/Prefabs/` → gán vào `VfxManager → Catalog[]` theo thứ tự enum `VfxId` |
| 8 icon nguyên mẫu Thần (128×128) | `Assets/WorldFaith/UI/Sprites/` |
| 15 icon phép màu (64×64) | `Assets/WorldFaith/UI/Sprites/` |
| Font (Cinzel, Nunito, Rajdhani) | Tải từ fonts.google.com → **Window → TextMeshPro → Font Asset Creator** → Generate → Save |

> Nguồn tài nguyên miễn phí: SFX (freesound.org, kenney.nl) · Nhạc (incompetech.com) · Sprite (kenney.nl, game-icons.net) · Font (fonts.google.com).

---

## Bước 9 — Kiểm tra setup bằng Validator

Trước khi build, chạy công cụ kiểm tra tích hợp để bắt lỗi thiếu tham chiếu:

```
Menu:  WorldFaith → Validate → Check All Managers
```

✅ **Đúng khi:** mọi mục đều dấu tích xanh trong Console.

Các cảnh báo thường gặp và cách xử lý:

| Cảnh báo | Cách xử lý |
|---|---|
| `AudioManager: SFX clips not assigned` | Gán audio clip trong Inspector của AudioManager (Bước 8b) |
| `VfxManager: catalog empty` | Gán VFX prefab, hoặc bỏ qua nếu chưa có VFX |
| `WorldRenderer: tile prefabs missing` | Gán ảnh ô đất (Bước 8a) |
| `SignalR DLLs not found` | Làm lại Bước 4 |

---

## Bước 10 — Chạy thử trong Editor

Luôn chạy thử trong Play mode trước khi build.

**1) Khởi động server trước** (xem [SETUP.md](./SETUP.md)):
```bash
cd Game-new/server/WorldFaith.Server
dotnet run
```

**2) Trong Unity:**
```
1. Mở LoginScene
2. Bấm nút Play (▶) ở giữa phía trên
3. Màn hình đăng nhập hiện ra
4. Bấm "Register" → điền username, email, password, display name → Register
5. Đăng nhập → màn hình Lobby hiện ra
6. Bấm "Create Room" → tạo phòng
7. Bấm "Start" (nếu chỉ có một mình, có thể chưa start được — chỉnh số người tối thiểu trong Balance Config của Admin Panel)
```

✅ **Đúng khi:** đăng nhập và vào được Lobby, Console không có lỗi đỏ trong lúc Play.

Lỗi thường gặp khi Play:
- `Connection refused` → server chưa chạy, hoặc URL sai (Bước 7).
- `401 Unauthorized` → token hết hạn, đăng nhập lại.
- `NullReferenceException on XxxManager` → một `[SerializeField]` chưa được gán.

---

# Phần C — Build cho từng nền tảng

> Mọi nền tảng đều chung 2 việc đầu: **(a)** thêm 3 scene vào Build Settings đúng thứ tự, **(b)** chọn nền tảng rồi **Switch Platform**. Sau đó mỗi nền tảng có vài tùy chỉnh riêng.

**Thêm scene vào Build Settings (làm một lần):**
```
File → Build Settings → Add Open Scenes (hoặc kéo thủ công) theo đúng thứ tự:
   0  Assets/Scenes/LoginScene
   1  Assets/Scenes/LobbyScene
   2  Assets/Scenes/GameScene
```

---

## Bước 11 — Build cho PC

### Windows
```
File → Build Settings
```
1. Platform: **PC, Mac & Linux Standalone** → Target Platform: **Windows** → Architecture: **x86_64**.
2. Bấm **Build** → chọn thư mục xuất (ví dụ `Builds/Windows/`).

*Kết quả:* thư mục chứa `WorldFaith.exe` + thư mục `WorldFaith_Data/`. **Phải giữ cả hai cùng nhau** khi chia sẻ.

### Mac
Như trên, nhưng Target Platform: **macOS**, Architecture: **Intel 64-bit + Apple Silicon** (universal) hoặc **Apple Silicon**.
*Kết quả:* `WorldFaith.app`. Lần đầu mở: chuột phải → **Open** để bỏ qua Gatekeeper.

### Linux
Target Platform: **Linux**, Architecture: **x86_64**.
*Kết quả:* file `WorldFaith.x86_64`. Cấp quyền chạy:
```bash
chmod +x WorldFaith.x86_64
./WorldFaith.x86_64
```

---

## Bước 12 — Build cho Android

### 12a. Bật chế độ nhà phát triển trên điện thoại (một lần)
```
1. Settings → About Phone
2. Tìm "Build Number" → chạm 7 lần → "Developer Mode enabled"
3. Settings → Developer Options → bật "USB Debugging"
4. Cắm điện thoại vào máy tính qua cáp USB
5. Trên điện thoại, bấm "Allow" khi hỏi USB debugging
```

### 12b. Player Settings
**File → Build Settings → Player Settings** (nút góc dưới-trái):

| Mục | Trường | Giá trị |
|---|---|---|
| Other Settings | Package Name | `com.tenban.worldfaith` (chữ thường, không dấu cách) |
| Other Settings | Version | `1.0` |
| Other Settings | Bundle Version Code | `1` |
| Other Settings | Minimum API Level | **Android 8.0 (API 26)** |
| Other Settings | Scripting Backend | **IL2CPP** |
| Other Settings | Target Architectures | **ARMv7** + **ARM64** |

### 12c. Build
```
Build Settings → Platform: Android → Switch Platform (chờ reimport)
→ cắm điện thoại → Build And Run  (build và cài thẳng lên máy)
```
Hoặc bấm **Build** để xuất file `.apk` rồi cài thủ công: `adb install -r WorldFaith.apk`.

> **Lên Google Play:** Player Settings → Publishing Settings → bật **Custom Keystore** (tạo keystore ký), rồi trong Build Settings bật **Build App Bundle (.aab)** → Build → tải `.aab` lên Google Play Console.

| Lỗi Android | Cách xử lý |
|---|---|
| Không nhận điện thoại | Bật lại USB Debugging, đổi cáp USB |
| `INSTALL_FAILED_VERSION_DOWNGRADE` | Gỡ bản cũ trên máy trước |
| Lỗi NDK khi build | Unity Hub → Installs → Add Modules → Android Build Support |
| Mở app là crash | Chạy `adb logcat -s Unity` xem log lỗi |
| `Failed to connect to server` | Dùng IP LAN thay cho `localhost` ở Bước 7 |

---

## Bước 13 — Build cho iOS (chỉ trên Mac)

Build iOS bắt buộc dùng máy Mac có **Xcode**.

### 13a. Chuẩn bị
- **Xcode 14+** (cài từ Mac App Store, miễn phí, ~10 GB).
- **Tài khoản Apple Developer** (miễn phí để test trên thiết bị; $99/năm để lên App Store).
- Một iPhone/iPad thật để test (hoặc dùng Simulator của Xcode).

### 13b. Player Settings

| Mục | Trường | Giá trị |
|---|---|---|
| Other Settings | Bundle Identifier | `com.tenban.worldfaith` |
| Other Settings | Version | `1.0` |
| Other Settings | Scripting Backend | **IL2CPP** |

### 13c. Build trong Unity
```
Build Settings → Platform: iOS → Switch Platform → Build
→ Unity tạo ra một thư mục project Xcode (ví dụ Builds/iOS/)
```

### 13d. Hoàn tất trong Xcode
```
1. Mở  Builds/iOS/Unity-iPhone.xcodeproj
2. Chọn project (icon xanh) ở cột trái → tab  Signing & Capabilities
3. Ở  Team  → chọn tài khoản Apple Developer của bạn
4. Nếu báo lỗi Provisioning Profile → bấm  Fix Issue  để tự xử lý
5. Cắm iPhone qua USB → chọn thiết bị ở thanh trên cùng
6. Bấm  Cmd+R  để build và cài lên máy
7. Trên iPhone: Settings → General → VPN & Device Management → Trust chứng chỉ của bạn
```

| Lỗi iOS | Cách xử lý |
|---|---|
| "No signing certificate" | Thêm tài khoản Apple vào Xcode → Settings → Accounts |
| "Provisioning profile doesn't include device" | Bật **Automatically manage signing** trong Signing |
| App crash trên iPhone | Xcode → Window → Devices and Simulators → View Device Logs |
| Lỗi bitcode | Player Settings → Other Settings → tắt **Enable Bitcode** |

---

## Bước 14 — Build cho WebGL (trình duyệt)

WebGL chạy ngay trong trình duyệt — người chơi không cần cài gì.

### 14a. Player Settings

| Mục | Trường | Giá trị |
|---|---|---|
| Resolution and Presentation | Default Canvas Width | `1280` |
| Resolution and Presentation | Default Canvas Height | `720` |
| Publishing Settings | Compression Format | **Gzip** |
| Publishing Settings | Enable Exceptions | **Explicitly Thrown Exceptions Only** |

### 14b. Build
```
Build Settings → Platform: WebGL → Switch Platform → Build
→ chọn thư mục xuất (ví dụ Builds/WebGL/)
```
*Kết quả:* thư mục có `index.html` và các file phụ trợ.

### 14c. Chạy thử
⚠️ **Không mở `index.html` trực tiếp** — phải qua một web server:
```bash
cd Builds/WebGL
python3 -m http.server 8080
# Mở http://localhost:8080
```

### 14d. Lưu ý WebGL
- Server **phải bật CORS** cho tên miền chứa bản WebGL. Thêm vào `appsettings.json`:
  ```json
  "AllowedOrigins": ["https://yourgame.netlify.app", "http://localhost:8080"]
  ```
- SignalR tự dùng WebSocket — hoạt động tốt với WebGL.
- Trình duyệt di động có thể chậm — hãy test trên thiết bị mục tiêu.
- Tùy chọn hosting: Nginx/Apache, GitHub Pages, Netlify, Vercel.

---

# Phần D — Tra cứu

## Khắc phục sự cố

### Lỗi trong Unity Editor

| Vấn đề | Cách xử lý |
|---|---|
| Không thấy menu WorldFaith | Chờ biên dịch xong → **Assets → Refresh** (`Ctrl+R`) |
| Đỏ Console khi mới mở project | Bình thường — hết sau khi xong Bước 3, 4, 5 |
| `The type or namespace 'Microsoft.AspNetCore.SignalR' not found` | Thiếu DLL SignalR → làm lại **Bước 4** |
| `The type or namespace 'WorldFaith.Shared' not found` | Chưa chép Shared Library → làm lại **Bước 5** |
| `NullReferenceException` trên một Manager | Một `[SerializeField]` chưa được gán trong Inspector |
| Scene thiếu trong Build Settings | Kéo scene từ Project window vào Build Settings |
| Camera không phản hồi chuột/chạm | Chưa cài Input System → làm lại **Bước 3a** |

### Lỗi kết nối

| Vấn đề | Cách xử lý |
|---|---|
| `Failed to connect to server` khi Play | Server chưa chạy → `dotnet run` trước |
| `401 Unauthorized` | Token hết hạn → đăng xuất, đăng nhập lại |
| `CORS error` trong Console | Cấu hình CORS của server thiếu origin của bạn → sửa `appsettings.json` |
| Chạy được trên PC nhưng không trên điện thoại | Điện thoại phải dùng IP LAN (`192.168.x.x`), không phải `localhost` |
| WebGL: vừa kết nối là đóng | Kiểm tra lỗi WebSocket ở Console trình duyệt; đảm bảo server bật WebSocket |

### Lỗi build

| Vấn đề | Cách xử lý |
|---|---|
| Android: NDK not found | Unity Hub → Installs → ⚙️ trên bản 6.3 → Add Modules → Android Build Support |
| Android: crash ngay khi mở | `adb logcat -s Unity` để xem log |
| iOS: lỗi code signing | Xcode → Signing & Capabilities → chọn Team |
| WebGL: màn hình đen | Mở Developer Tools của trình duyệt → tab Console xem lỗi JavaScript |
| PC: antivirus chặn bản build | Thêm thư mục build vào danh sách loại trừ của antivirus |

---

## Checklist trước khi build

```
[ ] Unity 6.3 LTS đã cài kèm module cần thiết
[ ] Đã mở project, import lần đầu hoàn tất
[ ] Input System đã cài (Bước 3a)
[ ] TextMeshPro đã cài + Import TMP Essential Resources (Bước 3b)
[ ] Newtonsoft JSON đã cài (Bước 3c)
[ ] Đủ 6 DLL SignalR trong Assets/Plugins/SignalR/ (Bước 4)
[ ] Shared Library đã chép vào Assets/WorldFaith/Shared/ (Bước 5)
[ ] Đã tạo & lưu LoginScene, LobbyScene, GameScene trong Assets/Scenes/ (Bước 6)
[ ] Server URL đã đặt trong mọi scene (Bước 7)
[ ] Đủ 10 ảnh ô đất gán vào WorldRenderer (Bước 8a)
[ ] Main Camera là Orthographic, pan limit khớp kích thước bản đồ (Bước 8c)
[ ] Audio clip đã gán vào AudioManager (Bước 8b)
[ ] WorldFaith → Validate → Check All Managers → toàn xanh (Bước 9)
[ ] Đã test Play mode — đăng nhập & lobby chạy được (Bước 10)
[ ] Build Settings: 3 scene đúng thứ tự (Login=0, Lobby=1, Game=2)
[ ] Đã chọn nền tảng mục tiêu và Switch Platform
[ ] Player Settings đã cấu hình cho nền tảng đó
[ ] Build xong và test trên thiết bị mục tiêu
```

---

<p align="center">
  Cần trợ giúp? Mở issue tại <a href="https://github.com/thanhtinz/Game-new/issues">github.com/thanhtinz/Game-new/issues</a><br>
  ← Quay lại <a href="./SETUP.md">Cài đặt server & admin</a> · <a href="./README.md">Giới thiệu game</a>
</p>
