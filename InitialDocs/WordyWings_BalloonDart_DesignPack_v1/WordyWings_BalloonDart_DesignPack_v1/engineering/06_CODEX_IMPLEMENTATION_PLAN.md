# 06 – Codex Implementation Plan

## Objective

Replace/refactor the current static “tap a card” implementation for this mechanic with a reusable **moving balloons + toy dart shooter** implementation while preserving the current ABP.io + React + Phaser project architecture.

## Phase 0 – inspect before editing

Codex must first:

1. Identify the current level scene/component that renders W01-L08.
2. Find the existing level JSON/config model.
3. Find audio manager, asset loader, progress/star system, responsive scale strategy, and event bus.
4. Find the current physics setup.
5. List reusable classes and files before creating new ones.
6. Avoid replacing working systems when extension is sufficient.

## Phase 1 – data contract

Add `balloon_dart` to the mechanic type union/enum.

Add typed config equivalent to `engineering/08_TYPESCRIPT_CONTRACTS.ts`.

Validate level JSON at dev/test time against `data/balloon-dart.schema.json` or the project’s existing validation layer.

## Phase 2 – scene/mechanic skeleton

Implement lifecycle:

```text
load config
→ preload required assets
→ create launcher
→ create round 1 balloons
→ play instruction audio
→ enable input
→ resolve shots
→ complete round
→ repeat x3
→ complete level
→ send summarized progress
```

Do not hard-code `circle`, `ball`, `star`, or `flower` in logic.

## Phase 3 – launcher and aiming

- Fixed launcher anchor at bottom-center for canonical layout.
- Aim angle clamped to upper playfield.
- Dotted aim line.
- Drag/release fires.
- Add tap-to-shoot mode behind config/accessibility setting.
- Prevent double fire while active dart/cooldown state blocks it.

## Phase 4 – balloon movement

Implement reusable path strategies:

- gentleBob
- driftHorizontal
- driftVertical
- figureEight
- crossLane

Movement must accept seeded phase/offset values so tests can be deterministic.

## Phase 5 – collision and feedback

Correct:

- lock current round
- pop target
- word audio
- star fill
- character celebration
- spawn next round

Wrong:

- bounce balloon
- say struck vocabulary item
- keep round active

Miss:

- despawn/recycle dart
- keep round active

## Phase 6 – hints

Implement thresholds from design spec. Hint system must be independent of target content.

## Phase 7 – responsive/mobile

Verify at:

- 1440×810
- 1280×720
- 1024×768-ish container with letterboxing
- 844×390 mobile landscape

Ensure safe-area padding works under Capacitor if the project already exposes safe-area variables.

## Phase 8 – progress/event integration

Reuse existing progress endpoint/service. Do not add backend endpoint unless current one cannot represent mechanic summary.

Emit local events defined in this pack.

## Phase 9 – tests

Add tests for:

- config validation
- target matching
- compound attributes (`blue` + `circle`)
- wrong-hit non-destruction
- round advances only after correct hit
- exactly one star per completed round
- restart cleans all listeners/tweens/bodies
- deterministic seeded motion where applicable

## Phase 10 – cleanup

- Remove the old static-card implementation only when no other levels use it.
- If it is a shared mechanic, leave it intact and map only specified levels to `balloon_dart`.
- No duplicated event listeners after scene restart.
- No orphan physics bodies/tweens.
- No console errors.
