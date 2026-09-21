# 05 – Phaser Architecture

## Principle

Do not create a separate hard-coded scene for each vocabulary item. Implement Balloon Dart as a reusable mechanic driven entirely by level JSON.

Prefer integrating into the project’s existing generic `LevelScene` / mechanic factory. If no generic level scene exists, create one reusable `BalloonDartScene` only.

## Suggested module

```text
src/game/mechanics/balloon-dart/
├── BalloonDartController.ts
├── BalloonManager.ts
├── BalloonTarget.ts
├── LauncherController.ts
├── AimController.ts
├── Dart.ts
├── DartPool.ts
├── CollisionResolver.ts
├── RoundController.ts
├── HintController.ts
├── BalloonDartEffects.ts
├── BalloonDartEvents.ts
├── BalloonDartTypes.ts
└── index.ts
```

Adapt filenames to current project conventions instead of duplicating equivalent classes.

## Responsibility split

### `BalloonDartController`
Coordinates mechanic lifecycle. Does not own rendering primitives directly when a lower-level object can.

### `BalloonManager`
Creates/positions balloons, applies movement paths, maintains safe spacing, and resets between rounds.

### `BalloonTarget`
One balloon instance with semantic payload, physics body, visual state, label/icon, and movement state.

### `LauncherController`
Visual launcher, barrel rotation, recoil, reload state.

### `AimController`
Pointer/touch input, angle clamp, aim guide, tap-to-shoot alternative.

### `DartPool`
Object pool for darts. Never create/destroy large numbers of projectile objects every shot.

### `CollisionResolver`
Maps dart↔balloon collision to correct/wrong/miss behavior. Must compare semantic IDs/attributes, not sprite names.

### `RoundController`
Runs target prompt, round start/end, 3-round progression, star fill.

### `HintController`
Counts errors/inactivity and activates replay/glow/guide hints.

## Physics recommendation

Preserve the existing project physics convention.

If the project already uses **Matter**:

- `gravity.y = 0` for this mechanic.
- Balloon bodies can be circle/ellipse sensors or lightweight static/kinematic-style bodies whose position is driven by path motion.
- Darts are dynamic bodies with velocity along aim vector.
- Use collision categories so darts only collide with balloons/world bounds relevant to this mechanic.

If the current game already supports **Arcade Physics** as a first-class system, Arcade is sufficient and simpler. Do not introduce a second physics stack solely for this screen unless there is a clear project-level reason.

## React boundary

React owns:

- route/app shell
- account/session
- world map
- parent dashboard
- page-level loading/error shell

Phaser owns:

- balloons
- projectile
- aim input
- collisions
- animation
- round HUD inside the game canvas

Do not render moving balloon gameplay as React DOM elements.

## API boundary

No API call per shot.

At level completion send one summarized payload, for example:

```json
{
  "levelId": "W01-L08",
  "mechanic": "balloon_dart",
  "roundsCompleted": 3,
  "shots": 7,
  "wrongHits": 2,
  "misses": 2,
  "hintCount": 1,
  "durationSeconds": 68,
  "stars": 3
}
```

Existing offline/checkpoint infrastructure should be reused.

## Performance targets

- Desktop: 60 FPS target.
- Mid-range mobile: ≥ 30 FPS target.
- Pool darts and pop particles.
- Maximum active balloons MVP: 5.
- Maximum pop particles per event: 14.
- Do not load assets for all 200 levels on startup.
