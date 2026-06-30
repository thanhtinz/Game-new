# WorldFaith

> **Game mô phỏng làm Thần (God Simulation) — sandbox chiến thuật nhiều người chơi.**
> Bạn không điều khiển thế giới. Bạn gieo *niềm tin*, và niềm tin mới là thứ điều khiển thế giới.

<p align="center">
  <em>"Người chơi không điều khiển thế giới. Họ tác động đến niềm tin, và niềm tin điều khiển thế giới."</em>
</p>

---

## Mục lục

1. [WorldFaith là game gì?](#1-worldfaith-là-game-gì)
2. [Bạn sẽ làm gì trong game?](#2-bạn-sẽ-làm-gì-trong-game)
3. [Vòng lặp chơi (Core Loop)](#3-vòng-lặp-chơi-core-loop)
4. [Các hệ thống nổi bật](#4-các-hệ-thống-nổi-bật)
5. [Một ván chơi diễn ra như thế nào](#5-một-ván-chơi-diễn-ra-như-thế-nào)
6. [Cách bạn thắng và thua](#6-cách-bạn-thắng-và-thua)
7. [Công nghệ & kiến trúc](#7-công-nghệ--kiến-trúc)
8. [Cấu trúc thư mục dự án](#8-cấu-trúc-thư-mục-dự-án)
9. [Bắt đầu nhanh](#9-bắt-đầu-nhanh)
10. [Tài liệu liên quan](#10-tài-liệu-liên-quan)

---

## 1. WorldFaith là game gì?

**WorldFaith** là một game mô phỏng làm Thần (god simulation) kết hợp chiến thuật sandbox, chơi nhiều người. Mỗi người chơi nhập vai **một vị Thần** đang cố gắng *tồn tại* trong ký ức của loài người thông qua niềm tin.

Điểm khác biệt lớn nhất: **bạn không trực tiếp điều khiển các nền văn minh.** Các vương quốc, thành bang, bộ lạc trong thế giới đều do **AI tự vận hành** — chúng tự lo dân số, kinh tế, chính trị, chiến tranh, tôn giáo, tội phạm, tai nạn, may rủi và các mối quan hệ xã hội.

Vai trò của bạn là **người gieo niềm tin**:
- Bạn gửi giấc mơ, điềm báo, phép màu, thiên tai để **tác động** đến con người.
- Con người **tự quyết định** cách phản ứng — tin, nghi ngờ, sợ hãi, cải đạo hay phản bội.
- Mỗi hành động thành công sinh ra **Niềm tin (Faith)** — nguồn năng lượng để bạn làm những điều lớn hơn.

> 💡 **Ý tưởng cốt lõi:** Đây là một *trình mô phỏng niềm tin xã hội*. Tôn giáo lan truyền qua vua chúa, quý tộc, nô bộc, hội nhóm, gia đình, tội phạm, anh hùng, tai nạn, phép màu và cả tin đồn.

---

## 2. Bạn sẽ làm gì trong game?

Bạn **không** phải là vua, tướng quân hay người xây thành. Bạn là **một vị Thần cạnh tranh để được nhớ đến**. Bạn quan sát con người sống, đau khổ, thờ phụng, hoài nghi, phản bội và tiến hóa — rồi bạn có thể giúp đỡ, thao túng, dọa nạt, ban phước, trừng phạt hoặc làm họ sa ngã.

| Bạn muốn… | WorldFaith đáp ứng bằng… |
|---|---|
| **Cảm giác làm Thần** | Phép màu, giấc mơ, điềm báo, thiên tai, cổng quỷ, tiến hóa thần thú. |
| **Chiều sâu chiến thuật** | Tiêu Niềm tin khôn ngoan, phản đòn Thần đối thủ, quản lý lòng tin và thứ bậc xã hội. |
| **Những câu chuyện** | Quan hệ NPC, vương quốc, hội nhóm, giáo phái tạo nên *lịch sử tự phát*. |
| **Sự tự do** | Bất kỳ chủng tộc nào cũng có thể thờ bất kỳ vị Thần nào — theo xác suất, không khóa cứng. |
| **Sự nguy hiểm** | Một vị Thần có thể *chết* nếu bị lãng quên, bị cấm thờ, hoặc bị Thần khác thay thế. |

---

## 3. Vòng lặp chơi (Core Loop)

```
   ┌─────────────────────────────────────────────────────────┐
   │  1. QUAN SÁT   → Tìm NPC, vùng đất, vương quốc dễ tác động │
   │  2. TÁC ĐỘNG   → Giấc mơ, điềm báo, ban phước, thiên tai   │
   │  3. AI PHẢN ỨNG → Tin / nghi ngờ / sợ / cải đạo / phản bội │
   │  4. THAY ĐỔI   → Niềm tin, Lòng tin, Nỗi sợ tăng/giảm      │
   │  5. CHI TIÊU   → Dùng Niềm tin cho can thiệp lớn hơn       │
   │  6. ĐỐI ĐẦU    → Thần đối thủ phản đòn, làm hỏng kế hoạch  │
   │  7. LỊCH SỬ    → Văn minh chiến tranh, hoàng kim, hoặc sụp │
   └───────────────────────────┬─────────────────────────────┘
                               │  (lặp lại)
                               ▼
```

Bạn **đẩy xác suất**, chứ không ép kết quả. Sức mạnh của một vị Thần nằm ở việc *gây ảnh hưởng* đúng người, đúng lúc — không phải ra lệnh.

---

## 4. Các hệ thống nổi bật

WorldFaith được xây dựng quanh nhiều hệ thống mô phỏng đan xen nhau:

| Hệ thống | Mô tả ngắn |
|---|---|
| 🙏 **Kinh tế Niềm tin** | Tín đồ × lòng thành × độ hợp chủng tộc × lòng tin → Niềm tin mỗi nhịp. |
| 👑 **8 nguyên mẫu Thần (Archetype)** | Mỗi vị Thần có khuynh hướng riêng (ánh sáng, tự nhiên, bóng tối…) ảnh hưởng đến chủng tộc theo. |
| 🧬 **7 cấp bậc Thần** | Từ *Forgotten* (bị lãng quên) đến *Ancient* (cổ đại) — càng cao, phép màu càng mạnh. |
| 🧝 **8 chủng tộc + ma trận tương hợp 8×8** | Elf yêu Thần tự nhiên, Quỷ ghét Thần ánh sáng… mọi cặp đều có hệ số riêng. |
| 🏰 **Mô phỏng vương quốc** | Kinh tế, quân sự, lương thực, ổn định, chính thể, nổi loạn — tất cả do AI vận hành. |
| ⛪ **Hệ thống tôn giáo & giáo lý** | 5 trục giáo lý (khoan dung↔trừng phạt, hòa hợp↔thống trị…) định hình bản sắc đạo. |
| ✨ **15 phép màu & phản đòn** | Phép màu có thể bị Thần khác hóa giải, làm trễ, hoặc bẻ ngược. |
| 🐉 **Tiến hóa & Anh hùng (Champion)** | Quái vật có thể tiến hóa thành thần thú; tín đồ có thể thành Thánh hoặc sa thành Quỷ vương. |
| 🎭 **AI Director** | "Đạo diễn" điều phối nhịp độ: chuyển kỷ nguyên, chống trì trệ, chống một Thần độc bá. |
| 🗺️ **Sinh thế giới kiểu WorldBox** | Bản đồ tạo theo thuật toán: lục địa, dãy núi, sông chảy xuôi dốc, bờ cát, làm mượt biome. |

> Muốn xem chi tiết đầy đủ về cơ chế, công thức cân bằng và thiết kế? Đọc **[Tài liệu thiết kế game (GDD)](./WorldFaith_GDD_v1.0.md)**.

---

## 5. Một ván chơi diễn ra như thế nào

Mỗi thế giới đi qua **5 kỷ nguyên (Ages)**, được AI Director tự động chuyển theo số nhịp (tick):

| Kỷ nguyên | Trọng tâm | Người chơi thường làm gì |
|---|---|---|
| 🌱 **Sơ khai (Early)** | Tín đồ đầu tiên | Giấc mơ, mưa, ban phước nhỏ, đền thờ đầu tiên |
| 🏰 **Vương quốc (Kingdom)** | Mở rộng chính trị | Tác động vua chúa, quý tộc, hội nhóm, xây đền |
| ⚔️ **Xung đột (Conflict)** | Thánh chiến | Châm ngòi/dập tắt chiến tranh, tranh giành thánh tích |
| 💀 **Sụp đổ (Collapse)** | Khủng hoảng | Cổng quỷ mở, văn minh yếu bắt đầu tan rã |
| 🔄 **Tái sinh (Rebirth)** | Hồi sinh | Văn minh mới mọc lên từ tàn tích cũ |

Mỗi ván tạo ra **một huyền thoại mới**: một giáo phái nô bộc chiếm lấy hoàng cung, một con quái trở thành thần thú, hay một vị Thánh sa ngã thành Quỷ vương.

---

## 6. Cách bạn thắng và thua

WorldFaith **không nhất thiết có một người thắng cuối cùng.** Trải nghiệm cốt lõi là **sự sống còn của vị Thần** qua những chu kỳ văn minh sinh — diệt — tái sinh.

**Bạn thua khi:**
- Vị Thần của bạn không còn tín đồ nào, **và** không còn thánh tích hay giáo phái ẩn để duy trì.
- Tên của bạn bị cấm thờ.
- Tôn giáo của bạn biến mất khỏi ký ức người sống.

**Thần bị lãng quên (Forgotten God):** Nếu tín đồ về 0 nhưng bạn vẫn còn ít nhất một thánh tích đang hoạt động hoặc một giáo phái ẩn, bạn **sống sót yếu ớt** ở trạng thái Forgotten thay vì bị xóa sổ. Hãy luôn giữ ít nhất một thánh tích trước khi mất người tín đồ cuối cùng.

---

## 7. Công nghệ & kiến trúc

WorldFaith gồm **bốn thành phần** hoạt động cùng nhau:

```
┌──────────────┐     SignalR (WebSocket)     ┌──────────────────┐
│ Unity Client │ ◄─────────────────────────► │   Game Server    │
│ (người chơi) │        REST + JWT           │  ASP.NET Core 8  │
└──────────────┘                             └────────┬─────────┘
                                                      │
┌──────────────┐     REST + JWT                       │
│ Admin Panel  │ ◄───────────────────────────────────┤
│ (Next.js)    │                                      │
└──────────────┘                             ┌────────┴─────────┐
                                             │ MongoDB + Redis  │
                                             └──────────────────┘
```

| Thành phần | Công nghệ |
|---|---|
| **Game Server** | ASP.NET Core 8 (C#), SignalR WebSocket |
| **Cơ sở dữ liệu** | MongoDB 7.0 (dữ liệu game) + Redis 7.2 (cache realtime) |
| **Client** | Unity 6.3 LTS (C#) |
| **Admin Panel** | Next.js 14, TypeScript, Tailwind CSS |
| **Xác thực** | JWT Bearer + Refresh Token Rotation |
| **Nhịp mô phỏng** | 500ms/tick (chỉnh được) |
| **Tối đa mỗi thế giới** | 8 vị Thần |
| **Bản đồ** | 128×128 ô (chỉnh được, có seed) |

---

## 8. Cấu trúc thư mục dự án

```
Game-new/
├── README.md                 ← Bạn đang ở đây (giới thiệu game)
├── SETUP.md                  ← Hướng dẫn cài đặt & chạy server/admin (cho người mới)
├── UNITY_BUILD_GUIDE.md      ← Hướng dẫn build client Unity từng bước (cho người mới)
├── WorldFaith_GDD_v1.0.md    ← Tài liệu thiết kế game đầy đủ (cơ chế, công thức)
├── ASSETS.md                 ← Danh sách ~204 file tài nguyên (ảnh, âm thanh, font)
│
├── server/                   ← Game Server (ASP.NET Core 8)
│   └── WorldFaith.Server/
├── client-unity/             ← Client Unity (người chơi)
│   └── Assets/WorldFaith/
├── admin-panel/              ← Bảng quản trị (Next.js)
├── shared/                   ← Thư viện dùng chung (enum, model, contract)
│   └── WorldFaith.Shared/
├── tests/                    ← Unit test (xUnit)
│   └── WorldFaith.Tests/
│
├── docker-compose.yml        ← Chạy DB + server bằng Docker
├── Dockerfile
└── WorldFaith.sln            ← Solution Visual Studio
```

---

## 9. Bắt đầu nhanh

Bạn mới làm quen với dự án? Hãy đi theo đúng thứ tự này:

1. **Cài đặt & chạy server + admin panel** → làm theo **[SETUP.md](./SETUP.md)**.
   Đây là bước bắt buộc đầu tiên: cài công cụ, dựng database, chạy server, mở Admin Panel.

2. **Build & chạy client Unity** → làm theo **[UNITY_BUILD_GUIDE.md](./UNITY_BUILD_GUIDE.md)**.
   Hướng dẫn từng bước để mở project Unity, cài package, tạo scene và build cho PC / Android / iOS / WebGL.

3. **Tìm hiểu sâu cơ chế game** → đọc **[WorldFaith_GDD_v1.0.md](./WorldFaith_GDD_v1.0.md)**.

Nếu chỉ muốn xem nhanh, server chạy bằng Docker chỉ với một lệnh (xem chi tiết trong SETUP.md):

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
docker-compose up -d        # khởi động MongoDB + Redis + Server
```

> ⚠️ Tài khoản admin mặc định và toàn bộ bước cấu hình nằm trong **[SETUP.md](./SETUP.md)**. Đừng dùng mật khẩu mặc định khi triển khai thật.

---

## 10. Tài liệu liên quan

| Tài liệu | Nội dung | Dành cho |
|---|---|---|
| **[SETUP.md](./SETUP.md)** | Cài đặt, cấu hình, chạy server & admin panel, triển khai production | Người mới bắt đầu |
| **[UNITY_BUILD_GUIDE.md](./UNITY_BUILD_GUIDE.md)** | Mở project Unity, cài package, tạo scene, build mọi nền tảng | Người mới bắt đầu |
| **[WorldFaith_GDD_v1.0.md](./WorldFaith_GDD_v1.0.md)** | Thiết kế game đầy đủ: hệ thống, công thức cân bằng, lộ trình | Người thiết kế / lập trình |
| **[ASSETS.md](./ASSETS.md)** | Danh sách ~204 file tài nguyên cần chuẩn bị | Họa sĩ / âm thanh |

---

<p align="center">
  <strong>WorldFaith</strong> · "Người chơi không điều khiển thế giới. Họ tác động đến niềm tin, và niềm tin điều khiển thế giới."<br>
  Cần trợ giúp? Mở issue tại <a href="https://github.com/thanhtinz/Game-new/issues">github.com/thanhtinz/Game-new/issues</a>
</p>
