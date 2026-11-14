# 🚀 Lunar Outpost – Engineering Trade-offs (Hackathon Project)

2D Unity game (Unity 2022+, C#) for kids 7-12. Manage a Moon/Mars outpost where
**every choice costs something**. Real NASA science: SPEs, GCRs, regolith, water shielding.

## Folder structure
```
Assets/_Project/
├── Scenes/              Main scene + MiniGames/ (ONE scene per mini-game!)
├── Prefabs/             UI/ and Shared/ prefabs
├── Art/ Audio/          Sprites, UI art, fonts, music, sfx
├── Data/                ScriptableObject assets
│   ├── Resources/       Resource definitions (Food&O2, Power, Shielding, Rest)
│   └── Events/          Dilemma assets (one file per dilemma)
├── Scripts/
│   ├── Core/            GameManager, ResourceManager, GameEvents (the "glue")
│   ├── Data/            ScriptableObject class definitions
│   ├── Dashboard/       Main menu UI, resource bars (Sliders + TMP)
│   ├── Events/          Dilemma popup + random event system
│   ├── Telescope/       NASA APOD API fetch + 5-second rest timer
│   ├── MiniGames/
│   │   ├── Pipes/       Mini-game 1: Food & O2
│   │   ├── Shields/     Mini-game 2: Galaga-style magnetic deflector
│   │   └── Sokoban/     Mini-game 3: zero-g battery crates
│   └── Shared/          Helpers used by several modules (keep tiny)
└── Docs/                Design notes, NASA science facts
```

## Team ownership (edit names!)
| # | Person | Module | Folders they own |
|---|--------|--------|------------------|
| 1 | ___ | Core + Resources | Scripts/Core, Scripts/Data, Data/Resources |
| 2 | ___ | Dashboard UI | Scripts/Dashboard, Prefabs/UI, Art/UI |
| 3 | ___ | Dilemmas / Events | Scripts/Events, Data/Events |
| 4 | ___ | Telescope (NASA API) + Pipes mini-game | Scripts/Telescope, Scripts/MiniGames/Pipes |
| 5 | ___ | Shields mini-game | Scripts/MiniGames/Shields |
| 6 | ___ | Sokoban mini-game + Audio/Art polish | Scripts/MiniGames/Sokoban, Audio |

## Code rules
- **Namespaces**: `LunarOutpost.Core`, `.Dashboard`, `.Events`, `.Telescope`, `.MiniGames.Pipes`, etc.
- **Modules never reference each other directly.** They talk through `Core`:
  - Change a resource: `ResourceManager.Instance.Change(ResourceType.Power, -10f)`
  - Mini-games report back with a `GameEvents.OnMiniGameFinished` event.
- Use `[SerializeField] private` for Inspector fields. Comment everything (kids' game, beginner-friendly team!).
- Resource bars are 0–100. Every choice must have a cost.

## Git rules (Unity merges are painful – follow these!)
1. **One scene per mini-game, one owner per scene.** Never edit someone else's scene.
2. Build things as **prefabs**, not loose objects in scenes.
3. Branches: `main` (always works) ← `dev` ← `feature/<name>-<thing>`. Open a PR into `dev`.
4. Commit the `.meta` files. Never commit `Library/`, `Temp/`, etc.
5. Unity: *Edit → Project Settings → Editor → Asset Serialization = Force Text*, *Version Control = Visible Meta Files*.
6. Pull before you start, push small and often.
7. **NASA API key**: don't commit it. Use `DEMO_KEY` for tests or a local `secrets.json` (git-ignored).

## Setup
1. Install Unity 2022 LTS (2D template), create the project **inside this repo's root** (so `Assets/` is here), or open this folder as the project.
2. Install packages: TextMeshPro, 2D Sprite, Input System (optional).
