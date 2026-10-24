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
