# Black Alert — verified development status (2026-10-24)

## Verified on GitHub
- Write access and commits work.
- Unity 6 project structure and procedural native-3D test scene are checked in.
- Animated-code tank turret rigs, helicopter rotors, factory machinery, live combat shell paths, selection indicator and Android touch input are in source.
- GitHub automated source validation passes (this is **not** C# compilation).
- Android readiness workflow ran. **Unity activation secrets are missing.**

## Not yet verified
- No Unity compilation.
- No APK generated, Android installation or runtime tests.
- Models are procedural engineering test meshes, NOT high-fidelity final soldiers/tanks/buildings from concept illustrations.
- Online multiplayer, profiles backend and uploaded portraits, 6/8 player netcode, full music, World War III and special modes remain on requirements/engineering roadmap.

## Current release gate
1. Set up Unity cloud activation via GitHub Actions secrets. See docs/UNITY_CLOUD_BUILD.md and https://game.ci/docs/github/activation/.
2. Run GitHub Actions > Android APK (Unity) > Run workflow.
3. Diagnose genuine Unity compile/export errors from the job logs.
4. Install resulting APK on phone, screenshot menu/game and capture Android errors.
5. Only then improve production art, deep physics, and network simulation.

**Never claim source checks mean the game runs, or show concept mockups as in-game assets.** We will keep approved quality target and all modes in BLACK_ALERT_REQUIREMENTS.md.


## 2026-10-24 — game-first, free-minutes-preserving development

The user asked to build substantial systems in source before paying Unity cloud build costs. A headless game mode layer has been committed under `Assets/Scripts/Simulation/`:
- `MatchState.cs`: room/team validation, 2–8 player Skirmish elimination, Survival ring stage and last commander/team rules, Battlefield capture sectors and ticket drain.
- `FaceOffDeck.cs`: asymmetric career-card role, 12 distinct tactical effects, seeded card draws, energy and cooldowns.
- `WorldWarCampaign.cs`: country graph validation, ownership, capital-connected supply lines, alliances, economy, reinforcement and simplified turn-based invasion resolution. **The full verified all-country dataset, diplomacy UI and Unity tactical integration do not yet exist.**

These rule files are compiled outside Unity by `Tools/GameRulesChecks/GameRulesChecks.csproj` and the workflow `.github/workflows/game-rules.yml`. **26 assertions passed on GitHub Actions**: https://github.com/echallita3-sudo/Black-Alert/actions/runs/40052994085

Crucial distinction: these tests compile only the pure C# simulation layer and **cannot validate the Unity project's components**, model rigging, live graphics, Android export or networking. The last Unity Cloud APK tested is still Build #1 (early geometric visual test). Avoid spending user's free build minutes until substantial integrations and assets are ready.
