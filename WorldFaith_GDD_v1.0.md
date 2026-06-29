# WorldFaith — Master Game Design Document v1.0
**Multiplayer God Simulation Strategy Sandbox**

> **Core Fantasy:** Players do not control civilization directly. They influence belief, and belief changes the world.

*Prepared as a professional design bible for concept validation, prototyping, and early production planning.*

---

## Document Control

| Field | Value |
|-------|-------|
| Project | WorldFaith |
| Document | Master Game Design Document |
| Version | 1.0 |
| Primary Genre | God Simulation / Sandbox Strategy / Multiplayer AI Simulation |
| Target Platform | PC first; console/mobile possible later |
| Prototype Recommendation | Single-player simulation prototype before multiplayer |
| Core Design Statement | Gods compete by shaping what mortals believe. |

**Version Notes:** This version consolidates the god system, faith economy, NPC hierarchy, kingdom simulation, race faith affinity, miracles, counterplay, and prototype roadmap. The document is written as a buildable production blueprint for a small prototype team. Detailed numeric values are starting targets and should be balanced through playtesting.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Product Vision](#2-product-vision)
3. [Player Fantasy](#3-player-fantasy)
4. [Game Pillars](#4-game-pillars)
5. [Core Game Loop](#5-core-game-loop)
6. [Match Structure and Ages](#6-match-structure-and-ages)
7. [God System](#7-god-system)
8. [Faith Economy](#8-faith-economy)
9. [Race Faith Affinity](#9-race-faith-affinity)
10. [NPC Social Simulation](#10-npc-social-simulation)
11. [Kingdom Simulation](#11-kingdom-simulation)
12. [Royal, Noble, Servant, and Guild Systems](#12-royal-noble-servant-and-guild-systems)
13. [Religion System](#13-religion-system)
14. [Miracles and Divine Counterplay](#14-miracles-and-divine-counterplay)
15. [Evolution and Champion System](#15-evolution-and-champion-system)
16. [Events and Story Engine](#16-events-and-story-engine)
17. [AI Behavior Model](#17-ai-behavior-model)
18. [Multiplayer Design](#18-multiplayer-design)
19. [UI/UX Blueprint](#19-uiux-blueprint)
20. [Art and Audio Direction](#20-art-and-audio-direction)
21. [Technical Architecture](#21-technical-architecture)
22. [Data Model](#22-data-model)
23. [Balance Formulas](#23-balance-formulas)
24. [Prototype Roadmap](#24-prototype-roadmap)
25. [Risk Register](#25-risk-register)
26. [Appendices](#26-appendices)

---

## 1. Executive Summary

WorldFaith is a multiplayer god simulation strategy sandbox in which players act as gods trying to survive through belief. Civilizations are not directly controlled by players. They are driven by AI systems that manage population, economy, politics, warfare, religion, crime, accidents, luck, and social relationships.

Players gain divine points from faith. They spend those points on miracles, dreams, divine voices, disasters, portals, dungeons, blessings, corruption, and creature evolution. Rival gods can counter or twist these actions, creating indirect PvP based on public trust rather than direct combat.

A player loses when their god has no followers, their name is forbidden, or their religion disappears from living memory. There does not need to be one final winner. The core experience is god survival across repeated cycles of civilization growth, collapse, and rebirth.

**Unique Selling Point:** WorldFaith is a social faith simulator. Religions spread through kings, nobles, servants, guilds, families, criminals, heroes, accidents, miracles, and rumors.

---

## 2. Product Vision

### Vision Statement
Create a living fantasy world where belief is the main battlefield. Players shape civilizations by influencing faith, but AI societies decide how to respond.

### Emotional Target
- Feel powerful, but never fully in control.
- Watch small religious actions grow into historical consequences.
- Experience surprise when NPCs interpret divine signs differently than expected.
- Feel the danger of being forgotten, outlawed, or replaced by a rival god.
- Create legendary stories such as a servant cult taking over a royal court, a monster becoming a divine beast, or a saint falling into a demon lord.

### Player Promise
Every match creates a new myth. The player does not simply win wars; the player creates religions, legends, heresies, monsters, saints, and civilizations.

---

## 3. Player Fantasy

The player is not a king, general, or city builder. The player is a god competing for remembrance. The player watches mortals live, suffer, worship, doubt, betray, and evolve. The player can help, manipulate, frighten, bless, punish, or corrupt them.

| Player Desire | WorldFaith Response |
|--------------|-------------------|
| I want to feel like a god | Use miracles, dreams, omens, disasters, portals, and divine evolution. |
| I want strategy | Spend faith carefully, counter rival gods, manage trust, and influence social hierarchy. |
| I want stories | NPC relationships, kingdoms, guilds, cults, royals, nobles, and accidents create emergent history. |
| I want freedom | Any race can worship any god; alignment is probability-based, not hard-locked. |
| I want danger | A god can die if forgotten, forbidden, or replaced by another faith. |

---

## 4. Game Pillars

| Pillar | Meaning | Design Requirement |
|--------|---------|-------------------|
| **Belief is Power** | Faith generates divine points. | Every major system must connect back to faith, trust, or fear. |
| **Indirect Control** | Players influence, but AI decides. | God powers should push probability, not guarantee every outcome. |
| **Living Society** | NPC status and relationships matter. | Royal, noble, servant, guild, and commoner interactions affect religion. |
| **No Fixed Morality** | Good and dark are not race-locked. | A holy god can uplift monsters; a dark god can corrupt saints. |
| **Counterplay** | God actions can be contested. | Miracles should have counters, delays, or risk. |
| **Cycles of History** | Worlds rise, fall, and return. | Ages create natural pacing and replayability. |

---

## 5. Core Game Loop

### Primary Loop

1. Observe the world and identify vulnerable or valuable NPCs, regions, kingdoms, guilds, or races.
2. Influence mortals using dreams, whispers, omens, blessings, disasters, or miracles.
3. Mortals react through AI systems: belief, doubt, fear, politics, crime, loyalty, war, migration, or conversion.
4. Faith, trust, or fear changes based on outcome.
5. Spend gained divine points on bigger interventions, religion expansion, champions, or evolution.
6. Rival gods counter, corrupt, or exploit the same society.
7. Civilizations enter war, golden age, collapse, or rebirth.

### Age Loop

| Age | Focus | Typical Player Actions | Risk |
|-----|-------|----------------------|------|
| **Early Age** | First followers | Dreams, rain, small blessings, first shrine | Religion fails before it spreads. |
| **Kingdom Age** | Political expansion | Influence royals, nobles, guilds, temple growth | Rival gods compete for institutions. |
| **Conflict Age** | Holy wars and monsters | Counter miracles, empower champions, spawn dungeons | Civilian deaths reduce faith base. |
| **Collapse Age** | Empires fall | Protect cults, preserve relics, save core believers | God name becomes forbidden or forgotten. |
| **Rebirth Age** | New civilizations | Revive through relics, cults, myths, dreams | Old enemies return in new forms. |

---

## 6. Match Structure and Ages

### Recommended Modes

| Mode | Players | Length | Goal |
|------|---------|--------|------|
| **Sandbox** | 1-8 | Open-ended | Create emergent world stories. |
| **God Survival** | 2-8 | 60-180 min | Do not be forgotten. |
| **Cycle Dominion** | 2-8 | Fixed cycles | Highest influence after several Ages. |
| **Scenario** | 1-8 | Variable | Survive specific crisis such as demon invasion or eternal winter. |
| **Prototype Test** | 1 | 15-30 min | Validate faith, AI, and miracle loop. |

### Loss Conditions
- No living believers remain.
- The god name is outlawed and suppressed in all active societies.
- The religion becomes culturally extinct and no cult, relic, or myth can revive it.
- Optional: divine energy falls to zero for a long decay period.

### Victory Conditions
WorldFaith does not require a single winner, but competitive modes can use survival ranking, dominant faith percentage, highest trust, fear empire score, relic control, or cycle score.

---

## 7. God System

### God Creation Model
Recommended model: preset archetype plus customizable evolution tree. This gives clear identity while preserving long-term build freedom.

| Archetype | Core Fantasy | Strength | Weakness |
|-----------|-------------|----------|---------|
| **Light** | Heal, reveal, protect, inspire saints | High trust and devotion | Weak when trust collapses. |
| **Darkness** | Corrupt, frighten, hide cults | Fear economy and infiltration | Can trigger rebellions and hatred. |
| **Nature** | Growth, beasts, forests, harmony | Strong with elves and beastfolk | Vulnerable to industrial or fire destruction. |
| **War** | Strength, conquest, heroes | Strong with orcs and aggressive kingdoms | May destroy own believer base. |
| **Knowledge** | Magic, research, prophecy | Strong long-term scaling | Slow early expansion. |
| **Order** | Stability, law, empire | Controls kingdoms efficiently | Rigid; weak to chaos and rebellion. |
| **Chaos** | Mutation, luck, madness | Unpredictable growth and monsters | Hard to control outcomes. |

### God Resources

| Resource | Source | Spent On |
|----------|--------|---------|
| **Faith** | Believers × Devotion × Race Affinity × Trust × Institutions | All divine actions |
| **Trust** | Miracle success, commandments obeyed, healing, prophecy fulfilled | Conversion speed, commandment compliance |
| **Fear** | Disasters, curses, monsters, plague | Dark archetype alternative Faith source |
| **Memory** | Relics, sacred sites, myths, oral tradition | God survival after followers lost |

### God Rank

Gods gain divine rank as cumulative faith milestones are reached. Higher rank increases miracle efficiency, opens advanced evolution paths, expands divine communication reach, and modifies race affinity responses. A forgotten god loses rank and can only operate through relics and hidden cults.

---

## 8. Faith Economy

### Core Formula

```
Faith Gain Per Tick = Population Believers × Devotion × Race Affinity × Trust Modifier
                    × Institution Modifier × Event Modifier × God Rank Modifier
```

### Variable Definitions

| Variable | Description | Example |
|----------|-------------|---------|
| Population Believers | Number of NPCs currently worshiping the god. | 1,000 villagers. |
| Devotion | Casual, devout, fanatic, cultist intensity. | Fanatics generate more faith. |
| Race Affinity | Natural race-domain compatibility. | Elves favor Nature. |
| Trust Modifier | Confidence in god after events. | Rain saved crops: trust up. |
| Institution Modifier | Temples, priests, royal endorsement, guild support. | Royal temple doubles spread. |
| Event Modifier | War, famine, disaster, miracle success/failure. | False prophecy reduces gain. |
| God Rank Modifier | Higher divine rank increases efficiency. | Ancient god has greater presence. |

### Believer Types

| Type | Conversion Difficulty | Faith Output | Stability | Notes |
|------|----------------------|-------------|-----------|-------|
| **Casual** | Easy | Low | Low | Good for early spread but easy to lose. |
| **Devout** | Medium | Medium | Medium | Stable religious population. |
| **Fanatic** | Hard | High | High | Powerful but may cause extremism. |
| **Cultist** | Special | High | Hidden | Great for forbidden gods and palace infiltration. |
| **Heretic** | Emergent | Variable | Unstable | Believes in altered doctrine or splinter faith. |

### Faith Layers

| Layer | Meaning | Change Speed |
|-------|---------|-------------|
| **Passive Faith** | Inherited racial and cultural tendency. | Very slow. |
| **Active Faith** | Current worship and temple membership. | Moderate. |
| **Core Faith** | Deep personal conviction or trauma-based belief. | Very hard to change. |

---

## 9. Race Faith Affinity

### Design Goal
Any race can believe in any god, but races have passive belief tendencies based on culture, biology, history, environment, and instinct. This creates believable civilizations without removing emergent exceptions.

### Core Rules
- No race has a hard lock against any god.
- Each race has affinity percentages toward divine domains.
- Affinity modifies conversion speed, faith gain, trust, and resistance to doubt.
- Personal traits, childhood, homeland, social class, and divine events can override racial tendency.
- Race affinity represents probability, not destiny.

### Affinity Scale

| Affinity | Meaning | Gameplay Impact |
|----------|---------|----------------|
| 150-170% | Deep cultural harmony | Fast conversion, high trust, high faith gain. |
| 120-140% | Preferred belief | Good conversion and stable worship. |
| 90-110% | Neutral | Normal conversion. |
| 60-80% | Difficult | Slow conversion and lower faith gain. |
| 30-50% | Rejected tendency | Requires strong events or special NPC traits. |
| 10-20% | Taboo | Possible only through extraordinary story events. |

### Sample Race Affinity Matrix

| Race | Strong Affinities | Weak Affinities | Special Notes |
|------|-------------------|----------------|---------------|
| **Humans** | Order, Light, War, Knowledge, Nature | None extreme | Most flexible; easily influenced by leadership. |
| **Elves** | Nature, Light, Knowledge | Fire/Destruction, Darkness, Chaos | Burned homeland severely reduces trust. |
| **Dwarves** | Order, Craft, Knowledge, War | Chaos, Nature excess | Hard to convert; tradition matters. |
| **Orcs** | War, Strength, Survival, Chaos | Peace, Healing, Knowledge | Good or intelligent orcs can emerge but are statistically rare. |
| **Beastfolk** | Nature, Tribe, Hunt, Moon | Order empire, abstract Knowledge | Tribal loyalty amplifies local prophets. |
| **Demons** | Darkness, Chaos, Death, Domination | Light, Peace, Mercy | Fear-based faith is efficient. |
| **Angels** | Light, Order, Justice | Darkness, Chaos, Death | Can fall through betrayal, trauma, or corruption. |
| **Undead** | Death, Memory, Darkness | Nature, Life, Fertility | May preserve forgotten gods as old memory. |

### Personal Trait Override
Race decides starting probability. Traits decide personal exceptions. A Genius Orc may love Knowledge. A Traumatized Elf may embrace Fire if fire saved them from monsters. A Merciful Demon may follow Light after a saint spares them.

| Trait | Effect on Faith |
|-------|----------------|
| **Genius** | Raises Knowledge affinity and resistance to superstition. |
| **Fanatic** | Raises devotion and obedience; lowers doubt. |
| **Compassionate** | Raises Healing, Light, Mercy, Nature. |
| **Ambitious** | Raises War, Darkness, Order, Power. |
| **Traumatized** | Can create hatred or dependence based on event source. |
| **Curious** | Raises openness to foreign gods and heresy. |
| **Traditional** | Raises ancestral religion stability. |
| **Reckless** | Increases chance to accept dangerous gods or forbidden powers. |

### Environmental Memory
Divine actions are remembered by races and regions. A god who saves a forest may gain elf trust for generations. A god who floods an orc war camp may be seen as weak or dishonorable unless the storm kills a stronger enemy.

| Event | Affected Group | Faith Impact |
|-------|---------------|-------------|
| Forest burned | Elves and nature cultures | Trust toward destructive/fire gods drops sharply. |
| Famine ended | Farmers, commoners, nature followers | Trust and conversion rise. |
| Monster attack stopped | Village, guild, royal court | Heroic god reputation rises. |
| Noble scandal exposed | Commoners and servants | Trust in royal religion may fall. |
| Orc champion empowered | Orc clans | War/strength god faith rises. |
| Sacred mountain destroyed | Dwarves or ancestral cultures | Long-term hostility to responsible god. |

---

## 10. NPC Social Simulation

### Purpose
The NPC system makes faith spread through social life instead of only through population numbers. NPCs have class, role, relationships, personality, faith, fear, loyalty, ambition, morality, intelligence, secrets, and memory of events.

### Social Classes

| Tier | Class | Examples | Primary Role | Faith Influence |
|------|-------|---------|-------------|----------------|
| 1 | **Commoners** | Farmers, workers, merchants, hunters | Population, economy, local rumors | Main faith base. |
| 2 | **Servants** | Maids, guards, attendants, butlers | Serve nobles/royals; spread secrets | Powerful hidden transmission path. |
| 3 | **Adventurers** | Warriors, mages, healers, scouts | Dungeons, monsters, relics | Can become heroes, saints, or corrupted champions. |
| 4 | **Nobles** | Lords, dukes, governors | Regional control, taxes, armies | Can convert entire provinces. |
| 5 | **Royalty** | King, queen, prince, princess | National policy and legitimacy | Can shift kingdom religion quickly. |
| 6 | **Religious Elite** | Priests, prophets, cult leaders | Doctrine and worship | Direct faith multiplier. |

### NPC Core Stats

| Stat | Function | Gameplay Example |
|------|----------|----------------|
| **Faith** | Current belief strength in a god. | Determines prayer output. |
| **Trust** | Confidence in their god. | Low trust causes doubt or conversion. |
| **Fear** | Fear of gods, monsters, rulers, death. | Can fuel dark worship. |
| **Loyalty** | Duty to family, kingdom, house, guild, or religion. | A loyal noble may resist cult bribery. |
| **Ambition** | Desire for power or status. | High ambition increases corruption risk. |
| **Morality** | Personal ethical tendency. | A cruel priest may abuse doctrine. |
| **Intelligence** | Reasoning and planning. | Smart rulers resist obvious manipulation. |
| **Courage** | Willingness to face danger. | High courage helps resist fear gods. |
| **Trauma** | Emotional damage from past events. | May make NPC vulnerable to revenge religion. |

### Relationship Graph
Every important NPC should have a limited set of tracked relationships rather than tracking everyone.

Recommended relationships: family, superior, servant, rival, friend, lover/spouse, religious mentor, guild contact, enemy, and secret handler.

| Relationship | Potential Faith Effect |
|-------------|----------------------|
| Royal to Noble | Royal faith can pressure noble houses. |
| Noble to Servant | Servants may spread secrets from noble estates. |
| Servant to Royal | A hidden cultist servant can infiltrate palace faith. |
| Adventurer to Commoners | Successful dungeon clears inspire village conversion. |
| Priest to Noble | Nobles may fund temples or persecute heresy. |
| Criminal to Commoners | Crime raises fear and weakens trust in rulers. |

---

## 11. Kingdom Simulation

### Kingdom Core Stats

| Stat | Description | Linked Systems |
|------|-------------|---------------|
| **Population** | Total living citizens and workforce. | Faith base, army, economy. |
| **Food** | Agriculture and stored food. | Famine, riots, miracles. |
| **Wealth** | Gold, trade, taxes. | Nobles, guilds, construction. |
| **Military** | Defense, conquest, security. | War gods, crime suppression. |
| **Stability** | Civil order and legitimacy. | Rebellion, cults, royal trust. |
| **Corruption** | Abuse by elites or institutions. | Dark gods, crime, servant resentment. |
| **Religious Unity** | How unified the kingdom faith is. | Schism, holy war, conversion. |
| **Technology/Magic** | Development level. | Knowledge gods, guild power. |
| **Fear Level** | Public fear of danger or punishment. | Dark resource, panic, obedience. |
| **Happiness** | Daily welfare. | Trust, conversion, rebellion. |

### Kingdom AI Priorities
1. Survive immediate threats: famine, invasion, monsters, plague, collapse.
2. Preserve royal legitimacy and noble loyalty.
3. Maintain population, food, and economy.
4. Respond to religious pressure and miracles.
5. Expand territory if stable and ambitious.
6. Suppress crime, cults, rebels, or rival faiths depending on ideology.
7. Use adventurer guilds to solve threats that armies cannot efficiently handle.

### Government Types

| Type | Strength | Weakness | Faith Behavior |
|------|----------|---------|----------------|
| **Monarchy** | Fast policy from royal decision | Succession crisis | Royal faith strongly affects nation. |
| **Theocracy** | High religious unity | Low tolerance | Priests dominate conversion. |
| **Noble Council** | Regional stability | Factional conflict | Noble houses compete over doctrine. |
| **Tribal Clan** | High loyalty and war readiness | Weak bureaucracy | Chief and shaman matter most. |
| **Merchant State** | Wealth and trade | Low military unity | Faith follows profit and social stability. |
| **Monster Horde** | Rapid expansion | Low diplomacy | Strength gods spread quickly. |

---

## 12. Royal, Noble, Servant, and Guild Systems

### Royal Court
The royal court is the political center of a kingdom. It contains royals, advisors, commanders, servants, guards, visiting nobles, priests, spies, and sometimes hidden cultists.

| Court Role | Function | Faith Risk |
|------------|---------|-----------|
| **King/Queen** | Sets policy, wars, public religion | Conversion can shift entire kingdom. |
| **Prince/Princess** | Succession and marriage politics | Can be targeted by dreams or cults. |
| **Advisor** | Influences decisions | High-risk manipulation target. |
| **Court Priest** | Religious legitimacy | Can create schism or suppress rival gods. |
| **Servant** | Access to secrets | Can spread cults unseen. |
| **Guard Captain** | Security control | Can protect or overthrow court. |

### Noble Houses
Nobles serve the royal family but control regions. They collect taxes, raise troops, appoint officials, sponsor temples, and influence local faith. Nobles may obey, rebel, betray, or secretly worship another god.

| Noble Behavior | Trigger | Outcome |
|---------------|---------|---------|
| **Support Crown** | High loyalty, shared faith | Kingdom stability increases. |
| **Regional Conversion** | Noble adopts new god | Province faith shifts. |
| **Secret Cult** | Ambition, fear, corruption | Hidden religion spreads. |
| **Rebellion** | Low loyalty, high ambition | Civil war. |
| **Religious Patronage** | High devotion, wealth | Temples and priests grow. |

### Servant Network
Servants are socially weak but informationally powerful. They move through private spaces where royals and nobles reveal secrets. A servant can become a spy, prophet, cultist, blackmailer, or hidden saint.

- Servants overhear royal secrets and scandals.
- Servants connect noble houses through daily labor.
- Servants can spread rumors faster than official priests.
- Servant mistreatment increases resentment, crime, and cult vulnerability.
- A miracle saving servants can create loyal hidden believers inside elite households.

### Adventure Guild
The Adventure Guild is semi-independent. It accepts contracts from kingdoms, nobles, villages, temples, and sometimes cults. It connects dungeon systems, relics, monsters, heroes, and faith reputation.

| Guild Mission | Possible Results | Faith Impact |
|--------------|----------------|-------------|
| **Clear Dungeon** | Success, death, corruption, relic discovery | Gods tied to mission gain or lose trust. |
| **Hunt Monster** | Monster slain or evolved | War/nature/dark gods may react. |
| **Escort Noble** | Protect or expose noble | Can trigger scandal or loyalty. |
| **Explore Ruins** | Find relic, awaken old god | Memory system activates. |
| **Investigate Cult** | Expose or join cult | Faith conflict escalates. |

---

## 13. Religion System

### Religion as a Dynamic Entity
A religion has name, god, doctrine, symbols, holy places, priesthood, rituals, sects, enemies, legitimacy, and memory. It can split, go underground, merge, radicalize, or become state religion.

### Doctrine Axes

| Axis | Low End | High End | Gameplay Effect |
|------|---------|---------|----------------|
| **Mercy vs Punishment** | Forgiveness | Retribution | Changes crime and heresy response. |
| **Isolation vs Expansion** | Protect own people | Convert outsiders | Changes missionary behavior. |
| **Harmony vs Dominion** | Balance with nature | Control the world | Changes race/environment reactions. |
| **Freedom vs Order** | Individual faith | Strict hierarchy | Changes royal/noble support. |
| **Sacrifice vs Prosperity** | Suffering has value | Blessed life proves faith | Changes disaster interpretation. |

### Schism and Heresy
Schisms occur when religion spreads across different classes, races, regions, or traumatic events. A god may support, suppress, or exploit a schism. Heresies are dangerous but can help a god survive suppression by decentralizing belief.

| Schism Trigger | Example |
|---------------|---------|
| **Class Divide** | Commoners worship a merciful version; nobles worship order version. |
| **Race Difference** | Elves reinterpret fire god as rebirth instead of destruction. |
| **Failed Miracle** | Followers blame priesthood and form reform sect. |
| **Dark Counterplay** | Rival god inserts false doctrine through dreams. |
| **Forbidden Name** | Cult preserves god under a secret symbol. |

---

## 14. Miracles and Divine Counterplay

### Miracle Philosophy
Miracles should be powerful but risky. A miracle is not just an effect; it is a public religious statement. If it succeeds, trust rises. If countered, misunderstood, or harmful, trust may collapse.

### Miracle Categories

| Category | Examples | Primary Resource | Risk |
|----------|---------|-----------------|------|
| **Weather** | Rain, drought, storm, snow | Faith | Can be countered or harm civilians. |
| **Blessing** | Harvest, healing, courage, fertility | Faith + trust | Failure damages trust. |
| **Disaster** | Earthquake, fire, plague, volcano | Faith/fear | Destroys believers and creates hatred. |
| **Communication** | Dream, omen, divine voice, revelation | Faith | Can be ignored or misread. |
| **Summoning** | Portal, monsters, guardians | Faith/fear | Can spiral out of control. |
| **Corruption** | Madness, fall, cursed bloodline | Fear/dark faith | May create heroes against you. |
| **Creation** | Dungeon, relic, sacred land | Faith/memory | Rival gods may exploit it. |

### Miracle Costs (Starting Numeric Targets)

| Size | Faith Cost | Examples |
|------|-----------|---------|
| Small | 50-100 | Rain, dream, blessing |
| Medium | 250-500 | Storm, curse, portal |
| Large | 1000+ | Volcano, revelation, divine beast |

### Counterplay Examples

| Action | Counter | Public Interpretation |
|--------|---------|----------------------|
| Rain saves crops | Thunderstorm floods village | Followers doubt rain god or fear storm god. |
| Heal saint | Corrupt saint | Saint becomes unstable or fallen. |
| Prophecy warns invasion | False dream alters details | King may make wrong decision. |
| Create divine beast | Dark curse mutates beast | Protector becomes disaster. |
| Open dungeon for loot | Portal infestation | Guild deaths reduce trust. |
| Bless royal line | Expose royal scandal | Royal faith legitimacy collapses. |

### Timing and Visibility
- Instant miracles are dramatic but should have high cost or cooldown.
- Delayed miracles create strategy and counter windows.
- Hidden actions such as dreams and corruption should not reveal the responsible god immediately.
- Public actions such as volcanoes or revelations should be visible to all gods and mortals.

---

## 15. Evolution and Champion System

### Core Concept
Gods can shape living beings through belief. Evolution depends on race, faith, trauma, environment, miracles, and personal destiny. There is no fixed good or evil path.

### Evolution Paths

| Base Being | Holy / Divine Path | Dark / Corrupt Path | Neutral / Wild Path |
|------------|-------------------|--------------------|--------------------|
| **Human Hero** | Saint, Oracle, Holy Knight | Fallen Saint, Demon Lord | Legendary King, Archmage |
| **Wolf** | Sacred Wolf, Divine Beast | Blood Fenrir | Alpha Spirit |
| **Orc Warrior** | Guardian Champion | Berserker Demon Lord | Warlord King |
| **Elf Mage** | Forest Saint, Star Seer | Ash Witch | Archdruid |
| **Monster** | Holy Guardian | Abyssal Titan | Ancient Beast |
| **Demon** | Redeemed Guardian | Archdemon | Independent Demon King |

### Champion Rules
- Champions are named NPCs with strong narrative weight.
- A champion can spread faith, win wars, inspire cults, or break kingdoms.
- Champions can be stolen, corrupted, redeemed, killed, or remembered as relic legends.
- Players should not control champions directly; they influence their beliefs, goals, and circumstances.

---

## 16. Events and Story Engine

### Event Types

| Event Type | Examples | Faith Impact |
|------------|---------|-------------|
| **Daily Life** | Marriage, birth, market luck, accident | Small trust changes. |
| **Crime** | Theft, murder, corruption, bandit raid | Fear rises; trust in rulers falls. |
| **Politics** | Succession, scandal, rebellion | Faith can follow faction lines. |
| **Guild** | Dungeon clear, relic find, adventurer death | Champion and myth generation. |
| **Disaster** | Fire, flood, plague, famine | Major faith tests. |
| **Divine** | Miracle, omen, revelation, counter-miracle | Direct god reputation changes. |
| **Cycle** | Empire collapse, rebirth, forbidden religion | God survival challenge. |

### Luck, Crime, and Accident System
Small NPC events matter because mortals interpret them religiously. A lucky harvest may be seen as blessing. A servant accident in a noble house may become rumor. A crime wave may create demand for order, justice, fear, or revenge gods.

| Incident | Likely Mortal Interpretation |
|----------|------------------------------|
| Farmer survives lightning | Chosen by storm god or punished but spared. |
| Noble child recovers from illness | Healing god gains trust. |
| Guild party dies in dungeon | Dungeon god, death god, or rival god blamed. |
| Servant exposes royal corruption | Truth/justice gods gain support. |
| Orc clan wins battle after dream | War god reputation rises. |
| Elf forest fire stopped by rain | Nature/rain god earns deep trust. |

---

## 17. AI Behavior Model

### AI Decision Stack
1. **Need:** What problem is urgent? Food, war, crime, faith, politics, survival.
2. **Personality:** How does this NPC or kingdom prefer to solve problems?
3. **Belief:** Which gods, religions, fears, taboos, and prophecies matter?
4. **Social Pressure:** What do royals, nobles, servants, guilds, and commoners expect?
5. **Memory:** What past miracles, disasters, betrayals, or luck events are remembered?
6. **Action:** Decide response such as worship, rebel, investigate, convert, suppress, pray, migrate, attack.

### King AI Behavior Table

| Situation | Possible AI Response | Factors |
|-----------|---------------------|---------|
| **Famine** | Pray, ration, tax, raid neighbor, ask guild, build shrine | Food, faith, trust, ruler morality. |
| **New Cult** | Ignore, investigate, suppress, join secretly | Stability, fear, noble involvement. |
| **Miracle Success** | Public praise, temple funding, conversion | Trust, doctrine, royal faith. |
| **Counter-Miracle Disaster** | Blame rival god, blame original god, execute priests | Intelligence, propaganda, evidence. |
| **Noble Rebellion** | Negotiate, crush, marry alliance, call holy war | Loyalty, military, faith unity. |
| **Dungeon Appears** | Hire guild, seal area, worship dungeon god, exploit relics | Threat level, economy, guild strength. |

### NPC Faith Decision Formula
```
Conversion Chance = Base Openness × Race Affinity × Trait Modifier
                  × Social Pressure × Recent Event Impact × Trust Difference × Fear Pressure
```

### Optimization Rule
Do not simulate every NPC at the same detail level. Use three layers:
- Population groups for commoners.
- Important NPC agents for elites and champions.
- Event summaries for background activity.

---

## 18. Multiplayer Design

### Core Principle
Players interact through the world, not through direct attacks. Rival gods counter miracles, convert institutions, corrupt champions, spread heresies, or manipulate royal decisions.

### Player Information

| Info Type | Visibility |
|-----------|-----------|
| Public miracles | Visible to all players. |
| Large disasters | Visible to all players. |
| Faith percentages by region | Approximate unless scouted through divine knowledge. |
| Hidden cults | Unknown unless discovered. |
| Dreams and whispers | Private unless target reveals or behavior exposes it. |
| Rival god exact faith | Hidden; shown as estimated rank. |

### Anti-Snowball Tools
- Large religions become easier targets for schism.
- High-rank gods attract fear and alliances against them.
- Big miracles have cooldowns and public counter windows.
- Killing too many enemies may reduce total world faith available.
- Forgotten gods can survive as relics or cults for comeback potential.

---

## 19. UI/UX Blueprint

### Primary Screens

| Screen | Purpose | Key Elements |
|--------|---------|-------------|
| **World Map** | Main simulation view | Biomes, kingdoms, faith overlays, event alerts. |
| **God Panel** | Player power management | Faith, trust, fear, rank, miracles, cooldowns. |
| **Kingdom Panel** | Inspect civilization | Population, ruler, religion, stability, nobles, guild. |
| **NPC Inspector** | Important character details | Class, traits, relationships, faith, secrets. |
| **Religion Panel** | Faith details | Doctrine, sects, temples, believers, rivals. |
| **Event Log** | Narrative tracking | Miracles, scandals, wars, conversions, deaths. |
| **Race Affinity View** | Strategic targeting | Race-domain compatibility and trust modifiers. |

### Map Overlays
- Trust heatmap
- Fear heatmap
- Faith ownership
- Religion conflict
- Kingdom stability
- Crime
- Noble influence
- Guild activity
- Dungeon danger
- Race population

---

## 20. Art and Audio Direction

### Visual Style Recommendation
For prototype: readable 2D or low-poly stylized. Avoid high-detail visuals early. This game succeeds through simulation clarity, not visual realism.

### Visual Priorities
- Every kingdom and religion must be readable at map scale.
- Miracles must have clear visual identity and cause/effect feedback.
- NPC social class should be recognizable through iconography.
- Race faith affinity can be shown through symbols, colors, or UI badges.
- Dungeons, portals, holy sites, and cursed areas need strong silhouettes.

### Audio Direction
- Divine actions use layered sound: whisper, choir, storm, low rumble, bells, drums.
- Each god archetype should have a musical identity.
- Public miracles should feel different from hidden dreams.
- Faith gain/loss should have subtle but satisfying feedback.

---

## 21. Technical Architecture

### Recommended Prototype Architecture

| System | Responsibility |
|--------|---------------|
| **World Manager** | Tiles, biomes, settlements, resources, disasters. |
| **Population Manager** | Grouped commoner simulation and demographic changes. |
| **NPC Manager** | Important named NPCs and relationship graph. |
| **Kingdom Manager** | Economy, war, politics, government, stability. |
| **Religion Manager** | Beliefs, doctrines, sects, conversion, temples. |
| **Faith Economy Manager** | Faith, trust, fear, memory calculations. |
| **God Manager** | Player resources, miracles, rank, cooldowns. |
| **Event Manager** | Crime, accident, luck, prophecy, disasters, story events. |
| **Guild Manager** | Dungeons, missions, adventurers, relics. |
| **AI Director** | Pacing, age transitions, crisis generation. |

### Performance Strategy
- Use grouped simulation for commoners instead of individual agents.
- Track full detail only for royals, nobles, champions, priests, guild leaders, cult leaders, and selected servants.
- Batch faith calculations by region and religion.
- Run low-priority AI less frequently than active crisis AI.
- Use event queues rather than constant individual checks.

---

## 22. Data Model

### Core Data Entities

| Entity | Important Fields |
|--------|----------------|
| **God** | id, name, archetype, rank, faith, trust profile, fear, miracle unlocks, memory. |
| **Religion** | id, god_id, doctrine, sects, temples, legal status, symbols, believers. |
| **Race** | id, name, affinity matrix, cultural taboos, passive traits. |
| **NPC** | id, name, race, class, kingdom, traits, faith, trust, fear, relationships, secrets. |
| **Kingdom** | id, name, ruler, government, population, economy, stability, military, religion mix. |
| **Noble House** | id, territory, loyalty, wealth, faith, rivalries, servants. |
| **Guild** | id, base city, members, reputation, active missions, relics. |
| **Event** | id, type, location, participants, cause, effect, memory weight. |
| **Miracle** | id, god_id, type, cost, cooldown, visibility, target, counters. |
| **Relic** | id, origin god, effect, location, memory power, owner. |

### Sample NPC Record

| Field | Example |
|-------|---------|
| Name | Rukha Ironmind |
| Race | Orc |
| Class | Adventurer |
| Traits | Genius, Curious, Brave |
| Passive Affinity | War 150%, Knowledge 40% |
| Trait Override | Knowledge +50% |
| Active Faith | Knowledge God |
| Core Conflict | Feels pressure from orc clan to worship Strength God |
| Story Potential | May become scholar-hero or heretic champion. |

---

## 23. Balance Formulas

### Faith Gain
```
Faith Gain = Believers × Devotion × Race Affinity × Trust × Institution × Event × Rank
```

### Trust Change
```
Trust Change = Miracle Result Value × Attribution Certainty × Doctrine Match × Harm Modifier
```

### Conversion Chance
```
Conversion Chance = Openness × Race Affinity × Social Pressure × Trust Difference
                  × Recent Events × Trait Modifier
```

### Schism Chance
```
Schism Chance = Religion Size × Doctrine Conflict × Class Tension × Race Difference
              × Failed Miracle Impact × Priest Corruption
```

### Rebellion Chance
```
Rebellion Chance = Low Stability × Noble Ambition × Food Shortage × Religious Conflict × Crime × Fear
```

### Champion Fall Chance
```
Fall Chance = Trauma × Ambition × Corruption Exposure × Trust Loss × Rival God Influence
            − Loyalty − Core Faith
```

### Starting Numeric Targets

| Value | Starting Target | Notes |
|-------|----------------|-------|
| Faith tick interval | 10 seconds | Prototype friendly. |
| Small miracle cost | 50-100 Faith | Rain/dream/blessing. |
| Medium miracle cost | 250-500 Faith | Storm/curse/portal. |
| Large miracle cost | 1000+ Faith | Volcano/revelation/divine beast. |
| Casual devotion | 0.5x | Low output. |
| Devout devotion | 1.0x | Baseline. |
| Fanatic devotion | 2.0x | High but risky. |
| Cultist devotion | 1.5x | Hidden and stable. |
| Major failed miracle trust hit | -20% to -50% | Depends on damage. |

---

## 24. Prototype Roadmap

### Prototype V1 — Faith Sandbox
- One small map with 3 biomes and 2 AI villages.
- Two playable gods: Nature and War.
- Three races: Humans, Elves, Orcs.
- Basic faith gain, trust, and race affinity.
- Three miracles: Rain, Bless Strength, Storm Counter.

**Outcome goal:** Prove that AI reaction to miracles feels fun.

### Prototype V2 — Social NPC Layer
- Add royal, noble, servant, adventurer guild roles.
- Add named NPCs and relationship graph.
- Add crime, accident, and luck events.
- Add conversion through social hierarchy.

**Outcome goal:** Prove faith can spread through NPC interactions.

### Prototype V3 — Dungeons and Champions
- Add dungeon spawn and guild missions.
- Add hero, saint, demon lord, divine beast paths.
- Add relics and memory system.

**Outcome goal:** Prove emergent stories are memorable.

### Prototype V4 — Multiplayer Counterplay
- Two-player online or local test.
- Add counter-miracle windows.
- Add hidden dreams and visible disasters.

**Outcome goal:** Prove indirect PvP is understandable and strategic.

### Prototype V5 — Cycle System
- Add collapse and rebirth.
- Add forbidden religions and hidden cult survival.
- Add god memory through relics.

**Outcome goal:** Prove long-term god survival loop.

---

## 25. Risk Register

| Risk | Severity | Mitigation |
|------|---------|-----------|
| AI simulation becomes too complex | High | Use layered simulation and event batching. |
| Players feel disconnected | High | Give clear feedback, event log, and faith attribution. |
| Miracle counterplay feels unfair | Medium | Use warning windows, visibility rules, and cost scaling. |
| Snowballing dominant gods | High | Use schism, coalition pressure, public target status, and trust decay. |
| Too many NPCs to understand | Medium | Focus on important named NPCs and summaries. |
| Multiplayer sync complexity | High | Prototype single-player first. |
| Religious themes become sensitive | Medium | Use fictional gods, races, and fantasy framing; avoid real-world religions. |
| Scope too large | High | Build in strict prototype phases. |

---

## 26. Appendices

### Appendix A — Example Emergent Story

A servant in the royal palace secretly worships a forbidden dark god after being saved in a nightmare. The servant converts another maid, then learns that a noble is hiding treason. The cult blackmails the noble and uses his wealth to fund hidden shrines. The prince falls ill, and the cult claims only their god can save him. When the prince recovers due to a miracle, he becomes sympathetic. Years later he inherits the throne and legalizes the forbidden faith. The kingdom changes religion without a single army being directly controlled by the player.

### Appendix B — Example Race Exception

An orc born with the Genius and Curious traits rejects the clan strength god and follows the Knowledge God. The clan distrusts him, but after he invents siege engines that save the tribe, his faith gains legitimacy. Over time, a rare orc scholarly sect appears. This is possible because race affinity is probability, not destiny.

### Appendix C — Core Philosophy

**Design Mantra:** Players do not control the world. They influence belief, and belief controls the world.

---

*WorldFaith Master Game Design Document v1.0*
