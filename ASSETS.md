# WorldFaith — Danh Sách Asset Cần Thiết

> Tài liệu này liệt kê **toàn bộ asset** cần chuẩn bị để game chạy đầy đủ.  
> Code đã xây dựng xong — phần còn thiếu là các file media, prefab, và texture.

---

## Mục lục

1. [Audio — SFX](#1-audio--sfx-47-file)
2. [Audio — Nhạc Nền](#2-audio--nhạc-nền-5-file)
3. [VFX — Particle Prefabs](#3-vfx--particle-prefabs-28-file)
4. [UI — Sprites & Textures](#4-ui--sprites--textures)
5. [World — Tile Textures](#5-world--tile-textures-8-biome)
6. [Nhân Vật & Icon](#6-nhân-vật--icon)
7. [Fonts](#7-fonts)
8. [Animation Clips](#8-animation-clips)
9. [ScriptableObjects Config](#9-scriptableobjects-config)
10. [Scene Setup Checklist](#10-scene-setup-checklist)
11. [Đề xuất nguồn tải miễn phí](#11-đề-xuất-nguồn-tải-miễn-phí)

---

## 1. Audio — SFX (47 file)

Đặt tại: `Assets/WorldFaith/Audio/SFX/`  
Format: `.wav` hoặc `.mp3`, mono, 44100Hz

### UI (7 file)
| Tên file | Mô tả | Ghi chú |
|----------|-------|---------|
| `sfx_btn_click.wav` | Nhấn nút | Short click, ~0.1s |
| `sfx_btn_hover.wav` | Hover nút | Subtle, ~0.05s |
| `sfx_panel_open.wav` | Mở panel | Woosh nhẹ, ~0.2s |
| `sfx_panel_close.wav` | Đóng panel | Reverse woosh, ~0.2s |
| `sfx_tab_switch.wav` | Đổi tab | Tick nhẹ, ~0.1s |
| `sfx_notification.wav` | Thông báo | Bell/chime, ~0.3s |
| `sfx_error.wav` | Lỗi | Buzz ngắn, ~0.2s |
| `sfx_success.wav` | Thành công | Positive chime, ~0.3s |

### Miracles Tier 1 (5 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_miracle_rain.wav` | Mưa bắt đầu rơi — rain drops |
| `sfx_miracle_dream.wav` | Giấc mơ thần linh — ethereal hum |
| `sfx_miracle_bless_harvest.wav` | Ban phước mùa màng — warm chime |
| `sfx_miracle_heal.wav` | Chữa lành — gentle heal sound |
| `sfx_miracle_omen.wav` | Điềm báo — mysterious whisper |

### Miracles Tier 2 (5 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_miracle_storm.wav` | Bão nổi lên — thunder crack |
| `sfx_miracle_earthquake.wav` | Động đất — deep rumble |
| `sfx_miracle_curse.wav` | Lời nguyền — dark whoosh |
| `sfx_miracle_portal.wav` | Cổng thần — energy hum |
| `sfx_miracle_divine_voice.wav` | Tiếng thần — reverb voice |

### Miracles Tier 3 (5 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_miracle_volcano.wav` | Núi lửa phun — explosion + rumble |
| `sfx_miracle_demon_invasion.wav` | Quỷ xâm chiếm — dark portal open |
| `sfx_miracle_divine_beast.wav` | Thần thú xuất hiện — roar |
| `sfx_miracle_revelation.wav` | Khải thị — choir + light burst |
| `sfx_miracle_holy_war.wav` | Thánh chiến — war horn |

### Miracle Counter (1 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_miracle_countered.wav` | Phép bị phản — clash/cancel sound |

### Religion (6 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_religion_founded.wav` | Lập tôn giáo — triumphant bell |
| `sfx_temple_built.wav` | Xây đền — construction complete |
| `sfx_conversion.wav` | Cải đạo — soft bell |
| `sfx_schism.wav` | Giáo hội phân rẽ — crack sound |
| `sfx_heresy.wav` | Dị giáo — whisper |
| `sfx_crusade.wav` | Thánh chiến — battle horn |

### Evolution (3 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_entity_evolve.wav` | Sinh vật tiến hóa — transform sound |
| `sfx_entity_apex.wav` | Apex entity xuất hiện — dramatic sting |
| `sfx_entity_attack.wav` | Entity tấn công — impact |

### Civilization (3 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_civ_founded.wav` | Thành lập civ — fanfare |
| `sfx_civ_collapsed.wav` | Civ sụp đổ — crumble + bell |
| `sfx_civ_at_war.wav` | Chiến tranh bùng nổ — drums |

### Game State (5 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_world_rebirth.wav` | Thế giới tái sinh — epic whoosh |
| `sfx_god_faded.wav` | Thần biến mất — fade out chime |
| `sfx_victory.wav` | Chiến thắng — epic victory fanfare |
| `sfx_defeat.wav` | Thất bại — somber end |
| `sfx_follower_prayer.wav` | Tín đồ cầu nguyện — ambient chant |

### NPC Events (4 file)
| Tên file | Mô tả |
|----------|-------|
| `sfx_npc_marriage.wav` | Đám cưới — cheerful bells |
| `sfx_npc_betrayal.wav` | Phản bội — tense sting |
| `sfx_npc_assassination.wav` | Ám sát — short dramatic hit |
| `sfx_npc_coronation.wav` | Đăng quang — royal fanfare |

---

## 2. Audio — Nhạc Nền (5 file)

Đặt tại: `Assets/WorldFaith/Audio/Music/`  
Format: `.mp3` hoặc `.ogg`, stereo, 44100Hz, seamlessly loopable

| Tên file | Layer | Khi nào phát | BPM gợi ý |
|----------|-------|-------------|-----------|
| `music_base.mp3` | Base | Luôn phát (muted khi pause) | 70-90 BPM |
| `music_religion.mp3` | Religion | Fade in khi ≥ 3 tôn giáo | 80-100 BPM |
| `music_war.mp3` | War | Fade in khi có Holy War | 110-130 BPM |
| `music_apocalypse.mp3` | Apocalypse | Fade in khi có Apex entity | 140-160 BPM |
| `music_victory.mp3` | Victory | One-shot khi thắng | Không cần loop |

> **Ghi chú:** Các layer phát đồng thời, fade in/out độc lập. Cần cùng key hoặc compatible keys.

---

## 3. VFX — Particle Prefabs (28 file)

Đặt tại: `Assets/WorldFaith/VFX/Prefabs/`  
Format: Unity Prefab (`.prefab`) với Particle System component

### Miracles
| Tên prefab | VFX Effect | Scale gợi ý |
|------------|-----------|------------|
| `VFX_Rain.prefab` | Rain particles từ trên xuống | 2.0x |
| `VFX_Storm.prefab` | Lightning + dark clouds | 3.0x |
| `VFX_Earthquake.prefab` | Ground crack + dust | 2.0x |
| `VFX_Volcano.prefab` | Lava splash + smoke column | 5.0x |
| `VFX_BlessHarvest.prefab` | Golden sparkles + leaf | 1.5x |
| `VFX_Curse.prefab` | Purple/black smoke + skulls | 1.5x |
| `VFX_HolyLight.prefab` | White/gold light burst | 2.0x |
| `VFX_DarkExplosion.prefab` | Dark energy burst | 2.0x |
| `VFX_DivineBeam.prefab` | Beam of light from sky | 3.0x |
| `VFX_DemonPortal.prefab` | Red/black swirling portal | 2.0x |

### Evolution
| Tên prefab | VFX Effect |
|------------|-----------|
| `VFX_EvolveBasic.prefab` | Glow + transform flash |
| `VFX_EvolveApex.prefab` | Massive glow + shockwave |
| `VFX_DivineBeastSpawn.prefab` | Golden particles + roar wave |
| `VFX_ApocalypticSpawn.prefab` | Dark explosion + red lightning |

### Religion
| Tên prefab | VFX Effect |
|------------|-----------|
| `VFX_TempleBuilt.prefab` | Construction sparkle |
| `VFX_ReligionSpread.prefab` | Wave spreading outward |
| `VFX_Crusade.prefab` | Holy fire + sword flash |
| `VFX_Schism.prefab` | Split/crack effect |

### God / Faith
| Tên prefab | VFX Effect |
|------------|-----------|
| `VFX_FaithGain.prefab` | Rising gold particles |
| `VFX_FaithLost.prefab` | Falling grey particles |
| `VFX_GodFaded.prefab` | Dissolve effect |
| `VFX_SacredGlow.prefab` | Persistent golden aura (loop) |

### World
| Tên prefab | VFX Effect |
|------------|-----------|
| `VFX_WorldRebirth.prefab` | Massive light wave |
| `VFX_CivCollapse.prefab` | Crumble + dust cloud |
| `VFX_CivFounded.prefab` | Expansion ripple |
| `VFX_MiracleCountered.prefab` | Impact clash + spark |

### NPC Events
| Tên prefab | VFX Effect |
|------------|-----------|
| `VFX_Marriage.prefab` | Heart particles + confetti |
| `VFX_Betrayal.prefab` | Red flash + shadow |

---

## 4. UI — Sprites & Textures

Đặt tại: `Assets/WorldFaith/UI/Sprites/`  
Format: `.png`, transparent background (trừ backgrounds)

### Icons (32×32 hoặc 64×64 px)
| Tên file | Dùng cho |
|----------|---------|
| `icon_faith.png` | Faith resource indicator |
| `icon_trust.png` | Trust stat |
| `icon_fear.png` | Fear stat |
| `icon_followers.png` | Follower count |
| `icon_temple.png` | Temple count |
| `icon_devotion.png` | Devotion level |
| `icon_miracle.png` | Miracle tab |
| `icon_religion.png` | Religion tab |
| `icon_map.png` | Map tab |
| `icon_chat.png` | Chat tab |
| `icon_evolution.png` | Evolution tab |
| `icon_commandment.png` | Commandment tab |
| `icon_settings.png` | Settings |
| `icon_leaderboard.png` | Leaderboard |

### Archetype Icons (128×128 px, mỗi loại 1 icon)
| Tên file | Archetype |
|----------|----------|
| `arch_order.png` | ⚖️ Order — scale/balance symbol |
| `arch_chaos.png` | 🌪️ Chaos — swirl/vortex |
| `arch_light.png` | ☀️ Light — sun/rays |
| `arch_darkness.png` | 🌑 Darkness — moon/shadow |
| `arch_nature.png` | 🌿 Nature — leaf/tree |
| `arch_death.png` | 💀 Death — skull/hourglass |
| `arch_knowledge.png` | 📚 Knowledge — eye/book |
| `arch_war.png` | ⚔️ War — crossed swords |

### Miracle Icons (64×64 px)
Tên theo pattern: `miracle_[name].png`  
15 file: `miracle_rain`, `miracle_dream`, `miracle_bless_harvest`, `miracle_heal`, `miracle_omen`, `miracle_storm`, `miracle_earthquake`, `miracle_curse`, `miracle_portal`, `miracle_divine_voice`, `miracle_volcano`, `miracle_demon_invasion`, `miracle_divine_beast`, `miracle_revelation`, `miracle_holy_war`

### UI Backgrounds & Panels
| Tên file | Kích thước | Dùng cho |
|----------|-----------|---------|
| `bg_main_menu.png` | 1920×1080 | Màn hình chính |
| `bg_lobby.png` | 1920×1080 | Lobby background |
| `bg_game_overlay.png` | 1920×1080 | Subtle game HUD bg |
| `panel_dark.png` | 512×512 (9-slice) | Panel container |
| `panel_gold.png` | 512×512 (9-slice) | Selected/active panel |
| `btn_normal.png` | 256×64 (9-slice) | Button default |
| `btn_hover.png` | 256×64 (9-slice) | Button hover |
| `btn_pressed.png` | 256×64 (9-slice) | Button pressed |
| `btn_disabled.png` | 256×64 (9-slice) | Button disabled |
| `scrollbar_bg.png` | 32×256 | Scrollbar background |
| `scrollbar_handle.png` | 32×64 | Scrollbar handle |
| `progress_bar_bg.png` | 256×32 | Progress bar bg |
| `progress_bar_fill.png` | 256×32 | Faith/Trust bar fill |

---

## 5. World — Tile Textures (8 Biome)

Đặt tại: `Assets/WorldFaith/World/Tiles/`  
Format: `.png`, 64×64 px hoặc 128×128 px

| Tên file | Biome | Màu chủ đạo |
|----------|-------|------------|
| `tile_grassland.png` | Đồng cỏ | `#4a9c2f` xanh lá |
| `tile_forest.png` | Rừng | `#1a5c1a` xanh đậm |
| `tile_mountain.png` | Núi | `#7a7a7a` xám |
| `tile_desert.png` | Sa mạc | `#c8b44a` vàng |
| `tile_tundra.png` | Tundra | `#b0c8e0` xanh nhạt |
| `tile_water.png` | Nước | `#2a64c8` xanh dương |
| `tile_volcano.png` | Núi lửa | `#c83210` đỏ cam |
| `tile_sacred.png` | Sacred | `#c8a832` vàng gold + glow |

> **Ghi chú:** Tile Sacred cần có hiệu ứng shimmer hoặc borde glow để nổi bật trên minimap.

### Tile Overlay Sprites
| Tên file | Dùng cho |
|----------|---------|
| `overlay_temple.png` | Hiển thị temple trên tile |
| `overlay_civ_border.png` | Viền lãnh thổ civilization |
| `overlay_religion_tint.png` | Màu tôn giáo phủ lên tile |
| `overlay_sacred_glow.png` | Aura vàng cho Sacred tile |
| `overlay_war.png` | Chỉ báo vùng đang có chiến tranh |

---

## 6. Nhân Vật & Icon

### NPC Tier Icons (64×64 px)
Đặt tại: `Assets/WorldFaith/UI/Sprites/NPCIcons/`

| Tên file | Tier | Style |
|----------|------|-------|
| `npc_commoner.png` | Commoner | Người thường, quần áo đơn giản |
| `npc_servant.png` | Servant | Áo hầu, nón hầu |
| `npc_adventurer.png` | Adventurer | Áo giáp nhẹ, kiếm |
| `npc_noble.png` | Noble | Áo quý tộc, vương miện nhỏ |
| `npc_royalty.png` | Royalty | Áo hoàng gia, vương miện lớn |
| `npc_champion.png` | Champion | Áo giáp thần thánh, hào quang |

### Organization Icons (64×64 px)
| Tên file | Organization |
|----------|-------------|
| `org_kingdom.png` | Lâu đài/thành |
| `org_royal_court.png` | Ngai vàng |
| `org_noble_house.png` | Gia huy |
| `org_adventure_guild.png` | Cung kiếm đôi |
| `org_religious.png` | Đền thờ |
| `org_underground.png` | Đầu lâu/mặt nạ |

### Entity Stage Icons (64×64 px)
Đặt tại: `Assets/WorldFaith/UI/Sprites/EntityIcons/`

| Tên file | Stage |
|----------|-------|
| `entity_wild_animal.png` | Wolf/bear silhouette |
| `entity_divine_beast.png` | Winged beast với aura |
| `entity_celestial_guardian.png` | Armored celestial being |
| `entity_human_hero.png` | Armored human |
| `entity_saint.png` | Holy figure với halo |
| `entity_fallen_demon.png` | Demon với wings |
| `entity_monster.png` | Dark creature |
| `entity_titan.png` | Giant armored being |
| `entity_apocalyptic.png` | World-ending entity |

---

## 7. Fonts

Đặt tại: `Assets/WorldFaith/UI/Fonts/`  
Format: `.ttf` hoặc `.otf`, sau đó tạo **TextMeshPro Font Asset**

| Tên font | Dùng cho | Style |
|----------|---------|-------|
| `font_title.ttf` | Tiêu đề, tên game | Serif, fantasy (vd: Cinzel) |
| `font_body.ttf` | Text thường, UI | Sans-serif dễ đọc (vd: Nunito) |
| `font_ui.ttf` | Số liệu, stats | Monospace hoặc compact (vd: Rajdhani) |
| `font_chat.ttf` | Chat messages | Nhỏ gọn, dễ đọc nhiều dòng |

> **Fonts miễn phí đề xuất (Google Fonts):**  
> - Cinzel / Cinzel Decorative (fantasy title)  
> - Nunito (body text, dễ đọc)  
> - Rajdhani (UI numbers)  
> - Noto Sans (hỗ trợ tiếng Việt đầy đủ)

---

## 8. Animation Clips

Đặt tại: `Assets/WorldFaith/UI/Animations/`

### UI Panel Animations
| Tên file | Mô tả | Duration |
|----------|-------|---------|
| `anim_panel_open.anim` | Panel slide in + fade | 0.25s |
| `anim_panel_close.anim` | Panel slide out + fade | 0.2s |
| `anim_toast_show.anim` | Toast notification appear | 0.3s |
| `anim_toast_hide.anim` | Toast disappear | 0.3s |
| `anim_button_press.anim` | Button scale down | 0.1s |
| `anim_tab_switch.anim` | Tab content transition | 0.15s |

### Game Animations
| Tên file | Mô tả | Duration |
|----------|-------|---------|
| `anim_faith_gain.anim` | Faith counter tăng số | 0.5s |
| `anim_victory_screen.anim` | Victory screen animate in | 0.8s |
| `anim_defeat_screen.anim` | Defeat screen animate in | 0.8s |
| `anim_countdown.anim` | Lobby countdown 3-2-1 | 3.0s |
| `anim_tutorial_highlight.anim` | Highlight frame pulse | 1.0s loop |
| `anim_miracle_cooldown.anim` | Miracle button cooldown fill | variable |

### Minimap
| Tên file | Mô tả |
|----------|-------|
| `anim_camera_indicator_pulse.anim` | Camera position indicator nhấp nháy |

---

## 9. ScriptableObjects Config

Đặt tại: `Assets/WorldFaith/Config/`  
Tạo trong Unity: `Right Click > Create > WorldFaith > ...`

> Code `[CreateAssetMenu]` cần thêm nếu muốn dùng ScriptableObject.  
> Hiện tại server-side balance config qua MongoDB — phần dưới là config client-side.

| Tên file | Nội dung |
|----------|---------|
| `ServerConfig.asset` | Server URL, Hub paths, timeout settings |
| `GameplayConfig.asset` | Tile size, camera bounds, minimap size |
| `AudioConfig.asset` | Master volume defaults |
| `ArchetypeVisualConfig.asset` | Color per archetype, icon per archetype |
| `TileVisualConfig.asset` | Color per biome, sprite per biome |
| `EntityVisualConfig.asset` | Icon per evolution stage |

---

## 10. Scene Setup Checklist

Sau khi cài đủ packages và copy asset vào, làm theo thứ tự:

### LoginScene
- [ ] Chạy **WorldFaith → Setup → Create Login Scene Objects**
- [ ] Gán font cho tất cả TextMeshPro components
- [ ] Gán background sprite `bg_main_menu.png`
- [ ] Gán `sfx_btn_click.wav` vào AudioManager
- [ ] Test đăng nhập kết nối server

### LobbyScene
- [ ] Chạy **WorldFaith → Setup → Create Lobby Scene Objects**
- [ ] Gán scenario dropdown options: Standard, TheLastLight, ReligionWars, EvolutionRace, FaithCrisis, Apocalypse
- [ ] Gán `bg_lobby.png`
- [ ] Test tạo phòng và join phòng

### GameScene
- [ ] Chạy **WorldFaith → Setup → Create Game Scene Objects**
- [ ] Gán tile sprites vào WorldRenderer (8 biome × texture)
- [ ] Gán 47 SFX clips vào `AudioManager.sfxClips[]` **theo đúng thứ tự SfxId enum**
  - Xem thứ tự tại Inspector → AudioManager → (Custom Editor hiển thị danh sách)
- [ ] Gán 5 music clips vào AudioManager
  - `musicBase`, `musicReligion`, `musicWar`, `musicApocalypse`, `musicVictory`
- [ ] Gán 28 VFX prefabs vào `VfxManager.catalog[]` theo đúng VfxId enum
- [ ] Gán Minimap RawImage + texture size 128
- [ ] Gán tutorial highlight frame và tooltip panel
- [ ] Cấu hình Server URL trong WorldFaithClient, LobbyClient, ChatClient
- [ ] Chạy **WorldFaith → Validate → Check All Managers** → tất cả ✅

### Build Settings
- [ ] Scene thứ tự: 0=LoginScene, 1=LobbyScene, 2=GameScene
- [ ] Platform: PC, Mac & Linux Standalone
- [ ] Color Space: Linear (khuyến nghị)
- [ ] Android: Minimum API 26

---

## 11. Đề xuất nguồn tải miễn phí

### Audio SFX
| Nguồn | Link | Ghi chú |
|-------|------|---------|
| **Freesound.org** | https://freesound.org | Miễn phí, cần attribution |
| **ZapSplat** | https://zapsplat.com | Miễn phí với account |
| **Sonniss GDC Pack** | https://sonniss.com/gameaudiogdc | Hàng năm miễn phí |
| **OpenGameArt** | https://opengameart.org | CC0/CC-BY |
| **Kenney Assets** | https://kenney.nl | CC0, quality tốt |

### Nhạc Nền
| Nguồn | Link |
|-------|------|
| **Free Music Archive** | https://freemusicarchive.org |
| **Incompetech** | https://incompetech.com/music |
| **OpenGameArt Music** | https://opengameart.org/content/music |
| **Musopen** | https://musopen.org |

### VFX & Particles
| Nguồn | Link |
|-------|------|
| **Unity Asset Store (free)** | Tìm "Particle effects free" |
| **Kenney Particle Pack** | https://kenney.nl/assets/particle-pack |

### Sprites & UI
| Nguồn | Link |
|-------|------|
| **Kenney UI Pack** | https://kenney.nl/assets/ui-pack |
| **OpenGameArt UI** | https://opengameart.org/content/ui |
| **Game-icons.net** | https://game-icons.net (SVG icons) |
| **Flaticon** | https://flaticon.com (cần attribution) |

### Fonts
| Nguồn | Link |
|-------|------|
| **Google Fonts** | https://fonts.google.com |
| **DaFont** | https://dafont.com (check license) |
| **FontSquirrel** | https://fontsquirrel.com |

---

## Tóm Tắt Số Lượng Asset

| Loại | Số lượng | Ưu tiên |
|------|---------|---------|
| SFX (.wav/.mp3) | 47 file | 🔴 Cao — game thiếu âm thanh kỳ lạ |
| Nhạc nền (.mp3) | 5 file | 🔴 Cao |
| VFX Prefabs (.prefab) | 28 file | 🟡 Trung bình — có thể dùng placeholder |
| Tile Textures (.png) | 8 + 5 overlay | 🔴 Cao — cần để render world |
| UI Sprites (.png) | ~50 file | 🟡 Trung bình — có thể dùng Unity default |
| Archetype Icons | 8 file | 🟡 Trung bình |
| Miracle Icons | 15 file | 🟡 Trung bình |
| Entity Icons | 9 file | 🟡 Trung bình |
| NPC/Org Icons | 11 file | 🟢 Thấp |
| Fonts | 4 file | 🟡 Trung bình |
| Animation Clips | 13 file | 🟢 Thấp — code có fallback |
| ScriptableObjects | 6 file | 🟢 Thấp — optional |

**Tổng:** ~204 asset files

---

*Cập nhật: WorldFaith v1.0 — Tất cả code đã hoàn chỉnh, chỉ cần điền asset*
