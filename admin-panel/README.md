# WorldFaith Admin Panel

Next.js 14 dashboard để quản lý WorldFaith server.

## Setup

```bash
cd admin-panel
npm install
npm run dev  # chạy tại http://localhost:3001
```

## Biến môi trường

Tạo file `.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Pages

| Route | Chức năng |
|-------|-----------|
| `/login` | Đăng nhập admin |
| `/dashboard` | Server stats realtime (auto refresh 5s) |
| `/worlds` | Quản lý worlds - snapshot, force end, force rebirth |
| `/players` | Quản lý players - search, ban/unban |
| `/config` | Balance config - chỉnh game params runtime |

## Balance Config

Tất cả game numbers đều tunable runtime không cần redeploy:
- **faith**: tick interval, gen rate, max faith
- **miracle**: cost cho 15 miracle types, counter window
- **religion**: spread chance, devotion decay, schism/heresy/crusade thresholds
- **evolution**: points cần per stage, force evolve cost
- **civ**: population growth rates, state thresholds
- **world**: rebirth interval, spawn counts
