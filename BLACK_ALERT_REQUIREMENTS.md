# BLACK ALERT — MASTER REQUIREMENTS (APPROVED)

This is the retained source of truth for the user's Black Alert vision. It describes features to ENGINEER, not features that already work. No mockup, splash image, reference GIF, or static PDF constitutes finished game art.

## 1. Core identity and platform
- New original real-time strategy game **Black Alert — Shadow Protocol**, inspired by the pacing, readability and responsive mechanics of classic Red Alert but not a copy of copyrighted game assets.
- **Native Unity 3D**, with tactical angled/isometric camera and high-fidelity real-time 3D unit/building models; not a WebView and not fake pre-rendered screenshots.
- Android landscape first, optimizable for a broad range of phone GPUs; computer development through Unity and Android Studio; repository is canonical source of truth.
- Installation may exceed 1 GB. APK plus validated external asset packs/OBB where appropriate, or Play Asset Delivery for store distribution. Do not ship broken/untested APKs.

## 2. Approved graphics standard
- Match the approved detailed battlefield, face-to-face duel, Survival and World War III mockups **in actual gameplay**: clear silhouettes, rich textures, convincing lighting, terrain and effects without unreadable clutter.
- Original modeled/rigger soldiers (walking, sprinting, aiming, reload, kneeling, climbing slopes, hit and death), tanks and artillery (animated tracks/suspension, hull and turret separate, gun recoil, smoke, debris), aircraft (rotor blur, banking, altitude, missile trails), ships (wakes, mass/inertia), and realistic structures.
- Buildings: headquarters, reactor/power plant, refinery, harvester docks, war factory, barracks, radar, airfield/helipad, naval yard, repair bay, defense turrets, tech structures, superweapons, shields. Each has operating mechanisms, construction phases, damage stages and destruction animation.
- Projectiles physically visualized with muzzle flashes, casings, tracer/shell trajectories, fragmentation, volumetric-looking smoke, fires, scorches, crater decals, weather and shadows.
- Nuclear strike: siren, incoming missile, terminal trajectory, huge explosion/glow, pressure ring, debris/smoke, ground aftermath, gameplay area effect and dramatic sound; support sensible graphics settings/accessibility for flashes.
- Full game audio: faction and commander voices, tank cannons, rifles, rotor sound, engines, footsteps terrain-dependent, artillery, explosions, spatial attenuation, adaptive music and interface cues. Professionally mixed sound is required, not just synthesized placeholders.
- Touch UI is an animated military command interface: production cards with actual unit portraits, radar/minimap, team colored controls, alerts, progress rings, power/credits, selection groups, abilities and options.

## 3. Unit and terrain physics
- Infantries have climbing angles, motion poses and stride cadence. Mountains genuinely change reachable paths, visibility and movement speed.
- Rivers, fords, mud, sand, snow and roads have separate movement penalties/advantages; water depth blocks non-amphibious vehicles, crossing creates splashes; bridges are chokepoints.
- Tracked and wheeled vehicles have differentiated acceleration, traction, turning radius, suspension and hull response.
- Ships, subs and hover/amphibious craft operate in appropriate water regions; heavy ships steer/accelerate more slowly than patrol ships and produce scale-dependent wakes.
- Helicopters and jets have separate air behavior, velocity, height changes, fuel/range, bank/roll and air defenses; aircraft cannot drive as tanks.
- Deterministic or authoritative server-validated physics/gameplay for online matches; visible FX may interpolate smoothly independent of network tick.

## 4. Main RTS
- Two original launch factions Azure Dominion and Crimson Forge; option to expand to additional well-distinguished factions (such as Obsidian Veil).
- Tech tree, base construction placement, power supply, harvesters/refineries, resources, upgrades, queues, rally points, repair, fog of war, radar, unit abilities and faction balance.
- Unit selection single/double tap, drag-box selection, long-press radial commands, attack/move/guard/stop/patrol, control groups, formation orders, pinch zoom, two-finger pan, minimap camera jump, touch-friendly tap targets.
- Enemy AI with sensible strategy, map objectives, multiple difficulty settings, threat assessment, resource planning and retreat/attack behavior.
- Match must not end prematurely: fair starting setup, victory/defeat conditions, suitable spawn protection and sufficient resources.

## 5. Five full game modes
1. **Skirmish:** solo vs AI or humans, 1v1 through 8 players, FFA/team and ranked/friendly variants. Detailed 2P/4P/6P/8P maps with meaningful mountains, oceans, rivers, roads, resources, obstacles, spawn symmetry and viable naval/air routes.
2. **Battlefield:** large sectors, control points, tickets, bonuses, reinforcement depots, multiple fronts and coordinated teamwork.
3. **Survival / Last Stand:** survive against other 2–8 players/teams until last commander/team standing. Optional AI threat waves/encroaching danger zone and spectator mode. Elimination must be fair and well signaled.
4. **Face-Off Duel:** a visually close-camera, mirrored face-to-face one-on-one arena. One commander uses **rolling tactical cards** unlocked via career progression; the opponent uses direct unit/action controls. Cards require clear costs, cooldowns, rarity/progression and balanced matchmaking. Original fire support, shield, EMP and artillery cards, satisfying animated card reel.
5. **World War III:** ambitious global strategic conquest, inspired by Risk but using Black Alert's own map/3D graphics. All countries/territories represented by structured geographical data, borders, strategic movements, supply/logistics, nation relations/diplomacy, alliances, nuclear deterrence, naval/air transport, campaigns, and tactical live RTS battles for disputed territories. Mobile-friendly globe/map controls.

## 6. Profiles, online and social
- Accounts with username, **user's own uploaded profile picture**, career level/rank, experience, units/cards progression, statistics, battles played, wins/losses and match history.
- Validate/resize profile uploads, apply privacy/security protections and allow removing a photo.
- Worldwide hosted multiplayer independently of PocketRA: public/private lobbies, room codes, map/team/faction picker, ready states, chat, voice, friends/invites, reconnection, anti-cheat/server validation, desync recovery and replay/spectate. Scalable 2,4,6,8 humans online after measured synchronization testing.
- Offline Solo mode and LAN test option may be delivered earlier, but are **not a substitute** for online public matches.

## 7. Honest delivery gates
- G0 Unity imports project, C# compiles, camera sees scene.
- G1 genuine official Unity Android export, APK installs, starts and displays HUD and 3D models on target phone.
- G2 quality game combat slice with 2 full factions, movement, damage, economy, animations, and audio, with screenshot/video from actual gameplay.
- G3 fully navigable 6P/8P maps and capable AI, tested for performance.
- G4 stable online server/live combat/session recovery.
- G5 all special modes and profile services verified.
- G6 World War III worldwide campaign persistence.
- G7 full graphics fidelity, polishing, performance/asset pack distribution.
- Static source tests or signing are NOT equivalent to Unity compile, Android gameplay validation or worldwide multiplayer verification. Never claim otherwise.

## 8. Status
This specification is locked; it is **not** a claim these features already exist. See STATUS.md for actual implementation and verification state.
