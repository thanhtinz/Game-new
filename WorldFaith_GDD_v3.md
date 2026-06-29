# WorldFaith — Game Design Document v3 (Professional Master Version)

> **Core Philosophy:** Players do not control the world. They influence belief, and belief controls the world.

**Version:** 3.0 Master  
**Status:** Full Design  
**Platform:** PC → Console → Mobile  
**Engine:** Unity (Prototype) / Unreal Engine 5 (Full Release)

---

## Mục lục

1. [Executive Summary](#1-executive-summary)
2. [Game Vision & Pillars](#2-game-vision--pillars)
3. [Core Gameplay Loop](#3-core-gameplay-loop)
4. [God System](#4-god-system)
5. [Faith Economy System](#5-faith-economy-system)
6. [Divine Communication System](#6-divine-communication-system)
7. [Miracle & Counter System](#7-miracle--counter-system)
8. [NPC Social Hierarchy](#8-npc-social-hierarchy)
9. [NPC Interaction & Event System](#9-npc-interaction--event-system)
10. [Organizations System](#10-organizations-system)
11. [AI Civilization System](#11-ai-civilization-system)
12. [Religion System](#12-religion-system)
13. [Evolution System](#13-evolution-system)
14. [Multiplayer Design](#14-multiplayer-design)
15. [Win / Lose Conditions](#15-win--lose-conditions)
16. [Game Modes & Scenarios](#16-game-modes--scenarios)
17. [Art Direction](#17-art-direction)
18. [Technical Architecture](#18-technical-architecture)
19. [Prototype Roadmap](#19-prototype-roadmap)
20. [Design Risks & Mitigations](#20-design-risks--mitigations)

---

## 1. Executive Summary

WorldFaith là game **god simulation sandbox multiplayer** — người chơi đóng vai các vị thần cạnh tranh nhau để ảnh hưởng lên các nền văn minh do AI điều khiển.

Điểm khác biệt cốt lõi:
- **Không kiểm soát trực tiếp** — mọi hành động đi qua đức tin và tôn giáo
- **NPC có hệ thống xã hội sâu** — 5 tầng lớp, 6 loại tổ chức, tương tác phức tạp (crime, marriage, betrayal, politics)
- **Emergent storytelling** — mỗi ván game tạo ra câu chuyện riêng chưa ai viết
- **Cyclical world** — thế giới sụp đổ và tái sinh, các thần tồn tại hoặc biến mất

---

## 2. Game Vision & Pillars

### Vision Statement
> Tạo ra một thế giới sống động nơi các nền văn minh phát triển độc lập và các thần cạnh tranh ảnh hưởng thông qua hệ thống niềm tin — không phải kiếm và máu, mà là đức tin và sự quên lãng.

### 5 Design Pillars

| Pillar | Mô tả |
|--------|-------|
| **Emergent Storytelling** | Câu chuyện xuất hiện từ hệ thống, không phải script cố định |
| **Indirect Influence** | Thần không đánh nhau — họ ảnh hưởng lên những kẻ đánh nhau |
| **Dynamic Belief** | Đức tin là tài nguyên, là vũ khí, và là điểm yếu |
| **Living Civilization** | NPC có cuộc sống, mối quan hệ, tham vọng riêng |
| **Cyclical Evolution** | Mọi thứ sụp đổ rồi tái sinh — kể cả các thần |

---

## 3. Core Gameplay Loop

```
┌─────────────────────────────────────────────────────┐
│                  EARLY GAME (Tick 1-100)            │
│  • Thực hiện miracle nhỏ (Dream, Rain, Omen)        │
│  • Xây dựng Trust với Commoners & Servants          │
│  • Sáng lập tôn giáo đầu tiên                       │
└───────────────────────┬─────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│                  MID GAME (Tick 100-600)            │
│  • Mở rộng sang Noble Houses & Royal Court          │
│  • Xây temple, phát lệnh thần cho civ               │
│  • Tôn giáo spread, schism, heresy                  │
│  • Counter miracles của rival gods                  │
│  • Influence qua Crime, Marriage, Politics          │
└───────────────────────┬─────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│                  LATE GAME (Tick 600+)              │
│  • Holy War giữa các tôn giáo                       │
│  • Evolution entities lên Apex                      │
│  • Catastrophic events (Volcano, Apocalypse)        │
│  • Corruption lan rộng trong Noble Houses           │
│  • Royalty sụp đổ hoặc thần thánh hóa              │
└───────────────────────┬─────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│                  REBIRTH CYCLE (Tick 1000)          │
│  • Thần nhiều follower → sống sót sang cycle mới    │
│  • Thần 0 follower → bị lãng quên, biến mất         │
│  • Thế giới tái sinh, civ mới xuất hiện             │
│  • Vòng lặp mới bắt đầu với lịch sử tích lũy       │
└─────────────────────────────────────────────────────┘
```

---

## 4. God System

### 4.1 Archetypes

Mỗi người chơi chọn 1 trong 8 archetype khi bắt đầu. Archetype quyết định passive bonus, miracle tree, và phong cách chơi.

| Archetype | Triết lý | Faith Bonus | Special |
|-----------|---------|------------|---------|
| **Order** ⚖️ | Cân bằng, ổn định | +10% từ temple | Revelation rẻ 20% |
| **Chaos** 🌪️ | Hỗn loạn, ngẫu nhiên | +15% khi civ đang war | Miracle effect x0.8-1.6 ngẫu nhiên |
| **Light** ☀️ | Ánh sáng, chữa lành | +20% khi Trust cao | HealFollower miễn phí |
| **Darkness** 🌑 | Bóng tối, sợ hãi | Faith từ Fear x1.5 | Curse cost -50%, effect x2 |
| **Nature** 🌿 | Thiên nhiên, tiến hóa | +10% từ Forest tiles | Evolution bonus x1.5 trên Sacred |
| **Death** 💀 | Cái chết, tái sinh | +30% khi civ sụp đổ | Thu Faith từ Fear |
| **Knowledge** 📚 | Tri thức, khai sáng | +5% mỗi miracle | DivineVoice -30%, effect x1.5 |
| **War** ⚔️ | Chiến tranh, sức mạnh | +10% khi civ thắng | HolyWar cost -30%, effect x1.5 |

### 4.2 God Resources

| Tài nguyên | Nguồn | Dùng để |
|-----------|-------|---------|
| **Faith** | Followers, temples, rituals | Thực hiện miracles, evolution, commandments |
| **Trust** | Miracle thành công, commandments được nghe | Xác định follower loyalty, spread speed |
| **Fear** | Curse, catastrophe, civ terror | Resource đặc biệt cho Dark archetypes |
| **Devotion** | Thời gian tín đồ theo đạo | Tăng Faith gen efficiency |

### 4.3 God Progression

```
Level 1 (Newborn God)
  ↓ 500 followers
Level 2 (Awakened)     → Mở khóa Tier 2 miracles
  ↓ 2000 followers
Level 3 (Established)  → Mở khóa Divine Communication tất cả
  ↓ 5000 followers
Level 4 (Powerful)     → Mở khóa Tier 3 miracles
  ↓ 10000 followers
Level 5 (Divine)       → Mở khóa Evolution influence, force-evolve Apex
```

---

## 5. Faith Economy System

### 5.1 Faith Generation

Faith tăng mỗi tick từ nhiều nguồn, áp dụng archetype multiplier:

```
Faith/tick = (
  Followers × follower_rate        [config: 0.01 default]
  + Temples × temple_rate          [config: 0.5 per temple]
  + DevotionLevel × follower_count × 0.005
  + Fear × fear_bonus (dark gods)  [config: 0.02]
) × ArchetypeMultiplier
```

### 5.2 NPC Tier ảnh hưởng đến Faith

Không phải follower nào cũng bằng nhau. NPC ở tier cao mang lại nhiều Faith hơn nhưng khó thuyết phục hơn:

| NPC Tier | Faith/tick/follower | Trust Required | Difficulty |
|----------|--------------------|--------------:|------------|
| Commoner | 0.01 (base) | 20 | Dễ |
| Servant | 0.02 | 30 | Dễ |
| Adventurer | 0.05 | 40 | Trung bình |
| Noble | 0.15 | 60 | Khó |
| Royalty | 0.50 | 80 | Rất khó |

> **Chiến lược:** Convert 1 Noble = 15 Commoners. Convert Royalty = 50 Commoners. Nên target tầng cao nhưng cần đầu tư Trust.

### 5.3 Faith Spending

| Hành động | Chi phí | Điều kiện |
|-----------|---------|-----------|
| Miracle Tier 1 | 3-15 Faith | Luôn có thể |
| Miracle Tier 2 | 20-50 Faith | God Level 2+ |
| Miracle Tier 3 | 60-150 Faith | God Level 4+ |
| Commandment | 5-30 Faith | Trust đủ |
| Force Evolve Entity | 50 Faith | God Level 5 |
| Found Religion | 0 Faith | 1 follower+ |
| Build Temple (via commandment) | 25 Faith | Trust ≥ 40 |

---

## 6. Divine Communication System

Thần không nói thẳng với NPC — mọi giao tiếp đều gián tiếp, tốn kém, và có thể bị bỏ qua hoặc hiểu sai.

### 6.1 Ba Cấp Độ Giao Tiếp

#### Dream (Giấc Mơ) — Tier 1
- **Chi phí:** 5 Faith
- **Target:** 1 NPC cụ thể (ưu tiên Adventurer+ để hiệu quả)
- **Hiệu quả:** Trust +5-15 tùy tier NPC
- **Rủi ro:** NPC có thể hiểu sai, spread thông tin sai cho người khác
- **NPC reaction:** Thay đổi hành vi nhẹ (decision weight shift)
- **Ví dụ:** Thần gửi giấc mơ cho Noble → Noble quyết định xây temple thay vì tấn công

#### Divine Voice (Tiếng Thần) — Tier 2
- **Chi phí:** 20 Faith
- **Target:** 1 Civilization hoặc Organization
- **Hiệu quả:** Trust +15, AI behavior change trong 50 ticks
- **Rủi ro:** Nếu Trust < 40, civ ignores hoặc hiểu ngược
- **NPC reaction:** Organization leader thay đổi policy
- **Ví dụ:** Thần ra lệnh Adventure Guild ngừng tấn công temple

#### Revelation (Khải Thị) — Tier 3
- **Chi phí:** 60 Faith
- **Target:** Toàn bộ world hoặc 1 region
- **Hiệu quả:** Mass Trust change, Religion event trigger
- **Rủi ro:** Rival gods có thể counter với Revelation ngược lại
- **NPC reaction:** Kingdom-wide policy change, religion schism có thể xảy ra
- **Ví dụ:** Thần tuyên bố một Noble House là "Chosen People" → cả kingdom follow religion đó

### 6.2 Commandment System (8 Loại Lệnh)

Phát lệnh cụ thể đến Civilization. Kết quả phụ thuộc vào Trust level của NPC nhận lệnh:

| Commandment | Faith | Trust cần | Hiệu quả khi nghe |
|------------|-------|----------|-------------------|
| ExpandTerritory | 20 | 40 | Military +15, trigger expansion |
| BuildTemple | 25 | 40 | Economy +10, Trust +10 |
| SpreadFaith | 15 | 40 | Religion spread speed +50% |
| MakeWar | 30 | 70 | IsAtWar = true, Military +20 |
| MakePeace | 10 | 40 | War ends, ceasefire |
| FocusEconomy | 10 | 40 | Economy +25 |
| FocusMilitary | 15 | 40 | Military +25 |
| Worship | 5 | 30 | Devotion +15 |

> **Nếu Trust không đủ:** NPC phớt lờ lệnh, Trust -5 (cảm thấy bị áp đặt).

---

## 7. Miracle & Counter System

### 7.1 Miracle Tiers

#### Tier 1 — Low Cost (3-15 Faith)
| Miracle | Faith | Hiệu quả |
|---------|-------|---------|
| Omen (Điềm Báo) | 3 | Trust +2, NPC nhận được "sign from above" |
| Dream | 5 | Trust target NPC +5-15 |
| Rain | 10 | Fertility tiles +15%, harvest bonus |
| HealFollower | 8 | Recovery, Trust +8; **Light god: Free** |
| BlessHarvest | 15 | Economy +20, Population +5% |

#### Tier 2 — Medium Cost (20-50 Faith)
| Miracle | Faith | Hiệu quả |
|---------|-------|---------|
| DivineVoice | 20 | Trust +15, AI behavior change |
| Curse | 25 | Economy -15, Trust -10; **Darkness: -50% cost, x2 dmg** |
| Storm | 30 | Military -10 nearby civs |
| Earthquake | 40 | Structures destroyed, population -10% |
| Portal | 50 | Trade routes +50%, cross-region movement |

#### Tier 3 — High Cost (60-150 Faith)
| Miracle | Faith | Hiệu quả |
|---------|-------|---------|
| Revelation | 60 | Mass Trust change, region-wide |
| DivineBeastCreation | 80 | Spawn DivineBeast entity |
| Volcano | 100 | Area devastation, tiles → Volcano type |
| DemonInvasion | 120 | Spawn wave of Monsters attacking civs |
| HolyWar | 150 | Religion war trigger; **War god: -30% cost, x1.5 effect** |

### 7.2 Counter System

Khi rival god dùng miracle, window **N giây** (configurable) để phản phép:

```
Rival dùng Miracle
        ↓
Counter UI xuất hiện với countdown bar
        ↓
Bạn chọn Counter Miracle (phải unlock + đủ Faith)
        ↓
Nếu counter thành công → Miracle bị hủy / giảm hiệu quả
Nếu không counter → Miracle có hiệu lực đầy đủ
```

**Counter Strategy Examples:**
- Curse → Counter bằng BlessHarvest (neutralize economy damage)
- Storm → Counter bằng Rain (cancel weather effect)
- HolyWar → Counter bằng Revelation (declare peace)
- DemonInvasion → Counter bằng DivineBeastCreation (defend)

---

## 8. NPC Social Hierarchy

Đây là hệ thống mới nhất và sâu nhất trong v3. Mỗi NPC trong world có **tier xã hội**, **tổ chức thuộc về**, và **mạng lưới quan hệ**.

### 8.1 Tier Hierarchy

```
         ╔══════════════╗
         ║  TIER 5      ║  Royalty
         ║  ROYALTY     ║  (1-5 NPC per Kingdom)
         ╠══════════════╣
         ║  TIER 4      ║  Nobles
         ║  NOBLES      ║  (5-20 NPC per Kingdom)
         ╠══════════════╣
         ║  TIER 3      ║  Adventurers
         ║  ADVENTURERS ║  (20-50 NPC)
         ╠══════════════╣
         ║  TIER 2      ║  Servants
         ║  SERVANTS    ║  (50-200 NPC)
         ╠══════════════╣
         ║  TIER 1      ║  Commoners
         ║  COMMONERS   ║  (Majority population)
         ╚══════════════╝
```

### 8.2 Tier 1 — Commoners (Thường Dân)

**Đặc điểm:**
- Chiếm đại đa số dân số
- Faith generation thấp nhưng stable
- Dễ convert vào religion
- Bị ảnh hưởng mạnh bởi crop failure, famine, disease

**AI Behaviors:**
- Nếu harvest fail 3 lần liên tiếp → Devotion giảm 20%/turn
- Nếu Noble bị scandal → Commoners mất trust vào kingdom
- Nếu Temple bị phá → ngay lập tức petition lên Noble
- Marriage giữa Commoners ảnh hưởng đến population growth

**Interaction với God:**
- Nhận Dream dễ nhất (trust threshold thấp)
- Spread rumor sau khi nhận Dream → lan thành oral tradition
- Nếu cả làng nhận cùng omen → tự lập cult nhỏ

### 8.3 Tier 2 — Servants (Người Hầu)

**Đặc điểm:**
- Làm việc trong Noble Houses, Royal Court, Religious Institutions
- Có access đến thông tin nội bộ
- Có thể là spy, blackmailer, hoặc loyal servant
- Quan hệ trực tiếp với Noble chủ nhân

**AI Behaviors:**
- Overheard conversations → có thể leak information
- Loyal Servant: Trust với Noble cao → resist god influence
- Disloyal Servant: Trust với Noble thấp → dễ bị corrupt bởi rival god
- Blackmail system: Servant biết bí mật Noble → extortion event

**Interaction với God:**
- Trung gian giữa Commoners và Nobles
- Convert 1 Head Servant → easier Noble access
- Servant scandal có thể destabilize Noble House

### 8.4 Tier 3 — Adventurers (Mạo Hiểm Gia)

**Đặc điểm:**
- Thuộc Adventure Guild (see Organizations)
- Mobile — di chuyển giữa các kingdoms
- Có thể là Warriors, Mages, Rogues, Priests
- Làm quest cho Nobles, explore dungeons

**AI Behaviors:**
- Khi Adventure Guild nhận quest từ Noble → party lên đường
- Encounter với Evolution Entities → có thể die, hoặc become Champion
- Religion spread qua Adventurers khi họ travel
- Betrayal: Adventurer được trả tiền để kill người thuê

**Interaction với God:**
- Adventurer trở thành **Champion** nếu được god influence đủ
- Champion là đặc vụ trực tiếp của god trên mặt đất
- Divine evolution path: Adventurer → Hero → Saint / Demon Lord

**Champion System:**
```
Adventurer (Trust ≥ 70 với God, God Level 3+)
         ↓ God chọn Champion
HumanHero (Divine ability unlocked)
         ↓ 150 evolution pts
Saint (nếu God là Light/Order/Knowledge)
hoặc FallenDemonLord (nếu God là Darkness/Chaos/Death)
```

### 8.5 Tier 4 — Nobles (Quý Tộc)

**Đặc điểm:**
- Leaders của Noble Houses
- Control territory, armies, taxes
- Có thể be King's ally hoặc rival
- Marriage hệ trọng — merge families, seal alliances

**AI Behaviors — Political:**
- **Alliance:** Noble A + Noble B marriage → combined military
- **Betrayal:** Noble giả vờ loyal → assassinate king
- **Corruption:** Noble nhận bribe từ rival god → change religion policy
- **Ambition:** Noble với Military > 80 có 30% chance mỗi 100 ticks thử depose king

**AI Behaviors — Religious:**
- Noble có personal religion (có thể khác official kingdom religion)
- Noble với devotion cao → fund temple construction
- Noble bị curse bởi god → lose public support
- Religious Noble có thể start crusade independently

**Interaction với God:**
- Convert Noble → thay đổi kingdom policy
- Noble House trở thành base của religion nếu Noble Ruling Religion = god's religion
- Noble betrayal event: God có thể whisper Noble, plant idea lật đổ king

### 8.6 Tier 5 — Royalty (Hoàng Tộc)

**Đặc điểm:**
- King, Queen, Crown Prince, Royal Advisors
- Control entire kingdom's direction
- Hardest to influence, highest Faith reward
- Death/betrayal triggers kingdom-wide crisis

**AI Behaviors — Governance:**
- **Decree:** King issue decree → all civ AI behavior shifts
- **Succession Crisis:** King dies without heir → civil war risk
- **Divine Right:** King claims divine mandate → bonus stability, harder for rival gods
- **Exile:** King exile Noble → lose ally, gain enemy

**AI Behaviors — Religion:**
- King adopts religion → entire kingdom Religion layer changes
- King declares Religious War → all armies mobilize
- King martyred by rival → Followers surge 50% (martyrdom bonus)
- Royal Wedding with foreign kingdom → religion mixing event

**Interaction với God:**
- Hardest to reach directly — must go through Noble chain
- Path: God → Trust Noble → Noble influences Royal Advisor → Advisor whispers King
- **Divine King** event: King personally devout → bonus Faith, Temple in every city
- **Apostate King** event: King abandons religion → mass schism, rival gods gain opening

---

## 9. NPC Interaction & Event System

NPC không chỉ là số liệu — họ có cuộc sống, và cuộc sống đó tạo ra events ảnh hưởng đến Faith.

### 9.1 Crime & Corruption Events

**Types:**

| Event | Trigger | Faith Impact | Kingdom Impact |
|-------|---------|-------------|----------------|
| **Theft** | Commoner economy < 20 | -2 Trust nếu god không can thiệp | Crime rate +5% |
| **Corruption Scandal** | Noble bị bribe | -10 Trust nếu religion-linked | Noble influence -20% |
| **Assassination** | Noble Ambition > 80 + target identified | Massive Trust swing | Succession crisis |
| **Heresy Trial** | Religion finds hidden cult | +5 Devotion cho orthodox followers | Execution event |
| **Extortion** | Servant với bí mật Noble | Economy drain | Social instability |
| **Tax Evasion** | Multiple Nobles collude | Kingdom economy -15% | King-Noble tension |

**God Interaction với Crime:**
- God nhận notification khi crime liên quan đến followers
- Có thể Miracle để punish criminal (Curse) hoặc protect victim (Heal)
- Nếu god ignores → followers mất trust ("God didn't help me")
- Dark gods có thể **encourage** crime → Fear resource gain

### 9.2 Accidents & Natural Events

| Event | Frequency | Faith Impact |
|-------|-----------|-------------|
| **Crop Failure** | 5% per tick khi fertile < 0.3 | -5 Devotion/tick trong region |
| **Disease Outbreak** | 3% per 50 ticks | Population -10%, Trust khẩn cấp cao |
| **Building Collapse** | 1% per 100 ticks | Trust -3, Economy -5 |
| **Flood** | After heavy Rain miracle | May damage Temples |
| **Trade Route Robbery** | 10% mỗi khi Adventurer quests | Economy -8 |

**God Interaction:**
- Crop Failure → god có thể Miracle: BlessHarvest → followers grateful
- Disease → god Heal → massive Trust gain → religion spread opportunity
- Accidents với no god response → atheism spread ("God doesn't care")

### 9.3 Luck System

Không phải mọi event đều có nguyên nhân rõ ràng. Có hệ số **Luck** (0-100) ảnh hưởng:

```
NPC Lucky Event (Luck roll 80-100):
  - Find treasure → Economy gain
  - Survive battle against odds → becomes local hero
  - Unexpected crop abundance → Gratitude, possible religion attribution

NPC Unlucky Event (Luck roll 0-20):
  - Accident → may blame god for "abandonment"
  - Business failure → turn to prayer or turn away
  - Child dies → crisis of faith (Trust -20) or deeper faith (Trust +30)
```

**God Influence on Luck:**
- High Devotion region → Lucky events more frequent (+10 to rolls)
- God sends Omen before major event → NPC feels "forewarned"
- Chaos god có thể amplify luck variance (both extremes more likely)

### 9.4 Betrayal System

Betrayal là event quan trọng nhất — thay đổi kingdom balance overnight.

**Types of Betrayal:**

**Noble Betrayal:**
- Trigger: Noble Ambition ≥ 75, Military ≥ 60, Personal Religion ≠ Official Religion
- Process:
  1. Noble bí mật contact rival king
  2. Exchange information / open gates
  3. Civil war trigger hoặc assassination
- God Opportunity: Both sides may pray for divine help → Faith gain from winner

**Servant Betrayal:**
- Trigger: Servant Loyalty < 30, bribed by rival faction
- Types: Poison king, forge documents, leak troop positions
- Impact: Noble House damaged, Trust shake

**Religious Betrayal:**
- Trigger: Priest-Noble conflict
- A Noble secretly funds rival religion
- Exposes: Public scandal, religion schism, heresy trial

**Champion Betrayal (special):**
- God's Champion turns against god after seeing darkness
- Trigger: God uses Champion for acts against Champion's moral code
- Result: Champion becomes enemy agent, spreads anti-god propaganda

### 9.5 Marriage & Politics System

Marriage không phải chỉ là roleplay — nó thay đổi kingdom structure:

**Marriage Types:**

| Type | Parties | Effect |
|------|---------|--------|
| **Political Marriage** | Noble ↔ Noble | Alliance, territory merge possibility |
| **Royal Marriage** | Royalty ↔ Royalty | Kingdom alliance, religion mixing |
| **Forbidden Marriage** | Commoner ↔ Noble | Social scandal, Commoner uprising risk |
| **Religious Marriage** | High Priest ↔ Royalty | Church-State union, massive devotion boost |
| **Arranged Marriage** | Any tier | May succeed or create resentment |

**Faith Impact của Marriage:**
- Royal wedding uses god's religion ceremony → god Faith +50
- Cross-religion marriage → both religions spread, possible conflict
- Marriage fails (divorce/death) → instability, rival gods exploit

**Succession & Inheritance:**
- Heir religion = weighted average of parents + kingdom official
- Noble heir personality inherits from parents (Aggressive + Logical = Calculated)
- God có thể influence unborn heir via Dream to pregnant mother (rare, cost 30 Faith)

### 9.6 Political Events

| Event | Trigger | God Opportunity |
|-------|---------|----------------|
| **Election** (Republic civ) | Every 50 ticks | Support candidate via Noble influence |
| **Rebellion** | King approval < 20 | Encourage or suppress |
| **Alliance Formation** | 2 kingdoms mutual threat | Broker via Revelation |
| **Embargo** | Kingdom dispute | Break via Portal miracle |
| **Tax Reform** | Economy crisis | Influence via Noble commandment |
| **Coronation** | New king | First impression → Trust opportunity |

---

## 10. Organizations System

6 loại tổ chức, mỗi loại có power, influence, và relationship với god.

### 10.1 Kingdom (Vương Quốc)

**Structure:**
```
King/Queen (Tier 5)
    ↓
Royal Council (Tier 4 Nobles)
    ↓
Noble Houses (Tier 4)
    ↓
Local Lords (Tier 4 minor)
    ↓
Commoners (Tier 1-2)
```

**Resources:** Tax revenue, Military, Territory  
**Religion Relationship:** Kingdom adopts Official Religion — affects 80% of NPCs  
**God Interaction:**
- Convert King → instant mass conversion (risky, high-cost path)
- Convert Council majority → influence king indirectly (safer)
- Kingdom can declare religion as "state religion" → Temple in every city

**Kingdom States:**
- Stable: Normal operation
- Expanding: Military aggressive, send Adventurers
- Declining: Economy problems, Nobles restless
- Collapsing: King < 10 approval, succession crisis imminent
- Fallen: Destroyed, territory open for new civ

### 10.2 Royal Court (Triều Đình)

**Structure:** King + Royal Advisors + Court Officials (all Tier 4-5)

**Special Roles:**
- **Chancellor:** Economy policy (controls trade, taxes)
- **General:** Military policy (war declarations, defense)
- **High Priest:** Religion policy (official religion, heresy trials)
- **Spymaster:** Intelligence (knows about betrayals before they happen)

**God Interaction — Critical Path:**
```
God wants to influence King
    ↓
Must first influence Spymaster (knows all)
    OR High Priest (direct religion channel)
    ↓
They whisper to King → King's decision shifts
```

**Court Intrigue Events:**
- Advisors compete for King's favor
- Rival gods each try to own different advisors
- When two god-influenced advisors clash → policy deadlock → civ weakens

### 10.3 Noble Houses (Gia Tộc Quý Tộc)

**Typically 3-8 Noble Houses per Kingdom**

**Each House Has:**
- **Head of House** (Tier 4 NPC)
- **Family Members** (Tier 4 minor + Tier 2 Servants)
- **Territory** (tiles they control)
- **House Religion** (may differ from kingdom official)
- **House Ambition** (0-100, affects betrayal chance)
- **House Economy** (independent from kingdom treasury)

**House Relationships:**
```
Allied Houses ←→ Rival Houses
     ↕                  ↕
Marriages          Assassination
Alliances          Political warfare
Joint ventures     Land disputes
```

**God Interaction:**
- Each Noble House is a separate influence target
- Convert Head of House → entire house shifts religion
- Whisper rivalry into two allied houses → they become rivals (divide and conquer)
- House with God's religion + Military > 70 = natural crusade initiator

### 10.4 Adventure Guild (Hội Mạo Hiểm)

**Unique organization** — không gắn với kingdom nào, hoạt động cross-kingdom.

**Structure:**
- **Guild Master** (experienced Tier 3 → elevated to Tier 4)
- **Veteran Members** (Tier 3 Adventurers)
- **Initiates** (Tier 2-3 new members)

**Guild Functions:**
- Accept quests from Nobles
- Explore dungeons → encounter Evolution Entities
- Spread news between kingdoms (gossip, information broker)
- Mercenary for hire in wars

**God Interaction — Unique:**
- Guild Master is BEST target for god influence (travels everywhere)
- Convert Guild Master → Guild becomes religious pilgrimage organization
- Adventurers can become Champions (see Tier 3 above)
- Guild can be hired to suppress rival religion's temples

**Guild Events:**
- Lost expedition → death of veteran adventurers → Trust crisis
- Monster slaying → member becomes hero → Faith boost for witnessing Commoners
- Discovery of ancient ruins → secret knowledge → may destabilize existing religion

### 10.5 Religious Institutions (Thiết Chế Tôn Giáo)

**Types:**
- **Temple** (local, 1 tile influence)
- **Cathedral** (city-wide, 3 tile influence)
- **Holy Site** (region-wide, Sacred tile)
- **Religious Order** (mobile, like Adventure Guild for religion)

**Hierarchy:**
```
High Priest / Grand Patriarch (Tier 4)
    ↓
Senior Priests (Tier 3)
    ↓
Local Priests (Tier 2)
    ↓
Temple Workers (Tier 1-2 Servants)
```

**God Interaction:**
- Religious Institution cho god direct access to followers
- High Priest is "mouth of god" to kingdom — High Priest's word = god's word
- Heresy Trial: High Priest can purge rival god's influence from city
- Inquisition: Aggressive anti-rival-god campaign (military + religious)

**Institution Events:**
- **Schism:** Priests disagree on doctrine → splits into 2 branches
- **Corruption:** Priest accepts bribe → public scandal → Devotion -30%
- **Martyrdom:** Priest killed defending temple → Faith +50% in region
- **Pilgrimage:** Major religious event → Commoners migrate → Faith spread

### 10.6 Underground / Criminal Organizations (Tổ Chức Ngầm)

**Types:**
- **Thieves Guild** (economy-focused crime)
- **Assassins Brotherhood** (targeted violence)
- **Cult Network** (hidden religion, Heresy)
- **Rebel Cell** (anti-government)

**Structure:** Hidden, cell-based, no public face

**God Interaction:**
- Dark gods (Chaos/Darkness/Death) can cultivate criminal orgs
- Criminal org → spread Fear → Fear resource for dark god
- Cult Network is how hidden religions (Heresy) propagate
- Assassins can be used to remove rival High Priest

**Criminal Org Events:**
- **Exposed:** Kingdom discovers org → mass arrest → Commoner unrest
- **Assassination Success:** Target killed → power vacuum
- **Cult Revealed:** Hidden religion exposed → choice: fight or convert
- **Gang War:** Two orgs compete → collateral damage to Commoners

---

## 11. AI Civilization System

### 11.1 Civilization Lifecycle

```
Tribal → Kingdom → Empire → (Declining) → Fallen → (Rebirth as new Tribal)
```

**State Thresholds:**
| State | Population | Economy | Note |
|-------|-----------|---------|------|
| Tribal | 0-500 | < 50 | No Noble Houses yet |
| Kingdom | 500-5000 | 50-150 | Noble Houses form |
| Empire | 5000+ | 150+ | Multiple kingdoms unified |
| Declining | Any | < 20 | Unstable, Noble betrayal likely |
| Fallen | 0 | — | Territory open |

### 11.2 AI Personality Types

| Personality | War | Economy | Religion | Special |
|------------|-----|---------|---------|---------|
| **Aggressive** | High | Medium | Weaponizes | Attacks every 10 ticks if strong |
| **Defensive** | Low | High | Practical | Builds walls, economy focus |
| **Fanatic** | Religion-driven | Medium | Central | Crusade if devotion > 70 |
| **Logical** | Balanced | Balanced | Pragmatic | War only if 2:1 advantage |
| **Opportunistic** | Target weak | Varies | Shopping | Switches religion to strongest god |

### 11.3 Economy Simulation

```
Income:
  + Farming (Fertility × Population × 0.001)
  + Trade (active Portal miracle bonus)
  + Taxes from Nobles
  + Temple donations (Devotion × TempleCount × 0.5)

Expenditure:
  - Military upkeep (Military × 0.1/tick)
  - Noble salaries (NobleCount × 2/tick)
  - Temple maintenance (TempleCount × 0.5/tick)
  - Event costs (disasters, wars)
```

### 11.4 War System

Civilizations go to war when:
- Military advantage > 1.5x opponent
- Religious mandate (Crusade trigger)
- Noble House with high Ambition seizes control
- God commandment (MakeWar + Trust ≥ 70)
- Retaliation for attack

**War Outcomes:**
- Winner: Territory gain, population gain, Economy gain
- Loser: Territory lost, population -20%, Noble betrayal risk +30%
- Stalemate: Both lose economy, Noble unrest rises
- God opportunity: Winning or losing side prays for help → Faith spike

---

## 12. Religion System

### 12.1 Religion Foundation

A religion needs:
1. A god (founder)
2. At least 1 follower
3. A name

Optional at founding:
- Hidden (cult) vs Public
- Doctrine: Pacifist / Militant / Exclusive / Inclusive

### 12.2 Religion Spread Mechanics

```
SpreadChance per 5 ticks = 
  DevotionLevel × 0.3
  + TempleCount × 0.05
  + log10(FollowerCount) × 0.02
  - RivalPresence × 0.1  (if rival religion in same civ)

NPC factors:
  + Fanatic personality: +0.2
  + Post-disaster: +0.15 (seeking meaning)
  + Post-miracle: +0.25 (saw divine power)
  - Logical personality: -0.1 (requires evidence)
  - After corruption scandal: -0.2 (distrust)
```

### 12.3 Religion Dynamics

**Schism (tách đôi):**
- Trigger: Devotion < 35%, Followers > 500, ticks ≥ 50
- 25% xác suất
- Result: 1/3 followers tách ra, create reformed branch
- Schism religion giữ god nhưng có doctrinal differences

**Heresy (tà giáo):**
- Trigger: 8% chance per 80 ticks, followers > 200
- Hidden cult formed (IsHidden = true)
- Ultra-high devotion (0.8) but small
- If exposed: Heresy trial, execution events

**Crusade:**
- Trigger: Devotion > 0.7, Military > 60, rival religion exists
- Civ attacks rival religion civ
- God receives Faith boost during crusade (followers fighting for faith)

**Conversion via NPC Tiers:**
- Commoner converts: Organic, slow, following neighbors
- Adventurer converts: Active spreader when they travel
- Noble converts: Semi-public, shifts house policy
- High Priest of other religion converts: Massive schism event
- King converts: Kingdom-wide policy change

---

## 13. Evolution System

### 13.1 Three Evolution Paths

```
CREATURE PATH:
WildAnimal (100pts) → DivineBeast (500pts) → CelestialGuardian [MAX]

HERO PATH:
HumanHero (150pts) → Saint (600pts) → FallenDemonLord [MAX]

MONSTER PATH:
Monster (120pts) → Titan (450pts) → ApocalypticEntity [MAX]
```

### 13.2 Evolution Points Gain

```
Points/tick = BaseGain × GodInfluenceBonus × TileBonus

BaseGain by Stage:
  WildAnimal/Monster: 1-3/tick
  HumanHero: 2-5/tick
  DivineBeast/Saint/Titan: 1-2/tick (slower at higher stages)

GodInfluenceBonus:
  God's Faith > 500 → ×1.5
  God's Faith > 800 → ×2.0

TileBonus:
  Sacred Tile → ×1.5 (configurable)
  Forest (for WildAnimal) → ×1.2
  Mountain (for Monster) → ×1.2
```

### 13.3 Apex Entity Effects

| Apex Entity | World Effect | Radius |
|------------|-------------|--------|
| CelestialGuardian | All ally civs: Military +20, Faith wave | 15 tiles |
| FallenDemonLord | Fear spread, civs lose hope | 15 tiles |
| ApocalypticEntity | Disaster trigger, panic, population flee | 20 tiles |

### 13.4 NPC Interaction với Entities

- **Commoners witness DivineBeast** → shrine building event → local cult forms
- **Adventurers fight Monster** → Quest chain event (kill or tame)
- **King claims Saint as advisor** → Kingdom becomes theocracy
- **ApocalypticEntity reaches Kingdom** → Nobility flees, Commoners pray

---

## 14. Multiplayer Design

### 14.1 Player Count & Roles

| Players | Recommended Scenario | Playtime |
|---------|---------------------|---------|
| 2 | TheLastLight, EvolutionRace | 30-45 min |
| 3-4 | Standard, ReligionWars | 45-90 min |
| 5-8 | Apocalypse, Competitive | 60-120 min |

### 14.2 Indirect PvP

**Không có combat trực tiếp giữa gods.** Tất cả đối đầu đi qua thế giới:

| Action | Effect on Rival |
|--------|----------------|
| BlessHarvest their target | Your religion spreads faster |
| Curse their Noble | Noble loses faith in rival god |
| Revelation countering theirs | Mass confusion, mixed signals |
| Sponsor Schism in rival religion | Weaken their follower base |
| Commandment rival civ's king to make peace | Block their Crusade |

### 14.3 Alliance System (Optional)

Gods có thể tạm thời liên minh:
- Share Faith generation bonus
- No counter-miracle against ally
- Coordinate Crusades
- Alliance breaks when one god reaches 60% followers

### 14.4 Observer Mode

Người chơi bị eliminate (0 followers) có thể:
- Watch as Observer
- See all NPC data (God view disabled)
- Vote on world events (flavor only, no mechanical impact)

---

## 15. Win / Lose Conditions

### 15.1 Standard Win Conditions

| Condition | How to Win |
|-----------|-----------|
| **Last Surviving God** | Only god with followers after Rebirth |
| **Faith Dominance** | Most followers after N cycles |
| **Religion Control** | Your religion > 70% of world followers |
| **Dynasty Control** | Your religion controls 3+ kingdoms simultaneously |

### 15.2 Scenario-Specific Conditions

| Scenario | Win Condition |
|----------|--------------|
| TheLastLight | Light god survives 3 Rebirth cycles |
| ReligionWars | First to 70% world followers |
| EvolutionRace | First to Apex evolution |
| FaithCrisis | Survive 3 cycles with Faith × 0.2 |
| Apocalypse | Survive 2 cycles with Monster × 3 |

### 15.3 Loss Conditions

- **0 followers** after Rebirth tick → eliminated
- **All temples destroyed** + 0 followers → eliminated
- **Name Forgotten:** If followers = 0 for 200 consecutive ticks → permanent elimination (even if some followers return later)

---

## 16. Game Modes & Scenarios

### 16.1 Sandbox Mode

- No win condition
- Infinite cycles
- All systems active
- Best for learning systems and emergent storytelling

### 16.2 Survival Mode

- Solo vs AI gods (2-4 AI opponents)
- Cycle limit: 5
- Difficulty: Easy / Normal / Hard / God-Tier
- Achievement system active

### 16.3 Competitive Mode (PvP)

- Ranked matches
- ELO-based matchmaking
- Fixed scenario rotates (monthly season)
- No alliance allowed

### 16.4 Scenario Mode

| Scenario | Description | Special Rules |
|----------|-------------|--------------|
| **Standard** | Classic competitive | All normal |
| **TheLastLight** | 1 Light god vs all | Light: win if survive 3 cycles |
| **ReligionWars** | Race to 70% | Religion-only win |
| **EvolutionRace** | Apex race | First Apex wins immediately |
| **FaithCrisis** | Slow faith | Faith gen × 0.2 |
| **Apocalypse** | Monster surge | Monster power × 3 |

---

## 17. Art Direction

### 17.1 Visual Style

**Primary style:** Low-poly stylized (inspired by WorldBox, Dwarf Fortress)
- World view: Isometric top-down
- NPC: Tiny figures visible on zoom in
- Tiles: Clearly readable biome colors
- Gods: Invisible (represented by particle effects and auras)

### 17.2 Visual Identity per System

| System | Visual Language |
|--------|----------------|
| Faith | Golden particles rising from buildings |
| Miracles | Dramatic VFX appropriate to type |
| Religion Spread | Color wash spreading tile by tile |
| Evolution | Transformation glow effect |
| NPC Tier | Size + color of NPC dot |
| Crime | Dark aura on involved tiles |
| Marriage | Heart particle between NPCs |
| Betrayal | Red flash, then dark shroud |

### 17.3 UI Philosophy

- **Minimal overlay** during world view
- **On hover:** NPC info popup with tier, relationships, religion
- **God view:** Overlay shows Faith/Fear heat map
- **Mobile:** Bottom tab navigation, large touch targets
- **Event log:** Color-coded by system (gold=faith, red=crime, purple=religion...)

---

## 18. Technical Architecture

### 18.1 Stack

| Layer | Technology |
|-------|-----------|
| Server | ASP.NET Core 8, SignalR |
| Database | MongoDB (persistent), Redis (real-time) |
| Client | Unity 2022.3 LTS |
| Admin | Next.js 14 |
| Auth | JWT + Refresh Token |
| Transport | WebSocket (SignalR) |

### 18.2 Simulation Architecture

```
WorldSimulationLoop (500ms/tick)
    ├── FaithService.GenerateFaithTick()
    ├── CivilizationSimulationService.Tick()
    │       ├── AI Personality behaviors
    │       ├── Economic simulation
    │       ├── Political event checks
    │       └── NPC Tier interactions
    ├── ReligionService.Tick()           [every 5 ticks]
    │       ├── Spread mechanics
    │       ├── Schism/Heresy checks
    │       └── Crusade evaluation
    ├── EvolutionService.Tick()          [every 3 ticks]
    │       ├── Point accumulation
    │       ├── Stage transitions
    │       └── Apex effects
    ├── NPCInteractionService.Tick()     [every 10 ticks]  ← NEW v3
    │       ├── Crime events
    │       ├── Marriage events
    │       ├── Betrayal evaluations
    │       ├── Political events
    │       └── Luck rolls
    ├── OrganizationService.Tick()       [every 20 ticks]  ← NEW v3
    │       ├── Noble House dynamics
    │       ├── Adventure Guild quests
    │       ├── Criminal Org operations
    │       └── Court intrigue
    └── ScenarioController.CheckWin()
```

### 18.3 NPC Data Model (New v3)

```csharp
public class NpcDocument
{
    public string Id { get; set; }
    public string WorldId { get; set; }
    public string CivilizationId { get; set; }
    public NpcTier Tier { get; set; }           // 1-5
    public string OrganizationId { get; set; }
    public NpcPersonality Personality { get; set; }
    
    // Stats
    public float Loyalty { get; set; }          // to their org/noble
    public float Ambition { get; set; }         // chance to betray
    public float Piety { get; set; }            // religious devotion
    public float Wealth { get; set; }
    
    // Relationships
    public List<NpcRelationship> Relationships { get; set; }  // spouse, ally, rival
    public string? SpouseId { get; set; }
    public List<string> ChildrenIds { get; set; }
    
    // God interaction
    public string? GodInfluenceId { get; set; }
    public float GodTrustLevel { get; set; }
    public List<string> ReceivedDreams { get; set; }
    
    // State
    public NpcState State { get; set; }         // Alive, Dead, Exiled
    public string? KnownSecretAboutNpcId { get; set; }  // for blackmail
}

public class NpcRelationship
{
    public string NpcId { get; set; }
    public RelationshipType Type { get; set; }  // Allied, Rival, Spouse, Parent
    public float Strength { get; set; }         // 0-100
}
```

### 18.4 Performance Considerations

| Component | Max count | Update frequency | Strategy |
|-----------|-----------|-----------------|---------|
| NPC (Tier 1) | 10,000 | Aggregated | Group simulation, not individual |
| NPC (Tier 2-3) | 500 | Every 20 ticks | Periodic |
| NPC (Tier 4-5) | 50 | Every tick | Full simulation |
| Organizations | 30 | Every 20 ticks | Event-driven |
| Tiles | 4,096 (64×64) | On change | Dirty flag |

> **Key insight:** Simulate Tier 1-2 as population statistics. Simulate Tier 4-5 as named individuals. Tier 3 is intermediate (named but lightweight).

### 18.5 Multiplayer Sync

- Authoritative server: All simulation runs server-side
- Client receives: WorldTickEvent (deltas only, not full state)
- God actions: Client → Server → Validate → Apply → Broadcast
- Late join: Full WorldState snapshot on connect

---

## 19. Prototype Roadmap

### Phase 1 — Foundation (8-12 weeks)
- [x] Procedural world generation (Perlin Noise, 8 biomes)
- [x] 2 playable gods with Faith system
- [x] Basic Tier 1 (Commoner) simulation
- [x] 5 Tier 1 miracles
- [x] Basic religion spread
- [x] Unity client + ASP.NET Core server
- [x] SignalR realtime sync

### Phase 2 — Core Systems (12-16 weeks)
- [x] All 15 miracles
- [x] Counter system
- [x] Full religion system (schism, heresy, crusade)
- [x] Evolution system (9 stages)
- [x] AI civ personalities (5 types)
- [x] Multiplayer (2-8 gods)
- [x] Rebirth cycle

### Phase 3 — NPC Depth (16-20 weeks) ← v3 NEW
- [ ] NPC Tier 1-5 implementation
- [ ] Noble Houses + Royal Court simulation
- [ ] Crime & Corruption events
- [ ] Marriage & succession system
- [ ] Adventure Guild quests
- [ ] Betrayal system
- [ ] Organization dynamics
- [ ] Champion system (Adventurer → Hero)

### Phase 4 — Polish & Balance (8-12 weeks)
- [x] Admin Panel + Balance Config (45 params)
- [x] Leaderboard ELO
- [x] Tutorial system
- [x] Audio Manager + VFX system
- [ ] Full sound effects library
- [ ] VFX prefabs for all events
- [ ] Mobile optimization
- [ ] Performance profiling (10,000 NPC simulation)

### Phase 5 — Content Expansion (ongoing)
- [ ] New scenarios
- [ ] New archetypes (Trickster, Nature expansion)
- [ ] Map editor
- [ ] Mod support
- [ ] Achievement system
- [ ] Seasonal events

---

## 20. Design Risks & Mitigations

| Risk | Severity | Mitigation |
|------|---------|-----------|
| **AI complexity doesn't scale** | High | Tier-based simulation: Tier 1 = stats, Tier 4-5 = named NPCs |
| **God balance issues** | High | BalanceConfigService (45 params, runtime tunable) |
| **Emergent systems create unfun outcomes** | Medium | Sandbox mode lets players opt out of competitive |
| **Multiplayer sync lag** | Medium | Server authoritative, delta compression |
| **NPC social system too complex** | Medium | Phase 3 is separate — Phase 1-2 work without it |
| **Performance with large worlds** | Medium | Dirty-flag tiles, aggregate Tier 1 NPCs |
| **Tutorial inadequate for complexity** | Medium | 9-step tutorial + sandbox mode as learning space |
| **Mobile UX for complex systems** | Low | Bottom tab navigation, long-press context menus |
| **Player isolation (indirect PvP)** | Low | Chat system, event log, visible rival god actions |

---

## Appendix A — Quick Reference

### God Resources at a Glance
```
Faith    = Primary resource for all divine actions
Trust    = Relationship with followers (affects spread, commandments)
Fear     = Dark gods alternative resource (from disasters, curses)
Devotion = Long-term follower depth (affects efficiency)
```

### NPC Tier Quick Reference
```
Tier 1 Commoner    = 0.01 Faith/tick, Trust req 20, Easy
Tier 2 Servant     = 0.02 Faith/tick, Trust req 30, Easy
Tier 3 Adventurer  = 0.05 Faith/tick, Trust req 40, Medium → can become Champion
Tier 4 Noble       = 0.15 Faith/tick, Trust req 60, Hard → controls territory
Tier 5 Royalty     = 0.50 Faith/tick, Trust req 80, Very hard → controls kingdom
```

### Organization Quick Reference
```
Kingdom            = Territory, Military, Tax
Royal Court        = Policy, Decree, Royal decisons
Noble Houses       = Land, Independent armies, House religion
Adventure Guild    = Cross-kingdom mobility, Champion factory
Religious Institutions = Faith distribution, Heresy suppression
Underground Orgs   = Crime, Fear generation, Hidden cult support
```

### Event System Quick Reference
```
Crime              → Hurts economy, opportunity for god intervention
Accidents          → Faith crisis or boost depending on god response
Luck Events        → Unpredictable, affected by devotion level
Betrayal           → Changes power structure overnight
Marriage           → Merges influences, Faith opportunity
Political Events   → Kingdom-wide shifts, alliance restructuring
```

---

*WorldFaith GDD v3 — Professional Master Version*  
*"Players do not control the world. They influence belief, and belief controls the world."*
