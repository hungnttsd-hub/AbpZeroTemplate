# REACT + PHASER ARCHITECTURE

## React ownership

Routes:

```text
/login
/children
/map/:childId
/play/:childId/:levelId
/parent/:childId/dashboard
/admin/content
```

`/play/...` renders `GameContainer` and hands a validated `LevelDefinition` + services into Phaser.

## Phaser contract

```ts
interface GameMechanic {
  readonly id: MechanicId;
  mount(ctx: MechanicContext, level: LevelDefinition): Promise<void> | void;
  dispose(): void;
}
```

`MechanicContext` exposes only:

- Phaser scene;
- audio service;
- asset resolver;
- attempt tracker;
- hint controller;
- event bus.

Mechanics do not call REST APIs directly.

## Event bridge

Phaser -> React events:

- `LEVEL_READY`
- `LEVEL_COMPLETED`
- `LEVEL_EXIT_REQUESTED`
- `GAME_ERROR`

React -> Phaser commands:

- `PAUSE`
- `RESUME`
- `RESTART`
- `MUTE_CHANGED`

## Scaling

Use Phaser Scale Manager with FIT + CENTER_BOTH around a logical 1600x900 gameplay space. UI safe area margins must account for small landscape phones and later Capacitor safe-area insets.

## Mechanics MVP notes

### Word Shot

Use drag pointer to set launch vector, preview dotted trajectory, release. Physics can use Matter or simpler ballistic tween in early MVP. Educational correctness is based on target collision, not physical destruction.

### Balloon Pop

Spawn 3–5 balloons with stable readable target images. Tap/click to pop. No harsh timer in first 10 levels.

### Drag & Sort

Pointer drag object to large destination zone. Provide click-select + click-destination fallback if drag becomes inaccessible.

### Letter Puzzle

Letter tiles are shuffled but target word remains 3–8 letters in MVP. After two errors, first correct tile pulses.

### Boss Challenge

Orchestrator runs 4 micro-rounds using existing mechanic modules. One completion payload; checkpoints inside boss prevent full reset.

## Asset resolution

```ts
interface AssetResolver {
  image(key: string): string;
  audio(key: string): string;
}
```

All production URLs derive from environment/config `ASSET_BASE_URL`.
