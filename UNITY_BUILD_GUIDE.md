# WorldFaith — Unity Client Build Guide

Complete step-by-step instructions to set up, configure, and build the Unity client for all platforms.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Open the Project](#2-open-the-project)
3. [Install Required Packages](#3-install-required-packages)
4. [Install SignalR](#4-install-signalr)
5. [Link the Shared Library](#5-link-the-shared-library)
6. [Create the Three Scenes](#6-create-the-three-scenes)
7. [Assign UI References](#7-assign-ui-references)
8. [Configure Server URLs](#8-configure-server-urls)
9. [Assign Assets](#9-assign-assets)
10. [Validate the Setup](#10-validate-the-setup)
11. [Test in the Editor](#11-test-in-the-editor)
12. [Build for PC](#12-build-for-pc)
13. [Build for Android](#13-build-for-android)
14. [Build for iOS](#14-build-for-ios-mac-only)
15. [Build for WebGL](#15-build-for-webgl)
16. [Troubleshooting](#16-troubleshooting)

---

## 1. Prerequisites

You need these installed before opening Unity:

| Tool | Version | Download |
|---|---|---|
| Unity Hub | Latest | https://unity.com/download |
| Unity Editor | **6.3 LTS** | Via Unity Hub |
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download |
| Git | Any | https://git-scm.com |

**Unity modules to install alongside 6.3 LTS:**
- Android Build Support (includes Android SDK & NDK Tools + OpenJDK)
- iOS Build Support *(Mac only)*
- WebGL Build Support

To add modules to an existing Unity installation:  
Unity Hub → **Installs** → click the gear icon next to 6.3 LTS → **Add Modules**

---

## 2. Open the Project

```
1. Open Unity Hub
2. Click  Open  →  Add project from disk
3. Navigate to:  Game-new/client-unity/
4. Select the folder (do not go inside it)
5. Click  Add Project
6. Click on the project to open it — Unity Hub will ask which Editor version to use
7. Choose  Unity 6.3 LTS
8. Click  Open
```

**First-time import takes 5–15 minutes.** Red errors in the Console during this time are expected — they resolve after the next steps.

---

## 3. Install Required Packages

Go to **Window → Package Management → Package Manager** (this moved out of the top-level Window menu starting in Unity 6 — in Unity 2022.x it was directly under **Window → Package Manager**).

### 3a. Active Input Handling (required for Unity 6)

WorldFaith's `CameraController.cs` uses the **Legacy Input Manager** (`Input.GetTouch`, `Input.GetAxis`, `Input.mousePosition`, etc.), not the newer Input System package. Unity 6 projects can default to **Input System Package (New)** only, which would break this script.

```
1. Edit → Project Settings → Player
2. Under Other Settings, find Active Input Handling
3. Set it to  Input Manager (Old)  or  Both
4. Unity will prompt to restart the Editor — click Restart
```

If you'd rather migrate to the new Input System instead of using Legacy, that's a valid choice too, but it requires rewriting `CameraController.cs`'s input handling — out of scope for this guide.

### 3b. TextMeshPro

1. In Package Manager, change the dropdown from **In Project** to **Unity Registry**
2. Search for `TextMeshPro`
3. Click **Install**
4. After installation completes, go to:  
   **Window → TextMeshPro → Import TMP Essential Resources**
5. Click **Import** in the dialog that appears

### 3c. Newtonsoft JSON

1. Click the **+** button in the top-left corner of Package Manager
2. Choose **Add package by name**
3. Type exactly: `com.unity.nuget.newtonsoft-json`
4. Click **Add**

### 3d. Mobile Notifications (for Android and iOS push notifications)

1. Click **+** → **Add package by name**
2. Type: `com.unity.mobile.notifications`
3. Click **Add**

Wait for the bottom-right progress bar to clear before continuing.

---

## 4. Install SignalR

SignalR is the realtime connection library between the client and server. It is not available through the Unity Package Manager and must be installed manually.

### Mac / Linux

Run this script from the repository root:

```bash
cd Game-new

# Create the plugin folder
mkdir -p client-unity/Assets/Plugins/SignalR

# Create a temporary .NET project and add SignalR
cd /tmp
mkdir signalr_temp && cd signalr_temp
dotnet new console -n sr -o sr
cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release -o pub

# Copy the DLLs into Unity
cd /tmp/signalr_temp/sr
ROOT="$(cd - && pwd)"
TARGET="$ROOT/client-unity/Assets/Plugins/SignalR"

cp pub/Microsoft.AspNetCore.SignalR.Client.dll           "$TARGET/"
cp pub/Microsoft.AspNetCore.SignalR.Client.Core.dll      "$TARGET/"
cp pub/Microsoft.AspNetCore.SignalR.Common.dll           "$TARGET/"
cp pub/Microsoft.AspNetCore.SignalR.Protocols.Json.dll   "$TARGET/"
cp pub/Microsoft.AspNetCore.Http.Connections.Client.dll  "$TARGET/"
cp pub/Microsoft.AspNetCore.Http.Connections.Common.dll  "$TARGET/"

# Clean up
cd /tmp && rm -rf signalr_temp

echo "Done. DLLs installed."
```

### Windows (PowerShell)

```powershell
cd Game-New

# Create plugin folder
New-Item -ItemType Directory -Force -Path "client-unity\Assets\Plugins\SignalR"

# Create temp project
cd $env:TEMP
mkdir signalr_temp
cd signalr_temp
dotnet new console -n sr -o sr
cd sr
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
dotnet publish -c Release -o pub

# Copy DLLs
$target = "$PSScriptRoot\client-unity\Assets\Plugins\SignalR"
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Client.dll"           $target
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Client.Core.dll"      $target
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Common.dll"           $target
Copy-Item "pub\Microsoft.AspNetCore.SignalR.Protocols.Json.dll"   $target
Copy-Item "pub\Microsoft.AspNetCore.Http.Connections.Client.dll"  $target
Copy-Item "pub\Microsoft.AspNetCore.Http.Connections.Common.dll"  $target

# Clean up
cd $env:TEMP && Remove-Item -Recurse -Force signalr_temp
Write-Host "Done."
```

### Verify

In Unity, go to **Project window → Assets → Plugins → SignalR**. You should see 6 `.dll` files listed.

---

## 5. Link the Shared Library

The shared library contains enums, models, and contracts used by both the server and the client. Unity needs a copy of the source files.

### Mac / Linux

```bash
cd Game-New

SRC="shared/WorldFaith.Shared"
DST="client-unity/Assets/WorldFaith/Shared"

mkdir -p "$DST"
cp -r "$SRC/Enums"     "$DST/"
cp -r "$SRC/Models"    "$DST/"
cp -r "$SRC/Contracts" "$DST/"

echo "Shared library linked."
```

### Windows (PowerShell)

```powershell
$src = "shared\WorldFaith.Shared"
$dst = "client-unity\Assets\WorldFaith\Shared"

New-Item -ItemType Directory -Force -Path $dst
Copy-Item "$src\Enums","$src\Models","$src\Contracts" $dst -Recurse -Force

Write-Host "Shared library linked."
```

### Keeping it in sync

Whenever you update the server's shared library, re-run the copy command above or copy the changed files manually. The Shared folder in Unity is a copy, not a symlink.

**Wait for the Unity progress bar to clear** before the next step. The Console may show errors during import — they should resolve once compile finishes.

---

## 6. Create the Three Scenes

WorldFaith uses three scenes in a fixed load order. You must create them yourself using the built-in setup tools.

### Scene 1 — LoginScene

```
1. File → New Scene → Basic (Built-in) → Create
2. File → Save As...
   - Navigate to:  Assets/Scenes/
   - Name the file:  LoginScene
   - Click Save
3. Menu bar → WorldFaith → Setup → Create Login Scene Objects
4. File → Save  (Ctrl+S / Cmd+S)
```

What gets created: `AuthManager`, `MainThreadDispatcher`, `LobbyClient`, Canvas with `LoginUI`

### Scene 2 — LobbyScene

```
1. File → New Scene → Basic (Built-in) → Create
2. File → Save As...
   - Navigate to:  Assets/Scenes/
   - Name the file:  LobbyScene
   - Click Save
3. Menu bar → WorldFaith → Setup → Create Lobby Scene Objects
4. File → Save
```

What gets created: `AuthManager`, `LobbyClient`, `MainThreadDispatcher`, Canvas with `LobbyUI`, `GodSelectionScreen`

### Scene 3 — GameScene

```
1. File → New Scene → Basic (Built-in) → Create
2. File → Save As...
   - Navigate to:  Assets/Scenes/
   - Name the file:  GameScene
   - Click Save
3. Menu bar → WorldFaith → Setup → Create Game Scene Objects
4. File → Save
```

What gets created: all network clients, all managers, `WorldRenderer`, `CameraController`, all UI panels

> **The WorldFaith menu only appears after the Shared Library finishes importing.** If you don't see it, press **Assets → Refresh** (`Ctrl+R` / `Cmd+R`). If it still doesn't appear, check the Console for compile errors and fix them first.

---

## 7. Assign UI References

After running the scene setup, some `[SerializeField]` references need to be assigned manually. This section covers the most common ones.

### GameScene — WorldRenderer (2D Sprites)

Find `WorldRenderer` in the Hierarchy. In the Inspector, assign these under **Tile Prefabs** and **Marker Prefabs**:

| Field | What to assign |
|---|---|
| Grassland Sprite | Import `tile_grassland.png` as Sprite (2D and UI), drag here |
| Forest Sprite | `tile_forest.png` |
| Mountain Sprite | `tile_mountain.png` |
| Desert Sprite | `tile_desert.png` |
| Tundra Sprite | `tile_tundra.png` |
| Water Sprite | `tile_water.png` |
| Volcano Sprite | `tile_volcano.png` |
| Sacred Sprite | `tile_sacred.png` |
| Beach Sprite | `tile_beach.png` — sandy coastline tile |
| River Sprite | `tile_river.png` — narrower blue strip, distinct from Water |
| Temple Sprite | A small sprite icon for temples |
| City Marker Sprite | A dot or flag sprite for city markers |

**Quick way to create tile sprites:**
1. Import your tile texture PNG into `Assets/WorldFaith/World/Tiles/`
2. Select the texture in the Project window
3. In the Inspector, set **Texture Type → Sprite (2D and UI)** → Apply
4. Drag the sprite from the Project window onto the corresponding field in WorldRenderer
5. Repeat for each of the 10 tile types

The sprites are placed directly by `WorldRenderer` — no Prefab needed for tiles.

### GameScene — Main Camera (2D Orthographic)

WorldFaith renders the world map as a true 2D top-down scene on the XY plane (not 3D with a tilted camera). The `WorldFaithEditorTools` setup script configures this automatically, but it's worth understanding what it sets so you can tune it for your own map size:

| Setting | Default | Notes |
|---|---|---|
| Projection | Orthographic | Required — the game will not render correctly in Perspective mode |
| Position | `(64, 64, -10)` | Centered on a 128×128 map; Z must stay negative so the camera looks toward Z=0 where tiles live |
| Rotation | `(0, 0, 0)` | No tilt — straight top-down |
| Orthographic Size | `20` | Initial zoom level; `CameraController` will smoothly animate from here |

On the `CameraController` component (same GameObject), these fields control pan/zoom limits — adjust them if you use a map size other than the 128×128 default:

| Field | Default | Purpose |
|---|---|---|
| Pan Limit Min | `(-2, -2)` | World-space XY the camera cannot pan below |
| Pan Limit Max | `(130, 130)` | World-space XY the camera cannot pan beyond — should be slightly larger than `mapSize * tileSize` |
| Zoom Min / Max | `3` / `60` | Closest/furthest orthographic size allowed |

If you change the world's `Width`/`Height` (via Balance Config or `CreateWorldRequest`), update **Pan Limit Max** to roughly match the new map size in world units (`mapSize × tileSize`, where `tileSize` defaults to `1`).

### GameScene — AudioManager

Find `AudioManager` in the Hierarchy → look at Inspector → **Sfx Clips** array. Expand it and assign each SFX audio clip in the order of the `SfxId` enum (found in `Assets/WorldFaith/Shared/Enums/`).

Also assign the five music tracks under **Music**:
- Music Base → `music_base`
- Music Religion → `music_religion`
- Music War → `music_war`
- Music Apocalypse → `music_apocalypse`
- Music Victory → `music_victory`

### GameScene — VfxManager

Find `VfxManager` → **Catalog** array. Assign each VFX prefab in `VfxId` enum order. If you don't have VFX yet, leave the array empty — the game will still run without visual effects.

### GameScene — MinimapUI

Find `MinimapUI` → assign a `RawImage` UI element to **Minimap Image**. The image will display the render texture automatically.

### LoginScene and LobbyScene

These scenes have fewer references. Most are wired automatically by the Setup tool. Check the Console after Play for any `MissingReferenceException` errors and assign those specific fields.

---

## 8. Configure Server URLs

Each network script has a `[SerializeField]` `serverUrl` field visible in the Inspector. You need to set this in every scene that contains that script.

### Development (local)

| GameObject | Script | Field | Value |
|---|---|---|---|
| `AuthManager` | `AuthManager.cs` | Server Url | `http://localhost:5000` |
| `WorldFaithClient` | `WorldFaithClient.cs` | Server Url | `http://localhost:5000/hubs/world` |
| `LobbyClient` | `LobbyClient.cs` | Server Url | `http://localhost:5000/hubs/lobby` |
| `ChatClient` | `ChatClient.cs` | Server Url | `http://localhost:5000/hubs/chat` |

Each of these GameObjects exists in one or more scenes. Open each scene and set the URL.

### LAN (multiplayer on local network)

Replace `localhost` with your machine's LAN IP address:

```bash
# Find your LAN IP:
# Mac/Linux:
ifconfig | grep "inet " | grep -v 127.0.0.1

# Windows:
ipconfig | findstr "IPv4"
```

Example: `http://192.168.1.42:5000/hubs/world`

### Production

Replace with your deployed domain:
```
http://localhost:5000              →  https://api.yourdomain.com
http://localhost:5000/hubs/world   →  https://api.yourdomain.com/hubs/world
http://localhost:5000/hubs/lobby   →  https://api.yourdomain.com/hubs/lobby
http://localhost:5000/hubs/chat    →  https://api.yourdomain.com/hubs/chat
```

> **After changing URLs, rebuild the client.** URLs are baked into the build at compile time.

---

## 9. Assign Assets

### Minimum required assets (game cannot run without these)

**Tile sprites** — `Assets/WorldFaith/World/Tiles/` — 64×64 px PNG:

> After importing, select each PNG → Inspector → Texture Type: **Sprite (2D and UI)** → Pixels Per Unit: **64** → Apply

**10 tile types total** (the world generator produces all of these):


| File | Color hint |
|---|---|
| `tile_grassland.png` | `#4a9c2f` |
| `tile_forest.png` | `#1a5c1a` |
| `tile_mountain.png` | `#7a7a7a` |
| `tile_desert.png` | `#c8b44a` |
| `tile_tundra.png` | `#b0c8e0` |
| `tile_water.png` | `#2a64c8` |
| `tile_volcano.png` | `#c83210` |
| `tile_sacred.png` | `#c8a832` |
| `tile_beach.png` | `#e6d7a0` — sandy coastline |
| `tile_river.png` | `#468cdc` — narrower/brighter than Water |

**Sound effects** — `Assets/WorldFaith/Audio/SFX/` — 47 files  
See `ASSETS.md` for the full list with filenames matching the `SfxId` enum.

**Music** — `Assets/WorldFaith/Audio/Music/` — 5 files:
```
music_base.mp3
music_religion.mp3
music_war.mp3
music_apocalypse.mp3
music_victory.mp3
```

### Optional but recommended

| Type | Location |
|---|---|
| 28 VFX Prefabs (Particle Systems) | `Assets/WorldFaith/VFX/Prefabs/` |
| 8 God Archetype icons (128×128) | `Assets/WorldFaith/UI/Sprites/` |
| 15 Miracle icons (64×64) | `Assets/WorldFaith/UI/Sprites/` |
| Fonts: Cinzel, Nunito, Rajdhani | Download from fonts.google.com |

**To use custom fonts:**
1. Copy the `.ttf` file into `Assets/Fonts/`
2. **Window → TextMeshPro → Font Asset Creator**
3. Source Font File → select your `.ttf`
4. Click **Generate Font Atlas** → **Save**
5. Assign the generated `.asset` to TextMeshPro components in the UI

---

## 10. Validate the Setup

Run the built-in validator to catch missing references before building:

```
Menu: WorldFaith → Validate → Check All Managers
```

All items should show a green checkmark in the Console. Common warnings and fixes:

| Warning | Fix |
|---|---|
| `AudioManager: SFX clips not assigned` | Assign audio clips in AudioManager Inspector |
| `VfxManager: catalog empty` | Assign VFX prefabs or ignore if no VFX yet |
| `WorldRenderer: tile prefabs missing` | Create and assign tile prefabs (Step 7) |
| `SignalR DLLs not found` | Redo Step 4 |

You can also run individual checks:
```
WorldFaith → Validate → Check AudioManager Setup
WorldFaith → Validate → Check VfxManager Setup
```

---

## 11. Test in the Editor

Before building, always verify the client works in Play mode.

**Start the server first:**
```bash
cd Game-New/server/WorldFaith.Server
dotnet run
```

**Then in Unity:**
```
1. Open the LoginScene
2. Press Play (▶)
3. The Login screen appears
4. Click "Register" — fill in username, email, password, display name
5. Click Register → should redirect to login
6. Log in → Lobby screen appears
7. Click "Create Room" → room is created
8. Click "Start" (you are alone, so it may not start — adjust min players in Balance Config)
9. Alternatively: open a second Unity instance with a different project copy and join the room
```

**Check the Console for errors during Play.** Common issues:
- `Connection refused` → server is not running or URL is wrong
- `401 Unauthorized` → token expired, re-login
- `NullReferenceException on XxxManager` → a [SerializeField] is not assigned

---

## 12. Build for PC

### Windows

```
File → Build Settings
```

1. Click **Add Open Scenes** or drag scenes manually in this order:
   ```
   0  Assets/Scenes/LoginScene
   1  Assets/Scenes/LobbyScene
   2  Assets/Scenes/GameScene
   ```
2. Platform: **PC, Mac & Linux Standalone**
3. Target Platform: **Windows**
4. Architecture: **x86_64**
5. Click **Build**
6. Choose an output folder (e.g., `Builds/Windows/`)
7. Wait for the build to complete

Output: a folder containing `WorldFaith.exe` and a `WorldFaith_Data/` folder. Distribute both together.

### Mac

Same steps, but:
- Target Platform: **macOS**
- Architecture: **Intel 64-bit + Apple Silicon** (for universal binary) or **Apple Silicon** only

Output: `WorldFaith.app` — a self-contained application bundle. Right-click → Open on first launch to bypass Gatekeeper.

### Linux

- Target Platform: **Linux**
- Architecture: **x86_64**

Output: a `WorldFaith.x86_64` executable. Make it executable:
```bash
chmod +x WorldFaith.x86_64
./WorldFaith.x86_64
```

---

## 13. Build for Android

### One-time phone setup

On the Android device:

```
1. Settings → About Phone (or About Device)
2. Find "Build Number"
3. Tap it exactly 7 times — "Developer Mode enabled" appears
4. Go back to Settings → Developer Options (may appear at the bottom)
5. Enable "USB Debugging"
6. Connect the phone to your computer via USB
7. On the phone, tap "Allow" when asked about USB debugging for this computer
```

### Unity Player Settings

Go to **File → Build Settings → Player Settings** (bottom-left button):

| Section | Field | Value |
|---|---|---|
| Other Settings | Package Name | `com.yourname.worldfaith` (lowercase, no spaces) |
| Other Settings | Version | `1.0` |
| Other Settings | Bundle Version Code | `1` |
| Other Settings | Minimum API Level | **Android 8.0 (API level 26)** |
| Other Settings | Target API Level | **Automatic (highest installed)** |
| Other Settings | Scripting Backend | **IL2CPP** |
| Other Settings | Target Architectures | **ARMv7** + **ARM64** |
| Publishing Settings | Custom Main Manifest | *(leave unchecked unless you need custom permissions)* |

### Build

In **Build Settings**:
1. Platform: **Android**
2. Click **Switch Platform** (only needed once — wait for reimport)
3. Connect your Android phone
4. Click **Build And Run** to build and install directly on the connected phone

Or click **Build** to create an `.apk` file you can install manually:
```bash
adb install -r WorldFaith.apk
```

### For Google Play submission

In Player Settings → Publishing Settings:
- Enable **Custom Keystore** and create/use a signing keystore
- In Build Settings, enable **Build App Bundle (.aab)**
- Click **Build** — upload the `.aab` to Google Play Console

### Common Android issues

| Problem | Solution |
|---|---|
| Phone not detected | Enable USB Debugging, try a different USB cable |
| `INSTALL_FAILED_VERSION_DOWNGRADE` | Uninstall the old version from the phone first |
| Build fails with NDK error | Unity Hub → Installs → Add Modules → Android Build Support (re-install) |
| App crashes on launch | Run `adb logcat -s Unity` to see the error log |
| `Failed to connect to server` | Use your LAN IP instead of `localhost` in the Server URL fields |
| Large APK size | Enable **Split Application Binary** in Player Settings |

---

## 14. Build for iOS (Mac only)

iOS builds require a Mac with Xcode installed.

### Prerequisites

- **Xcode 14+** — install from the Mac App Store (it's free, ~10 GB)
- **Apple Developer Account** — free for device testing, $99/year for App Store
- A physical iPhone or iPad for testing (or use the Xcode Simulator)

### Unity Player Settings

Go to **File → Build Settings → Player Settings**:

| Section | Field | Value |
|---|---|---|
| Other Settings | Bundle Identifier | `com.yourname.worldfaith` |
| Other Settings | Version | `1.0` |
| Other Settings | Build | `1` |
| Other Settings | Camera Usage Description | `Used for AR features` *(if applicable)* |
| Other Settings | Microphone Usage Description | `Used for voice chat` *(if applicable)* |
| Other Settings | Scripting Backend | **IL2CPP** |

### Build in Unity

1. Platform: **iOS**
2. Click **Switch Platform** (wait for reimport)
3. Click **Build** — Unity generates an Xcode project folder (e.g., `Builds/iOS/`)

### Finish in Xcode

```
1. Open the generated Xcode project:
   - Navigate to Builds/iOS/
   - Open  Unity-iPhone.xcodeproj

2. In Xcode, select the project root in the left panel (the blue icon)

3. Click  Signing & Capabilities  tab

4. Under  Team:
   - Click the dropdown → select your Apple Developer account
   - If you don't have one, sign in at developer.apple.com

5. Xcode may show a "Provisioning Profile" error — click  Fix Issue  to auto-resolve

6. Connect your iPhone via USB cable

7. In the top toolbar, select your device from the device dropdown
   (next to the play/stop buttons)

8. Press  Cmd+R  (or click the Play button) to build and install on your device

9. On the iPhone:
   Settings → General → VPN & Device Management → Trust your developer certificate
```

### For App Store submission

```
1. In Xcode: Product → Archive
2. Wait for archiving to complete
3. The Organizer window opens automatically
4. Click  Distribute App
5. Choose  App Store Connect
6. Follow the wizard to upload to App Store Connect
7. In App Store Connect (appstoreconnect.apple.com):
   - Create an app listing if you haven't already
   - Select the uploaded build
   - Submit for review
```

### Common iOS issues

| Problem | Solution |
|---|---|
| "No signing certificate" | Add your Apple account to Xcode → Preferences → Accounts |
| "Provisioning profile doesn't include device" | In Xcode → Signing → enable "Automatically manage signing" |
| App crashes on iPhone | Xcode → Window → Devices and Simulators → select device → View Device Logs |
| Build fails with bitcode error | Player Settings → Other Settings → disable **Enable Bitcode** |

---

## 15. Build for WebGL

WebGL runs in a browser — no installation required for players.

### Unity Player Settings

Go to **File → Build Settings → Player Settings**:

| Section | Field | Value |
|---|---|---|
| Resolution and Presentation | Default Canvas Width | `1280` |
| Resolution and Presentation | Default Canvas Height | `720` |
| Publishing Settings | Compression Format | **Gzip** |
| Publishing Settings | Enable Exceptions | **Explicitly Thrown Exceptions Only** |
| Publishing Settings | Data Caching | Enabled |

### Build

1. Platform: **WebGL**
2. Click **Switch Platform**
3. Click **Build**
4. Choose output folder (e.g., `Builds/WebGL/`)

Output: a folder with `index.html` and supporting files.

### Host the build

You need to serve the folder from a web server. You **cannot** open `index.html` directly in a browser.

**Local testing:**
```bash
cd Builds/WebGL
python3 -m http.server 8080
# Open http://localhost:8080 in browser
```

**Production hosting options:**
- **Nginx / Apache** — copy the folder to your web server's root and configure:
  ```nginx
  # Required headers for WebGL
  add_header Cross-Origin-Opener-Policy same-origin;
  add_header Cross-Origin-Embedder-Policy require-corp;
  ```
- **GitHub Pages** — push the folder to a `gh-pages` branch
- **Netlify / Vercel** — drag and drop the folder in their dashboard

### CORS configuration

The server must allow requests from your WebGL domain. Add to `appsettings.json`:
```json
"AllowedOrigins": ["https://yourgame.netlify.app", "http://localhost:8080"]
```

### WebGL limitations

- No file system access (PlayerPrefs still works — uses browser localStorage)
- SignalR uses WebSocket automatically — works fine
- Large binary files increase load time — use **Gzip** compression (already set above)
- Mobile browsers may have performance issues — test on target devices

---

## 16. Troubleshooting

### Unity Editor issues

| Problem | Solution |
|---|---|
| WorldFaith menu missing | Wait for compile, then **Assets → Refresh** (Ctrl+R) |
| Red errors on first open | Normal — resolves after packages and shared library are installed |
| `The type or namespace 'Microsoft.AspNetCore.SignalR' not found` | SignalR DLLs missing — redo Step 4 |
| `The type or namespace 'WorldFaith.Shared' not found` | Shared Library not linked — redo Step 5 |
| `NullReferenceException` on any Manager | A `[SerializeField]` is not assigned in the Inspector |
| Scenes missing from Build Settings | Drag them from the Project window into Build Settings |
| Touch/mouse input does nothing in `CameraController` | Active Input Handling is set to "Input System Package (New)" only — redo Step 3a |

### Connection issues

| Problem | Solution |
|---|---|
| `Failed to connect to server` in Play mode | Server is not running. Run `dotnet run` first. |
| `401 Unauthorized` | Token expired. Sign out and sign in again. |
| `CORS error` in Console | Server CORS config doesn't include your origin. Check `appsettings.json`. |
| Works on PC but not on phone | Phone must use LAN IP (`192.168.x.x`), not `localhost` |
| WebGL: connection immediately closes | Check browser Console for WebSocket errors. Ensure server has WebSocket enabled. |

### Build issues

| Problem | Solution |
|---|---|
| Android: NDK not found | Unity Hub → Installs → gear on 6.3 → Add Modules → Android Build Support |
| Android: app crashes immediately | `adb logcat -s Unity` to see Unity crash log |
| iOS: code signing error | Xcode → Signing & Capabilities → select your Team |
| iOS: provisioning profile error | Click **Fix Issue** in Xcode or enable Automatic Signing |
| WebGL: black screen | Open browser Developer Tools → Console tab for JavaScript errors |
| PC: antivirus blocks the build | Add the build folder to antivirus exclusions |

### Performance issues

| Platform | Optimization |
|---|---|
| Android | Enable **IL2CPP** scripting backend, enable **ARM64**, disable unused modules in Player Settings |
| iOS | Same as Android |
| WebGL | Enable **Gzip compression**, reduce texture sizes, limit active particles |
| PC | Enable **GPU Instancing** on tile materials if map rendering is slow |

---

## Quick Reference

### Files to configure before building

```
client-unity/Assets/WorldFaith/Network/WorldFaithClient.cs  → serverUrl field
client-unity/Assets/WorldFaith/Network/LobbyClient.cs       → serverUrl field
client-unity/Assets/WorldFaith/Network/ChatClient.cs        → serverUrl field
client-unity/Assets/WorldFaith/Managers/AuthManager.cs      → serverUrl field
```

Set these via the Unity Inspector — do **not** edit the `.cs` files directly. Open each scene, find the GameObject, select it, and change the field in the Inspector panel.

### Scene → GameObjects that need URL configuration

| Scene | GameObject | URL type |
|---|---|---|
| LoginScene | `AuthManager` | Base server URL |
| LobbyScene | `AuthManager`, `LobbyClient` | Base URL + lobby hub |
| GameScene | `WorldFaithClient`, `ChatClient`, `AuthManager` | World hub + chat hub + base URL |

### Build order checklist

```
[ ] Unity 6.3 LTS installed with required modules
[ ] Project opened and initial import completed
[ ] Active Input Handling set to Input Manager (Old) or Both (Edit → Project Settings → Player)
[ ] TextMeshPro installed + Essential Resources imported
[ ] Newtonsoft JSON installed
[ ] SignalR DLLs in Assets/Plugins/SignalR/ (6 DLL files)
[ ] Shared Library copied to Assets/WorldFaith/Shared/
[ ] LoginScene created and saved in Assets/Scenes/
[ ] LobbyScene created and saved in Assets/Scenes/
[ ] GameScene created and saved in Assets/Scenes/
[ ] Server URLs set in all scenes
[ ] Tile sprites assigned to WorldRenderer fields (10 types: Grassland, Forest,
    Mountain, Desert, Tundra, Water, Volcano, Sacred, Beach, River)
[ ] Main Camera confirmed Orthographic with CameraController pan limits matching map size
[ ] Audio clips assigned to AudioManager
[ ] WorldFaith → Validate → Check All Managers → all green
[ ] Tested in Play mode — login and lobby work
[ ] Build Settings: 3 scenes in correct order (Login=0, Lobby=1, Game=2)
[ ] Target platform selected and Switch Platform done
[ ] Player Settings configured for target platform
[ ] Build complete and tested on target device
```

---

*WorldFaith — https://github.com/thanhtinz/Game-new*
