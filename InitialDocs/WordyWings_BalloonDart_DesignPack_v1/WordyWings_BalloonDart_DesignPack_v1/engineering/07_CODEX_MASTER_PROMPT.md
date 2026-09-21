# 07 – Codex Master Prompt

Copy the text below into Codex from the repository root.

---

You are upgrading the existing Wordy Wings game. The project architecture is already decided:

- Backend: ABP.io / ASP.NET Core
- Frontend shell: React + TypeScript
- Gameplay: Phaser
- Database: PostgreSQL
- Levels are data-driven JSON
- Future native packaging: Capacitor

Do NOT rewrite the architecture.

Your task is to upgrade the existing static vocabulary screen represented by W01-L08 (“Pop the circle”) into the reusable mechanic **`balloon_dart`**.

## Gameplay target

The player hears an instruction such as “Pop the circle.” Several balloons move gently in the playfield. Each balloon displays a candidate picture/shape/word. A toy launcher at the bottom fires a soft dart. The player aims and fires at the correct moving balloon.

The interaction should feel like a classic child-friendly launcher/bubble-shooter aiming game, but all UI, art, code, level design and behavior must remain original to Wordy Wings.

### Correct hit

- target balloon squashes briefly
- pops with friendly VFX
- plays the vocabulary word
- character celebrates
- one progress star fills
- next round begins

### Wrong hit

- wrong balloon does NOT disappear
- it squashes/rebounds
- dart bounces/fades
- play the struck item’s word
- supportive feedback only
- no life loss, no red X, no game over

### Miss

- dart leaves bounds and is recycled
- no punishment

A level contains 3 rounds; one completed round fills one star. Finishing all rounds always yields 3 progress stars. Accuracy is analytics only.

## First action: inspect the current project

Before writing code:

1. Locate current W01-L08 implementation.
2. Locate existing level config schema/types.
3. Locate Phaser scene architecture/mechanic factory.
4. Locate audio manager/queue.
5. Locate progress/star system.
6. Locate event bus.
7. Locate current physics configuration.
8. Locate scale/responsive strategy.
9. Report which existing classes/files will be reused, which will be extended, and which new files are required.

Do not create duplicate systems when equivalent infrastructure already exists.

## Required mechanic capabilities

Implement a generic `balloon_dart` mechanic supporting:

- 3–5 balloons
- moving targets
- `gentleBob`
- `driftHorizontal`
- `driftVertical`
- `figureEight`
- `crossLane`
- drag-to-aim + release-to-fire
- optional tap-balloon-to-fire accessibility mode
- dotted trajectory preview
- toy dart object pooling
- correct/wrong/miss collision behavior
- progressive hints
- audio queue integration
- 3-round star progress
- responsive landscape layout
- clean restart/scene teardown

## Important visual behavior

The correct balloon MUST NOT glow before hint mode activates. Do not accidentally reveal the answer.

Balloon collision/hit area should be forgiving and roughly match the visible balloon body.

Use a bottom-center launcher in the canonical implementation unless the current project has a strong reusable layout abstraction that makes another anchor preferable.

## Physics

Preserve current project convention.

If Matter is already the project standard, use Matter with no gravity for this mechanic and appropriate collision categories/sensors. Do not introduce Arcade Physics just for this screen.

If Arcade Physics is already supported by the project, it is acceptable.

## Data-driven requirement

No target-specific code such as:

```ts
if (word === 'circle') { ... }
```

or level-ID-specific gameplay branches.

All content comes from JSON.

Use `data/balloon-dart.schema.json` and `data/demo-levels.json` from this design pack as the desired contract; adapt to existing project conventions where appropriate.

## Architecture guidance

Prefer a reusable module conceptually equivalent to:

```text
src/game/mechanics/balloon-dart/
  BalloonDartController
  BalloonManager
  BalloonTarget
  LauncherController
  AimController
  DartPool
  CollisionResolver
  RoundController
  HintController
  BalloonDartEffects
  BalloonDartTypes
```

Do NOT create these files blindly if equivalent existing abstractions already exist.

## Input rules

Primary:

- drag/move to aim
- release to fire
- clamp launcher to upper playfield

Accessibility:

- tap balloon → launcher rotates toward it → fires

Touch and mouse must both work.

## Hint rules

- error 1: no answer reveal
- error 2: replay instruction once
- error 3: target gets subtle warm halo pulse
- error 5: short general direction guide

No auto-completion.

## API/progress

Do not call backend for each dart.

On level completion send/reuse one summary payload containing at least:

- levelId
- mechanic
- roundsCompleted
- shots
- wrongHits
- misses
- hintCount
- durationSeconds
- stars

Reuse existing ABP application service/API whenever possible.

## Performance

- 60 FPS desktop target
- >=30 FPS mid-range mobile
- pool darts and frequent particles
- max 5 balloons in MVP
- avoid loading assets for unrelated levels

## QA requirements

Implement/test:

1. config-driven targets
2. correct hit advances round once
3. wrong hit never destroys the wrong balloon
4. miss does not advance round
5. each completed round fills exactly one star
6. 3 rounds complete level
7. compound target matching works
8. repeated restart does not duplicate listeners/tweens/bodies
9. desktop mouse works
10. mobile touch works
11. 844×390 landscape is usable
12. audio lines never overlap
13. reduced-motion setting remains playable
14. no console errors
15. existing mechanics/tests continue to work

## Deliverable

After implementation, report:

- changed/new files
- architecture decisions
- how to add a new Balloon Dart level using JSON only
- how to add a new balloon movement pattern
- how to add a new semantic target type
- how to test desktop/mobile
- known limitations

Before coding, read the accompanying files in this pack, especially:

- `design/01_VISUAL_DESIGN_SPEC.md`
- `design/02_GAMEPLAY_SPEC.md`
- `engineering/05_PHASER_ARCHITECTURE.md`
- `data/balloon-dart.schema.json`
- `data/demo-levels.json`
- `qa/acceptance_criteria.md`

Implement incrementally and preserve existing working architecture.
