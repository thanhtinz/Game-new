# WorldFaith — Setup & Operation Guide

> A god simulation sandbox MMO. You play as a god: recruit followers, perform miracles, found religions, evolve creatures.  
> *"Players do not control the world. They influence belief, and belief controls the world."*

---

## Table of Contents

1. [Requirements](#1-requirements)
2. [Clone the Repository](#2-clone-the-repository)
3. [Database Setup](#3-database-setup)
4. [Server Configuration](#4-server-configuration)
5. [Run the Server](#5-run-the-server)
6. [Admin Panel](#6-admin-panel)
7. [Unity Client Setup](#7-unity-client-setup)
8. [Build the Unity Client](#8-build-the-unity-client)
9. [Asset Preparation](#9-asset-preparation)
10. [Verify Everything Works](#10-verify-everything-works)
11. [Production Deployment](#11-production-deployment)
12. [Default Credentials](#12-default-credentials)
13. [Admin Panel Guide](#13-admin-panel-guide)
14. [Game Mechanics Guide](#14-game-mechanics-guide)
15. [Technical Reference](#15-technical-reference)
16. [FAQ](#16-faq)

---

## 1. Requirements

Install these four tools before anything else:

### .NET SDK 8
```bash
# Download from https://dotnet.microsoft.com/download → select .NET 8.0
dotnet --version   # must show 8.x.x
```

### Docker Desktop
```bash
# Download from https://www.docker.com/products/docker-desktop
# Open Docker Desktop and wait for the whale icon to stop animating
docker --version   # must show 24.x.x or higher
```

> Docker Desktop must be running before any database steps.

### Node.js 20 LTS
```bash
# Download from https://nodejs.org → choose LTS (left button)
node --version     # must show v20.x.x or higher
```

### Unity 6.3 LTS
```
1. Download Unity Hub: https://unity.com/download
2. Open Unity Hub → sign in → Installs tab → Install Editor
3. Select Unity 6.3 LTS
4. Check Android Build Support module if you want Android builds
5. Click Install — takes 15-30 minutes
```

---

## 2. Clone the Repository

```bash
git clone https://github.com/thanhtinz/Game-new.git
cd Game-new
```

---

## 3. Database Setup

WorldFaith uses MongoDB for game data and Redis for realtime caching. Docker handles both.

```bash
# Make sure Docker Desktop is open, then:
docker-compose up worldfaith-mongo worldfaith-redis -d
```

First run downloads images (~3-5 min). Verify both are up:
```bash
docker ps
# Expected output:
# worldfaith-mongo    Up X minutes
# worldfaith-redis    Up X minutes
```

**Daily database management:**
```bash
docker-compose stop worldfaith-mongo worldfaith-redis   # stop
docker-compose start worldfaith-mongo worldfaith-redis  # start again
docker logs worldfaith-mongo --tail 20                  # view logs if issues

# Backup
docker exec worldfaith-mongo mongodump --db worldfaith --archive=/tmp/bk.gz --gzip
docker cp worldfaith-mongo:/tmp/bk.gz ./backup-$(date +%Y%m%d).gz

# Restore
docker cp my-backup.gz worldfaith-mongo:/tmp/restore.gz
docker exec worldfaith-mongo mongorestore --archive=/tmp/restore.gz --gzip
```

**Optional GUI:** MongoDB Compass at https://www.mongodb.com/products/compass → connect to `mongodb://localhost:27017`

---

## 4. Server Configuration

Edit `server/WorldFaith.Server/appsettings.json`:

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

| Field | Notes |
|---|---|
| `MongoDB` / `Redis` | Leave as-is for local Docker setup |
| `Jwt.Secret` | **Change before deploying** — use any random 32+ character string |
| `Admin.Password` | **Change before deploying** |

---

## 5. Run the Server

```bash
cd Game-new/server/WorldFaith.Server
dotnet run
```

First compile takes 30-60 seconds. You'll see:
```
[INF] WorldFaith Server starting up
[INF] Balance config seeded (90 params)
[INF] Admin account seeded: admin@worldfaith.game
[INF] Now listening on: http://localhost:5000
```

**Verify:** Open `http://localhost:5000/health` in a browser → should show `{"status":"ok"}`

**Stop:** Press `Ctrl + C`

**Run all services at once with Docker:**
```bash
docker-compose up -d        # start DB + server in background
docker-compose logs -f      # follow logs
docker-compose down         # stop everything
```

---

## 6. Admin Panel

### Install
```bash
cd Game-new/admin-panel
npm install
```

Create `admin-panel/.env.local`:
```bash
# Mac/Linux
echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > .env.local

# Windows — create the file manually with Notepad and save as .env.local
```

### Run
```bash
cd Game-new/admin-panel
npm run dev
```

Open **http://localhost:3001** → sign in with `admin@worldfaith.game` / `Admin@WorldFaith2024!`

---

## 7. Unity Client Setup

### Step 1 — Open the project
```
1. Open Unity Hub
2. Click Open → Add project from disk
3. Select the folder: Game-new/client-unity/
4. Choose Unity 6.3 LTS when prompted
5. Wait for initial import (5-10 minutes — red errors at first are normal)
```

### Step 2 — Install required packages

Go to **Window → Package Manager → Unity Registry** and install:

**TextMeshPro**
1. Search for "TextMeshPro" → Install
2. After install: **Window → TextMeshPro → Import TMP Essential Resources** → Import All

**Newtonsoft JSON**
1. Click `+` (top left) → **Add package by name**
2. Type: `com.unity.nuget.newtonsoft-json` → Add

**Mobile Notifications** (for Android/iOS push)
1. Click `+` → **Add package by name**
2. Type: `com.unity.mobile.notifications` → Add

### Step 3 — Install SignalR DLLs

SignalR handles realtime connection to the server.

**Mac / Linux:**
```bash
cd Game-new
DST="client-unity/Assets/Plugins/SignalR"
mkdir -p "$DST"
cd /tmp && mkdir signalr_tmp && cd signalr_tmp
dotnet new console -n sr -o sr
cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release -o pub
ROOT=$(cd - && pwd)
cp pub/Microsoft.AspNetCore.SignalR.*.dll       "$ROOT/$DST/"
cp pub/Microsoft.AspNetCore.Http.Connections*.dll "$ROOT/$DST/"
cd /tmp && rm -rf signalr_tmp
echo "SignalR installed"
```

**Windows:**
1. Go to: `https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/8.0.0`
2. Click **Download package** → save the `.nupkg` file
3. Rename `.nupkg` → `.zip` → extract it
4. Navigate inside to `lib/netstandard2.1/`
5. Copy all `.dll` files → paste into `Game-new/client-unity/Assets/Plugins/SignalR/` (create the folder if needed)

Wait for Unity to finish importing (progress bar in the bottom-right).

### Step 4 — Link the Shared Library

The shared library contains enums and contracts used by both server and Unity client.

**Mac / Linux:**
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

Wait for Unity to import the new files (bottom-right progress bar).

### Step 5 — Create the three scenes

Each scene must be saved in `Assets/Scenes/` and set up with the WorldFaith tools:

**LoginScene:**
```
File → New Scene → Basic (Built-in) → Save As "LoginScene" in Assets/Scenes/
Menu: WorldFaith → Setup → Create Login Scene Objects
```

**LobbyScene:**
```
File → New Scene → Basic (Built-in) → Save As "LobbyScene" in Assets/Scenes/
Menu: WorldFaith → Setup → Create Lobby Scene Objects
```

**GameScene:**
```
File → New Scene → Basic (Built-in) → Save As "GameScene" in Assets/Scenes/
Menu: WorldFaith → Setup → Create Game Scene Objects
```

> The **WorldFaith** menu appears in Unity's top menu bar after the Shared Library finishes importing. If it doesn't appear, press **Assets → Refresh** (`Ctrl+R` / `Cmd+R`).

### Step 6 — Configure server URLs

In each scene, find these GameObjects in the Hierarchy and set their `Server Url` field in the Inspector:

| GameObject | Field | Value (local) |
|---|---|---|
| `WorldFaithClient` | Server Url | `http://localhost:5000/hubs/world` |
| `LobbyClient` | Server Url | `http://localhost:5000/hubs/lobby` |
| `ChatClient` | Server Url | `http://localhost:5000/hubs/chat` |
| `AuthManager` | Server Url | `http://localhost:5000` |

These fields appear under the `[Header("Server Config")]` section in the Inspector.

### Step 7 — Validate the setup
```
Menu: WorldFaith → Validate → Check All Managers
```

All entries must show a green checkmark. If anything shows a warning, check the Console window for the specific error.

---

## 8. Build the Unity Client

### Build Settings

Go to **File → Build Settings** and configure:

1. Drag your three scenes into **Scenes In Build** in this exact order:
   ```
   0  Assets/Scenes/LoginScene
   1  Assets/Scenes/LobbyScene
   2  Assets/Scenes/GameScene
   ```
2. Select your target platform
3. Click **Build** (or **Build And Run** to install directly)

### PC (Windows / Mac / Linux)

```
Platform: PC, Mac & Linux Standalone
Target Platform: Windows (or Mac / Linux)
Architecture: x86_64
Click Build → choose output folder → wait
```

The output folder will contain the `.exe` (Windows) or `.app` (Mac) file you can distribute.

### Android

**One-time setup on your phone:**
```
Settings → About Phone → tap "Build Number" 7 times to unlock Developer Mode
Settings → Developer Options → enable "USB Debugging"
Connect phone to computer via USB cable
Allow USB debugging when prompted on phone
```

**Build in Unity:**
```
Platform: Android
Minimum API Level: Android 8.0 (API 26) — in Player Settings
Package Name: com.yourname.worldfaith — in Player Settings → Other Settings
```

```
Build And Run → select output folder → Unity installs directly to phone
```

Or choose **Build** only to get an `.apk` file you can sideload later.

**For Google Play submission:** Use **Build App Bundle (.aab)** instead of APK.

### iOS (Mac only)

```
Platform: iOS
Bundle Identifier: com.yourname.worldfaith — in Player Settings → Other Settings
Signing Team: your Apple Developer Team ID
```

```
Build → Unity creates an Xcode project folder
Open the .xcodeproj in Xcode
Select your device or simulator
Product → Run (⌘R)
```

For App Store submission: in Xcode, **Product → Archive → Distribute App**.

### WebGL (browser)

```
Platform: WebGL
Compression Format: Gzip (in Player Settings → Publishing Settings)
```

```
Build → generates a folder with index.html
Host the folder on any static file server (Nginx, Apache, GitHub Pages, Netlify)
```

> WebGL does not support SignalR TCP by default. Ensure your server supports WebSocket fallback (it does by default with ASP.NET Core SignalR).

### Common Build Issues

| Error | Fix |
|---|---|
| `HubConnection not found` | SignalR DLLs not in `Assets/Plugins/SignalR/`. Redo Step 3. |
| `WorldFaith namespace not found` | Shared Library not linked. Redo Step 4. |
| Missing scenes in build | Add scenes to Build Settings in order (Step 8). |
| Android `INSTALL_FAILED_VERSION_DOWNGRADE` | Uninstall old version from phone first. |
| iOS code signing error | Set your Apple Developer Team in Player Settings → Other Settings. |
| WebGL `Content Security Policy` error | Server must have CORS enabled for your WebGL domain. |
| `Failed to connect to server` | Wrong URL in Inspector. Check Step 6. For Android/iOS use your LAN IP, not `localhost`. |

---

## 9. Asset Preparation

See the full list at **[ASSETS.md](./ASSETS.md)** (~204 files).

### Required — game won't render without these

**10 Tile Textures** — save to `Assets/WorldFaith/World/Tiles/` (64×64 px PNG):
```
tile_grassland.png   tile_forest.png   tile_mountain.png   tile_desert.png
tile_tundra.png      tile_water.png    tile_volcano.png    tile_sacred.png
tile_beach.png        tile_river.png
```

Suggested colors: Grassland `#4a9c2f` · Forest `#1a5c1a` · Mountain `#7a7a7a` · Desert `#c8b44a` · Tundra `#b0c8e0` · Water `#2a64c8` · Volcano `#c83210` · Sacred `#c8a832` · Beach `#e6d7a0` · River `#468cdc`

**47 Sound Effects** — save to `Assets/WorldFaith/Audio/SFX/`  
After copying files, assign them in the Inspector: find `AudioManager` in your GameScene → expand `Sfx Clips[]` → assign each file in `SfxId` enum order.

**5 Music tracks** — save to `Assets/WorldFaith/Audio/Music/`:
```
music_base.mp3       → AudioManager → Music Base
music_religion.mp3   → AudioManager → Music Religion
music_war.mp3        → AudioManager → Music War
music_apocalypse.mp3 → AudioManager → Music Apocalypse
music_victory.mp3    → AudioManager → Music Victory
```

### Recommended
- **28 VFX Prefabs** → `Assets/WorldFaith/VFX/Prefabs/` → assign to `VfxManager → Catalog[]`
- **8 Archetype Icons** (128×128 px) → `Assets/WorldFaith/UI/Sprites/`
- **15 Miracle Icons** (64×64 px) → `Assets/WorldFaith/UI/Sprites/`
- **Fonts** (Cinzel, Nunito, Rajdhani) — download from fonts.google.com → copy `.ttf` files → **Window → TextMeshPro → Font Asset Creator** → generate and save

### Free sources
| Type | Source |
|---|---|
| SFX | freesound.org · kenney.nl · zapsplat.com |
| Music | incompetech.com · freemusicarchive.org |
| Sprites | kenney.nl · game-icons.net |
| Fonts | fonts.google.com |

---

## 10. Verify Everything Works

Run these checks in order:

**① Database running:**
```bash
docker ps   # both worldfaith-mongo and worldfaith-redis must show "Up"
```

**② Server responding:**
```bash
curl http://localhost:5000/health   # must return {"status":"ok"}
```

**③ Admin Panel:**
```
Open http://localhost:3001 → sign in → Dashboard must load with server stats
```

**④ Unity client:**
```
1. Press Play (▶) in the Unity Editor
2. Login screen appears → register a new account → sign in
3. Lobby screen appears → Create Room → Start game
4. Check Admin Panel → Dashboard → Active Worlds should show 1
```

**⑤ Multiplayer (two players):**
```
Run two Unity Editor instances (or build and run alongside Editor)
Both players join the same room → verify both see each other on the lobby list
Start game → verify both clients receive world ticks in Console
```

---

## 11. Production Deployment

### Server requirements

| Players | CPU | RAM | Bandwidth |
|---|---|---|---|
| 2-10 | 2 cores | 4 GB | 10 Mbps |
| 10-30 | 4 cores | 8 GB | 20 Mbps |
| 30+ | 8 cores | 16 GB | 50 Mbps |

Recommended OS: Ubuntu 22.04 LTS

### Step 1 — Install Docker on VPS
```bash
ssh user@YOUR_VPS_IP
sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER
exit && ssh user@YOUR_VPS_IP   # re-login to apply group
docker --version
```

### Step 2 — Upload and configure
```bash
scp -r Game-new/ user@YOUR_VPS_IP:/opt/worldfaith/
ssh user@YOUR_VPS_IP
cd /opt/worldfaith

cat > .env << 'EOF'
JWT_SECRET=Replace_This_With_A_Random_32_Char_String_Now
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=StrongPassword@2024!
EOF

docker-compose up -d
docker-compose logs -f   # wait for "Now listening on"
curl http://localhost:5000/health
```

### Step 3 — Nginx reverse proxy
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

### Step 4 — HTTPS (free with Let's Encrypt)
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
# Follow prompts, select "redirect HTTP to HTTPS"
```

### Step 5 — Update Unity server URLs

In each scene (LoginScene, LobbyScene, GameScene), find the four network GameObjects and change `Server Url`:

| Before (local) | After (production) |
|---|---|
| `http://localhost:5000/hubs/world` | `https://api.yourdomain.com/hubs/world` |
| `http://localhost:5000/hubs/lobby` | `https://api.yourdomain.com/hubs/lobby` |
| `http://localhost:5000/hubs/chat`  | `https://api.yourdomain.com/hubs/chat` |
| `http://localhost:5000`            | `https://api.yourdomain.com` |

Then rebuild the client for your target platform.

---

## 12. Default Credentials

| | |
|---|---|
| **Email** | `admin@worldfaith.game` |
| **Password** | `Admin@WorldFaith2024!` |
| **Role** | Admin (full Admin Panel access) |

> Change the password in `appsettings.json → Admin.Password` before deploying to production.

---

## 13. Admin Panel Guide

### Dashboard
Real-time server overview (auto-refreshes every 5 seconds):
- **Server status** — green pulse = online, red = down
- **8 stat cards** — Active Worlds, Gods, Online Players, Civs, Entities, Religions, NPCs, Organizations
- **Active Worlds list** — shows each world's tick, cycle, and god count

### Events Log
Live feed of all in-game events (refreshes every 3 seconds). Use the filter tabs to show only Crime / Accidents / Social / Political / Miracle / Evolution events. Toggle Auto Refresh off to pause and read carefully.

### Worlds
- **Force End** — ends the world immediately, calculates final scores
- **Force Rebirth** — resets world to tick 0, gods keep their current rank

### Maps & Tiles
Visual map editor (128×128 grid by default). Click any tile to edit its biome, fertility level, and whether it has a temple. Use **Place Sacred** to turn a tile into a Sacred Site (increases evolution points for entities nearby).

**Regen Map** rebuilds the entire map using the WorldBox-style terrain generator — organic continent shapes, ridge-line mountain chains, rivers carved downhill from peaks to the sea, sandy coastlines, and a biome-smoothing pass that removes single-tile noise specks. You can optionally enter a **seed** in the regen dialog; the same seed and map size always reproduce identical terrain, useful for sharing or debugging a specific world layout. Leave it blank for a random seed (requires confirmation either way — this destroys all current tiles, civs, and structures on that world).

Tile color guide: Grassland=green · Forest=dark green · Mountain=gray · Desert=yellow · Tundra=light blue · Water=blue · Volcano=red · Sacred=gold · Beach=sand · River=bright blue

### Dungeons
Manage dungeons where Adventure Guild runs missions.

**States:** Active (open), Infested (dangerous after 200 ticks), Sealed (admin locked), Cleared (mission complete)

**Spawn a dungeon:**
1. Click **+ Spawn Dungeon**
2. Choose type:
   - `AncientRuins` — safest, good for new guilds to practice
   - `LostTemple` — high relic chance, often linked to forgotten gods
   - `MonstersLair` — high danger, high reward
   - `ForbiddenSanctum` — most dangerous, strongest relics
   - `DarkPortal` — continuously spawns monsters if not sealed quickly
3. Enter X, Y tile coordinates (0-63)
4. Optionally enter a God ID to link a relic to that god (40% relic spawn chance)

Use **Seal** to lock a DarkPortal. Use **Clear** to mark a dungeon as completed.

### Relics
Divine artifacts that generate passive faith for their origin god.

**Key mechanics:**
- Each active relic generates 2-12 faith per 10 ticks for its origin god
- A god with 0 followers but active relics becomes **Forgotten** (survives weakly) instead of eliminated
- A god with 0 followers AND no relics/cults is **permanently eliminated**
- A civ holding a relic whose ruling religion matches the origin god gets **+50% faith bonus**
- Abandoned relics (no owner, no civ, not in dungeon) decay over time

**Transfer a relic:**
1. Click any relic to open the transfer modal
2. Enter NPC ID to give it to a specific NPC, or Civ ID to give it to a civilization
3. Leave both blank = relic goes abandoned and starts decaying

Use **Destroy** to permanently remove a relic (warning: the linked Forgotten god may be eliminated).

### God Note
Lists notable followers organized into 8 tabs based on their spiritual profile.

**Tabs:** Top Faithful · Rising Talents · Potential Priests · Saint Candidates · Prophet Candidates · Champions · Dangerous Followers · Hidden Assets

Each card shows the NPC's name, tier, faith level, talents, achievements, potential label, and risk assessment. Click any card to open the **Divine Action modal** with 9 actions:

| Action | Effect |
|---|---|
| Bless | +10% devotion, -10 corruption risk, 20% chance awakens a talent |
| Send Dream | +dreams received, +trust (Dream Sensitive NPCs get double effect) |
| Test | 70% → earn achievement + integrity gain; 30% → moderate doctrine violation |
| Promote | Advance church rank if conditions are met |
| Mark as Chosen | +30 Destiny Modifier, +20 reputation, attracts rival gods |
| Protect | -15 corruption risk, reduces assassination/kidnap vulnerability |
| Ignore | No action (saves faith) |
| Punish | Slight corruption reduction, slight loyalty decrease |
| Corrupt | (Dark Gods) Triggers severe doctrine violation, pushes NPC to dark path |

### Gods
View and edit each active god. Click to open the edit modal — adjust Faith, Trust, Fear, FollowerCount directly. Unlock specific miracles from the list. **Eliminate** removes the god from the game (irreversible).

When to use:
- God has negative Faith due to a bug → set Faith to 100
- Testing the counter-miracle system → unlock all miracles at once
- Balancing: one god is too dominant → reduce their Faith

### NPCs
Manage NPCs by tier. Use the tier filter to narrow down results.

Click any NPC to edit stats (Loyalty, Ambition, Piety, GodTrustLevel). Use **Exile** to remove them from the kingdom or **Kill** to end their life.

Scenarios you can create:
- **Betrayal:** set Noble Ambition = 90, Loyalty = 20 → Noble will betray the court soon
- **Force Champion:** find an Adventurer, set GodTrustLevel = 75, EvolutionPoints = 160 → next champion check promotes them
- **Test heresy:** set a Priest's Loyalty = 10 → they become a heresy risk

### Mobs / Entities
View and manage evolved creatures. The **Evolve** dropdown instantly moves an entity to the selected stage without waiting for EXP. **Spawn** creates a new entity at specific coordinates.

Use cases:
- Testing Apex entity world events → spawn an ApocalypticEntity at the map center
- Testing the Champion path → evolve a HumanHero to Saint directly

### Civilizations
Each civ shows Race, Government type, State, Economy/Military/Food/Stability bars, Population, and War status.

**Quick boost buttons** (in the table, no modal needed):
- `+E` → Economy +30
- `+M` → Military +30
- `+F` → Food +30
- `✕` → Collapse the civ immediately

Click a civ to open the full edit modal with all 8 stats plus Government dropdown, Personality, State, and War toggle.

### Religions
Manage religions including their Doctrine Axes.

Click a religion and switch to the **Doctrine Axes** tab to adjust all 5 sliders:

| Axis | Low end (-100) | High end (+100) |
|---|---|---|
| Mercy / Punishment | Forgive everything | Execute heretics |
| Isolation / Expansion | Protect existing flock | Aggressive missionary spread |
| Harmony / Dominion | Nature harmony (Elves love this) | Conquest (Orcs prefer this) |
| Freedom / Order | Individual liberty | Strict hierarchy (Nobles/Royals prefer this) |
| Sacrifice / Prosperity | Suffering has meaning | Prosperity proves faith |

Doctrine axes shift automatically from events. Admin can override manually. Use **Schism** to instantly split the religion (1/3 followers split off). Use **Delete** to remove it entirely.

### Organizations
Manage the six organization types. For **Underground** orgs, watch the **Heat Level** — the higher it is, the more likely they get exposed. Use **Expose** to force discovery. Use **Disband** to remove the organization.

To detect a **Court Deadlock**: check if Royal Court members have different `GodInfluenceId` values — if so, a deadlock is occurring and the kingdom is weakening.

### Players
Search by email or username (real-time). Click any player to:
- **Ban** (requires a reason) / **Unban**
- **Reset Password** (enter new password directly)
- **Promote to Admin** / **Remove Admin**

### Balance Config
Tune 90 game parameters without restarting the server.

1. Use the category tabs to filter, or type in the search box
2. Click any value field and edit it
3. Press **Enter** or click **Save** — the field turns green to confirm
4. Changes take effect within 60 seconds (cache TTL)
5. **Reset Default** restores everything to initial values

**Most important parameters:**

| Parameter | Description | Default |
|---|---|---|
| `faith.tick_interval` | Simulation tick speed in ms | 500 |
| `civ.famine_threshold` | Food level that triggers famine | 10 |
| `npc.champion_trust_required` | Trust needed for Adventurer → Champion | 70 |
| `rank.awakened_threshold` | Cumulative faith to reach Awakened rank | 5000 |
| `dungeon.relic_drop_chance` | Probability a dungeon contains a relic | 0.4 |
| `director.stagnation_disaster_chance` | Chance AI Director injects a crisis | 0.15 |

---

## 14. Game Mechanics Guide

### Faith Generation Formula

```
Faith/tick = (Followers × Devotion × RaceAffinity × Trust × Institution × Event)
           × ArchetypeBonus × GodRankMultiplier
```

- **Followers** — NPC tier determines faith contribution: Royalty 0.50/tick, Noble 0.15, Adventurer 0.05, Servant 0.02, Commoner 0.01
- **Devotion (BelieverType)** — Casual 0.5× · Devout 1.0× · Fanatic 2.0× · Cultist 1.5× · Heretic 0.3×
- **RaceAffinity** — race × archetype compatibility: Elf + Nature god = 1.6×; Demon + Light god = 0.2×
- **GodRankMultiplier** — Nascent 1.0× → Ancient 3.0×

**To increase faith quickly:**
1. Convert high-tier NPCs (one Royalty = 50 Commoners in faith output)
2. Build temples (+0.5 faith/tick each, regardless of follower count)
3. Keep Trust high — successful miracles raise trust → higher faith multiplier
4. Target races that match your archetype

### God Rank System

| Rank | Cumulative Faith | Power Multiplier | Unlocked Miracles |
|---|---|---|---|
| Forgotten | 0 followers | 0.1× | Survive via relics/cults |
| Nascent | 0 | 1.0× | Dream, Rain, BlessHarvest |
| Awakened | 5,000 | 1.2× | +Omen, HealFollower, Storm |
| Established | 25,000 | 1.5× | +Curse, DivineVoice, Earthquake, Portal |
| Revered | 100,000 | 1.8× | +Volcano, Revelation, DemonInvasion |
| Exalted | 400,000 | 2.2× | +DivineBeastCreation, HolyWar |
| Ancient | 1,000,000 | 3.0× | Full power |

**Forgotten God survival:** If your followers hit 0, you survive as long as you have at least one active relic or hidden cult. Without both, you are permanently eliminated. Always maintain at least one relic before losing your last followers.

### Doctrine Integrity (v1.2)

NPCs with divine power must live according to their god's doctrine. Their **Doctrine Integrity** score determines their power multiplier:

| Score | Status | Power Modifier |
|---|---|---|
| 90-100 | Exalted | ×1.30 |
| 70-89 | Faithful | ×1.05 |
| 50-69 | Shaken | ×0.83 |
| 25-49 | Compromised | ×0.55 |
| 0-24 | Broken → **Fall Event** | ×0.15 |

**Violation severity:**
- Minor contradiction → -2 to -5 (priest speaks in anger)
- Moderate violation → -8 to -15 (purity follower gives in to temptation)
- Major violation → -20 to -35 (saintess abandons innocents)
- Severe betrayal → -40 to -70 (chosen one secretly serves rival god)
- Doctrine inversion → -80 to -100 → **NPC falls** (Saintess becomes BloodSaint)

**Redemption:** NPC can restore integrity through pilgrimage/trial progress (0-100). Admin can trigger this or it progresses automatically.

### Escort System (v1.2)

Important religious figures attract escorts based on their rank:

| Church Rank | Escort Size | Primary Roles |
|---|---|---|
| Priest | 1-3 | Guard Knights |
| High Priest | 3-8 | Knights + Scribes + Disciples |
| Prophet | 5-20 | Knights + Pilgrims + Disciples |
| Saint / Saintess | 8-30 | Knights + Healers + Disciples + Fanatics |
| Divine Avatar | 20-50 | Elite full set |

- 3% chance an escort member is secretly corrupted by a rival god
- Fanatics will sacrifice themselves to protect a saint
- Escorts defend against kidnap attempts (Escort Strength vs Org Power)
- Successful protection → god faith +20, trust +5; kidnap success → god faith -50, trust -15

### Conversion Formula

```
ConversionChance = Openness × RaceAffinity × SocialPressure × TrustDiff × RecentEvents × DoctrineMatch
```

- **Openness by tier:** Commoner 0.8 → Royalty 0.15
- **Government spread bonus:** Theocracy 1.4× · Monarchy 1.2× · MerchantState 0.8×
- **Ruling religion bonus:** +50% social pressure if your religion rules the civ

To convert a difficult race (e.g., Demon following Light god, RaceAffinity 0.2×):
1. Send Dream repeatedly to raise GodTrustLevel slowly
2. Wait for a disaster (Crop Failure, Disease) — devotion surges create a conversion window
3. Perform a successful miracle right before the NPC faces hardship

### AI Director

The AI Director (every 20 ticks) controls world pacing:

**Age transitions (automatic):**
- Tick 100 → Kingdom Age — AncientRuins dungeons spawn near civs
- Tick 300 → Conflict Age — ForbiddenSanctum appears, holy wars possible
- Tick 600 → Collapse Age — DarkPortals open, weakest civs start collapsing
- Tick 850 → Rebirth Age — new civs grow from the ruins

**Anti-stagnation** (every 80 ticks): if no wars exist → 15% chance a natural disaster is injected into the weakest civ.

**Anti-snowball** (every 150 ticks): if one god holds >60% of all followers → weaker gods receive a faith boost of +50.

### World Generation

Each new world's terrain is built by a WorldBox-style procedural generator (not simple random noise) in nine stages:

1. **Continent shaping** — 2-4 organic landmass "blobs" are placed and blended together with domain-warped noise, producing natural bays and peninsulas instead of a perfect circle
2. **Elevation** — layered Perlin noise combined with the continent shape
3. **Ridge mountains** — a separate ridge-noise pass carves connected mountain *ranges* rather than scattered isolated peaks
4. **Moisture & temperature** — independent noise maps, with temperature falling toward the poles and additionally cooled by elevation
5. **Biome classification** — elevation/moisture/temperature/ridge values are combined to assign each tile a type
6. **Rivers** — starting at high-elevation mountain tiles, each river flows downhill (steepest-descent) until it reaches the sea or stalls
7. **Coastlines** — any land tile touching open water becomes a Beach tile
8. **Biome smoothing** — a majority-filter pass removes single-tile noise specks so regions read as cohesive Forest/Desert/Grassland blocks instead of static
9. **Sacred sites & spawning** — 5-8 Sacred tiles are placed, civilizations settle on fertile land (preferring spots near rivers and coastlines), and starting wildlife/monsters/heroes are seeded by biome

**Seeds:** every world has a seed (visible in Admin → Worlds and Admin → Maps). Leaving the seed blank when creating a room or regenerating a map picks a random one; entering the same seed with the same map size always reproduces identical terrain — useful for sharing a specific layout or debugging.

---

## 15. Technical Reference

| | |
|---|---|
| **Server** | ASP.NET Core 8, C#, SignalR WebSocket |
| **Database** | MongoDB 7.0 + Redis 7.2 |
| **Client** | Unity 6.3 LTS (C#), 27 scripts |
| **Admin Panel** | Next.js 14, TypeScript, Tailwind CSS, SVG Icons |
| **Auth** | JWT Bearer + Refresh Token Rotation |
| **Tick Rate** | 500ms/tick (configurable via `faith.tick_interval`) |
| **Max players/world** | 8 gods |
| **Map size** | 128×128 tiles by default (configurable, optional seed) |
| **Server .cs files** | 45 files, 31 service interfaces |
| **Admin pages** | 19 pages (excluding `_app`, `_document`) |
| **Unit tests** | 95 test cases (xUnit + Moq + FluentAssertions) |
| **CI/CD** | 4 GitHub Actions workflows |
| **Balance params** | 90 params, all runtime-tunable |
| **God archetypes** | 8 |
| **God ranks** | 7 (Forgotten → Ancient) |
| **Race types** | 8 with 8×8 affinity matrix |
| **NPC tiers** | 5 (Commoner → Royalty) |
| **Church ranks** | 14 (8 holy + 6 dark) |
| **Government types** | 6 |
| **Organization types** | 6 |
| **Miracles** | 15 (3 tiers) |
| **Doctrine axes** | 5 |
| **Believer types** | 5 |
| **Evolution stages** | 9 (3 paths × 3 stages) |
| **Dungeon types** | 5 |
| **Relic types** | 8 |
| **World Ages** | 5 (Early → Rebirth) |
| **Assets needed** | ~204 files (see ASSETS.md) |

### Simulation Loop Ticks

| Service | Frequency | Function |
|---|---|---|
| FaithService | Every tick | Faith gen with race affinity × rank multiplier |
| CivilizationSimulationService | Every tick | AI personalities, food cycle, government, rebellion |
| ReligionService | Every 5 ticks | Spread, schism, heresy, crusade |
| EvolutionService | Every 3 ticks | EXP accumulation, stage transitions |
| NPCInteractionService | Every 10 ticks | Crime, marriage, betrayal, luck, temptation events |
| MemoryService | Every 10 ticks | Relic faith gen, Forgotten god survival |
| OrganizationService | Every 20 ticks | Noble Houses, Guild missions, Court, Underground |
| AiDirectorService | Every 20 ticks | Age transitions, anti-stagnation, anti-snowball |
| AchievementService + EscortService | Every 30 ticks | Passive achievements, escort ticks, kidnap attempts |
| DungeonService | Every 50 ticks | Natural spawn, infestation check, DarkPortal warnings |
| GodRankService | Every 100 ticks | Rank updates, Forgotten state check |

### Unity Scripts Reference

| Script | Location | Purpose |
|---|---|---|
| `WorldFaithClient.cs` | `Network/` | SignalR hub connection, server events |
| `LobbyClient.cs` | `Network/` | Lobby hub (room management) |
| `ChatClient.cs` | `Network/` | Chat hub (in-game messages) |
| `MainThreadDispatcher.cs` | `Network/` | Dispatch SignalR callbacks to Unity main thread |
| `AuthManager.cs` | `Managers/` | Login, register, token refresh |
| `GameManager.cs` | `Managers/` | World state, event routing to UI |
| `AudioManager.cs` | `Audio/` | SFX and music layer management |
| `VfxManager.cs` | `VFX/` | Particle effect catalogue |
| `WorldRenderer.cs` | Root | Tile map rendering, entity positions |
| `CameraController.cs` | Root | Pan, zoom, world navigation |
| `GameHUD.cs` | `UI/Game/` | Main HUD (faith bar, tick, cycle, chat) |
| `WorldMapUI.cs` | `UI/Game/` | Minimap and full map view |
| `MiracleCounterUI.cs` | `UI/Game/` | Miracle counter-window UI |
| `GodSelectionScreen.cs` | `UI/Game/` | Archetype and name selection before game |
| `LobbyUI.cs` | `UI/Lobby/` | Room list, create/join room |
| `LoginUI.cs` | `UI/Lobby/` | Login and registration screens |

---

## 16. FAQ

**Q: "Cannot connect to Docker daemon" error?**  
A: Open Docker Desktop. Wait for the whale icon in the system tray to stop animating (can take 30-60 seconds), then retry.

**Q: Server says "Unable to connect to MongoDB"?**  
A: Database is not running. Run: `docker-compose up worldfaith-mongo worldfaith-redis -d`

**Q: Unity error "HubConnection could not be found"?**  
A: SignalR DLLs are missing. Repeat Step 3 (Install SignalR DLLs). After copying, press `Ctrl+R` in Unity to refresh assets.

**Q: The WorldFaith menu doesn't appear in Unity?**  
A: Wait for the compile progress bar (bottom-right) to finish. If still missing: **Assets → Refresh**. If there are red errors in the Console, fix those first.

**Q: Admin Panel shows 401 Unauthorized?**  
A: Session expired (60-minute token lifetime). Sign out and sign in again. Also verify `.env.local` has the correct `NEXT_PUBLIC_API_URL`.

**Q: Balance Config changes have no effect?**  
A: Wait up to 60 seconds for the cache to expire. If still not working, restart the server.

**Q: Playing on a LAN — how do other people connect?**  
A: Find your machine's LAN IP: `ipconfig` (Windows) or `ifconfig | grep inet` (Mac/Linux). Change the Unity Server URL from `http://localhost:5000/...` to `http://192.168.x.x:5000/...` (your actual IP). Rebuild and share the build.

**Q: Android build installs but crashes immediately?**  
A: Check the `logcat` output in Android Studio or `adb logcat | grep WorldFaith`. Most common causes: missing SignalR DLLs, or Minimum API Level set below Android 8.0.

**Q: WebGL build can't connect to the server?**  
A: Ensure your server has CORS enabled for the WebGL domain. Also check the browser console for Content Security Policy errors. The server URL in the build must match the domain you're serving the WebGL app from.

**Q: How do I reset all game data?**
```bash
docker-compose down -v   # WARNING: permanently deletes all data
docker-compose up worldfaith-mongo worldfaith-redis -d
# Then restart the server to re-seed the admin account
```

**Q: What is a Forgotten God?**  
A: A god whose followers dropped to 0. If they still have active relics or hidden cults, they survive in "Forgotten" state (faith gen ×0.1, capped at 500 Faith). If they have neither, they are permanently eliminated. Check Admin → Relics and Admin → Religions (filter IsHidden) to see what's keeping a god alive.

**Q: What is Doctrine Integrity?**  
A: A stat (0-100) that measures how closely an NPC lives according to their god's doctrine. High integrity = stronger divine power. Violating the doctrine reduces integrity. At 0-24 (Broken), the NPC may Fall — a Saint becomes a BloodSaint, a Prophet becomes a False Prophet. Admins can track this via the God Note → Warning Tags.

**Q: Why is my Saint's conversion chance so low even with high faith?**  
A: Three common causes: (1) The NPC's race has low affinity with your god's archetype. (2) The NPC is Tier 4/5 (Noble/Royalty) with inherently low openness (0.15-0.30). (3) Your religion's Doctrine doesn't match the NPC's personality. Check all three via Admin Panel → NPCs and → Religions.

**Q: How do I get the same map every time, or share a map with someone else?**  
A: Every world has a seed shown in Admin → Worlds and Admin → Maps. When creating a room, enter that same seed (and the same map width/height) and the terrain — continents, mountains, rivers, everything — will be identical. Leaving the seed blank picks a new random one each time.

---

*Need help? Open an issue at: https://github.com/thanhtinz/Game-new/issues*  
*WorldFaith v1.2 — "Players do not control the world. They influence belief, and belief controls the world."*
