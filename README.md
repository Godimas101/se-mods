# 🛸 Space Engineers Mods

> **Custom mods for Space Engineers — LCD displays, server gameplay overhauls, and compatibility patches.**

## What's This?

My full Space Engineers mod collection. The flagship project is **InfoLCD** — a suite of information display screens for LCDs. The rest are gameplay overhauls and balance patches built for the Sturmgrenadier server community.

## What Lives Here?

### 📊 InfoLCD Series

| Mod | Status | Notes |
|-----|--------|-------|
| **InfoLCD - Apex Update** | Active development | For the Apex Update modpack |
| **InfoLCD - Apex Advanced** | Maintenance only | For the Apex Advanced modpack — kept in sync |

20+ specialized screens: Items, Cargo, Power, Production, Ammo, Components, Doors, Damage, Gas, Life Support, and more. Scrolling lists, category filtering, subgrid scanning, CustomData configuration.

See [Mods/InfoLCD - Apex Update/CLAUDE.md](Mods/InfoLCD%20-%20Apex%20Update/CLAUDE.md) for the full screen inventory and implementation details.

---

### ⚙️ Sturmgrenadier Core Series

Core gameplay overhaul for the Sturmgrenadier server ecosystem.

| Mod | Purpose |
|-----|---------|
| Sturmgrenadier Core Mod | Base overhaul |
| Sturmgrenadier Core Power | Power system |
| Sturmgrenadier Core Production | Production & crafting |
| Sturmgrenadier Core Survival | Survival mechanics |
| Sturmgrenadier Core Vanilla Combat | Combat balance |

---

### 🔧 Mod Adjusters

Balance and compatibility patches for third-party mods used on Sturmgrenadier servers.

| Mod | Adjusts |
|-----|---------|
| Artillery MKII Turret - Goliath | Weapon balance |
| Dense Colorable Solar Panels | Power output |
| Federal Industrial - Utilities | Compatibility |
| Isy's Dense Solar Panels | Power output |
| Life'Tech - Algaetechnology | Farming/resources |
| ModCubeBlocks Refinery x10 | Refinery speed |
| ModCubeBlocks Upgrade Module | Module balance |
| More Engineer Characters | Character compatibility |
| More Wind Turbines | Wind power output |
| [Mafoo] More Batteries | Battery capacity/charge |

---

### ✨ Other Mods

| Mod | Description |
|-----|-------------|
| Not Just For Looks | Gives DLC cosmetic blocks actual useful stats |

---

## 🛠️ Development Setup

**Prerequisites:**
- Space Engineers + Mod SDK (auto-installs via Steam)
- Visual Studio or VS Code for C#

**SDK location:**
```
D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\
```

**Building:**
1. Reference SDK DLLs for IntelliSense
2. Write scripts in `Data/Scripts/<namespace>/`
3. Register LCD scripts in `Data/TextSurfaceScripts.sbc`
4. Test via `%AppData%\SpaceEngineers\Mods\` or Workshop

---

## 📚 Documentation

| File | Purpose |
|------|---------|
| [CLAUDE.md](CLAUDE.md) | AI assistant context — API patterns, gotchas, SDK reference |
| [MOD_MAKING_NOTES.md](MOD_MAKING_NOTES.md) | Working notes — implementation decisions, status tracking |
| [Mods/InfoLCD - Apex Update/CLAUDE.md](Mods/InfoLCD%20-%20Apex%20Update/CLAUDE.md) | InfoLCD screen inventory and scrolling implementation |

---

## 🔗 Key Resources

- [Space Engineers Modding Wiki](https://spaceengineers.wiki.gg/wiki/Modding/Reference)
- [Mod API Documentation](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html)
- [Steam Workshop](https://steamcommunity.com/app/244850/workshop/)

---

*"Building better blocks, one mod at a time."*
