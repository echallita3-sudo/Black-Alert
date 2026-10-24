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

## 2026-10-24 — match modes wired to Unity

A major source-only batch is now on `main`:

- **Unity modes:** `BlackAlertPlayableModes.cs` connects offline Skirmish, Battlefield control sectors and score tickets, a shrinking Survival ring, a card-powered Face-Off demonstration, and a clearly labeled *six-country demo* of World War III strategic turns.
- **Economy:** `RtsEconomy.cs` provides finite ore, cargo loading/deposit, generated/used power, tech upgrades, and time-based production queues. `BlackAlertOreHarvester.cs` models a physical truck shuttling mined cargo between ore nodes and the refinery; economy credits increase on delivery, not a free counter.
- **Controls:** unit type double-tap, touch long-press drag squad selection, desktop box selection, formation offsets, pinch zoom/pan, selection rings; touch-command suppression over HUD.
- **Navigation:** `GridPathfinder.cs` deterministic A* cost and slope-aware routing, integrated into selected squads' and ground AI's move orders in the Unity battlefield.
- **Profile:** persistent local commander name, XP, wins, losses and level, optional private PNG/JPEG in app data. In-app Android gallery picker, secure online uploads and social profiles are **not** implemented.
- **Testing:** 53 assertions passed in a free .NET GitHub Actions job (https://github.com/echallita3-sudo/Black-Alert/actions/runs/40079364677) spanning mode results, career cards, 6-country campaign rules, economy, and A* navigation. Last Unity source structural checks pass.

**No new Unity Cloud/Android build has been started.** These C# changes have **not been compiled or run in Unity** and the Android-tested APK remains Build #1 (basic geometric smoke test). Static test success is not Unity compilation. User's 200 monthly free Unity build-minute quota has not been spent by this batch.

**Incomplete vision:** the original approved high-fidelity textured/rigged meshes, professional sound, touch-optimized graphical HUD, Android gallery picker, working online 2–8 player synchronization and servers, full World War III world geography/campaign, 8-player map variants and polished multiplayer game modes remain unfinished. The new Unity mode UI and effects are prototypes only. Protect free minutes; build at a meaningful milestone after source/asset integration and test readiness.

## 2026-10-24 — Phase 5 original geometry upgrade

### Live 3D art (source committed, not Android tested)
New C# assets on `main`:
- `BlackAlertMeshGeometry.cs`: original Unity triangulated custom angular armor bodies, tapered fuselages, convex wing/deck plates, and continuous curved crawler tracks; computed normals and shared meshes.
- `BlackAlertDetailedVehicleArt.cs`: rebuilt battle tanks with layered beveled hull, continuous track loops, separate road wheels, artillery turret yaw/cannon recoil; helicopters with tapered canopy, launcher pods, spinning main and tail rotors; modern swept-wing jets with stabilizers, twin engines and missiles; guided-missile destroyers with tapering hull/deck, superstructure, launcher cells, rotating radar and independent naval cannon. Runtime `BlackAlertVehicleAnimator` now animates mechanical pieces.
- `BlackAlertDetailedInfantryArt.cs`: soldier model with separate head, visor, helmet, body armor, backpack, rifle, articulated shoulders/elbows and hips/knees/boots, walk-cycle motion.
- `BlackAlertCombatEffects.cs`: reusable pooled world-space muzzle sparks, impact embers, smoky dust and nuclear blast debris.
- `Assets/Shaders/AnimatedRiverWater.shader`: per-pixel animated moving ripples; battlefield now creates the material dynamically.
- `BattlefieldPrototype.cs` **uses the new actual model constructors at spawn**, aims the new tank/warship turrets, fires from muzzle transforms, and invokes VFX. It now enables soft sun shadows, distance fog, and ambient illumination. Legacy block-model source remains for backwards compatibility but is not the spawned art.
- `Tools/UnitySyntaxChecks/` and `.github/workflows/unity-syntax.yml`: Roslyn checks of all Unity C# syntax in editor and Android configurations at zero Unity Cloud usage. Workflow ran successfully: https://github.com/echallita3-sudo/Black-Alert/actions/runs/40089999562

The **53 headless gameplay rules tests** from prior milestone also remain passing. Source checks and Android readiness succeed. **There has been no new Unity Cloud build or Android APK** during this batch.

### Important limits
These are procedurally generated custom 3D meshes, not images, but they are still **early production blockout art**. They are **not yet** visually equivalent to approved AAA concept paintings (no scanned photoreal textures, sculpted high-detail production characters, animation motion capture, final sound mixing, cinematic weather/VFX, complete graphics optimization, or skeletal runtime skinning). The new assets and shader have not been checked in the Unity editor and may reveal Unity compilation/render/physics bugs. Continue GitHub development and free checks; spend the limited Unity minutes only once there is enough asset/game integration for meaningful tests.


### Material/detail update

Custom runtime camouflage in `BlackAlertProceduralTextures.cs` now generates reusable 256x256 armored-vehicle paint patterns and terrain pebble/roughness maps. `BlackAlertMeshGeometry.cs` sets actual UV coordinates for those patterns; `BlackAlertDetailedVehicleArt.cs` assigns faction camouflage materials to mesh parts, and `BlackAlertTerrain.cs` applies repeated ground detail. Animated water and fog are also wired into live scene startup.

**Build 1 remains the only Android-tested APK.** No new Unity Cloud build has been triggered. These changes are saved as source and need actual Unity rendering/performance validation before claims about image quality are possible.

## 2026-10-24 — Phase 6 tactical intelligence / fog of war

- `Assets/Scripts/Simulation/FogOfWarGrid.cs`: per-team grid knowledge with **unknown / previously explored / currently visible** states, finite circular scout radius, and line-of-sight concealment behind terrain blockers. Opposing teams cannot read each other's fog snapshot.
- `Assets/Scripts/BlackAlertTacticalIntel.cs`: Unity-side update on a budgeted 0.36-second interval; visible player units reveal terrain, hidden enemy units are deactivated for rendering/selection, and a clickable minimap shows ground/water/terrain and only the enemy units currently scouted. A Fog toggle exists for source/testing.
- `BattlefieldPrototype.cs`: creates the intelligence manager, registers unit positions and ranges, enforces faction-based target detection (no universal enemy omniscience), and allows camera repositioning from the minimap.
- `Assets/Shaders/VertexColorTerrain.shader` and bundled `Assets/Resources/Shaders/TerrainFogBundled.shader`: the actual 3D terrain surface dims unknown tiles and partially lights remembered tiles. Animated water shader bundled in Resources as well to reduce Android runtime stripping risk.
- The headless game-rule tests now total **65 passing assertions** including fog memory, faction isolation, finite sight radius, and occluded targets: https://github.com/echallita3-sudo/Black-Alert/actions/runs/40095203356.
- Latest C# Roslyn syntax, Android-readiness and source checks also passed on `main` prior to this documentation update: https://github.com/echallita3-sudo/Black-Alert/actions/runs/40095572139.

**Reality check:** these are committed code and free non-Unity tests. The fog shader, material inclusion, minimap touch controls, enemy visibility, and real frame rate have **not been verified in Unity or on Android**. Build #1 is still the sole tested Android APK; no new Unity Cloud minutes were spent. Fog computation here is client-side and **must move to trusted server authority** before real multiplayer; this version should not be described as secure anti-cheat networking.

Still outstanding: production 3D art/textures/animation and optimized UI, proper line-of-sight for individual altitude/units, real LAN/internet multiplayer with up to 8 players, full country dataset and complete World War III map, and tested Android bundles/asset packs.

## 2026-10-24 — Phase 7 offline enemy commander and win conditions

This source-only milestone introduces real battle objectives and tactical enemy decisions:

- `Assets/Scripts/Simulation/TacticalCommanderBrain.cs` creates deterministic squad orders using **only visible enemy unit observations**, the commander's own army, its known headquarters positions, and available economy state. Unit types have distinct priorities: defensive reactions to approaching attackers, damaged infantry retreat, destroyers remain in navigable waterways, riflemen favor Battlefield sector capture, and Survival units approach the safe zone. AI starts with scouting and advances a sufficiently large force toward the opposing HQ after 65 simulation seconds.
- `BattlefieldPrototype.cs` wires the commander into live Unity unit orders and finite `RtsEconomy` production. The opponent is allocated a **separate** credits/technology/power ledger and ore supply; its *virtual harvester* is not yet a visible truck. Players still have the visual physical harvester. The AI checks every 4.5 seconds rather than every frame.
- `Assets/Scripts/Simulation/StrongholdBattle.cs`: per-faction command HQ hit points, damage, reset and a distinct match victor. HQ buildings in Unity now accept weapons/nuclear blast hits and produce rigidbody-based falling rubble upon destruction. `BlackAlertPlayableModes.cs` ends the battle when a headquarters is destroyed, while Survival mode ends when the final opposing troops have been eliminated. Completed matches stop producing extra troops/fighting and can be restarted using game-mode selection.
- Unity HUD displays enemy AI strategy/credits and the two HQ health levels.
- `Tools/GameRulesChecks/Program.cs`: **86 passing headless .NET C# tests** for prior game modes, fog, pathing, economy and the new tactical commander/HQ rules (https://github.com/echallita3-sudo/Black-Alert/actions/runs/40099483557). Unity C# Roslyn syntax and source/Android readiness checks succeeded on committed gameplay source. Free GitHub tests do not certify Unity API compilation.

**No new Unity Cloud build has been started and no new APK is available.** Build #1 remains the only Unity Android-tested version. These new systems require actual Unity compilation, Android testing and tuning, including terrain routing for HQ attacks, performance, player experience, and mobile usability. AAA art, production animation, multiplayer servers and full global World War III remain unfinished. Do not call this the finished game.
