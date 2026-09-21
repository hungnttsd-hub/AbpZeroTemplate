# CODEX MVP SPEC — 30 LEVELS

## Mission

Build a working MVP of **Wordy Wings** using the agreed architecture: **ABP.io + React + TypeScript + Phaser + PostgreSQL**, deployable to Render, with future **Capacitor** mobile reuse.

The MVP is not a mock. It must be playable end-to-end with placeholder/original SVG assets and audio fallbacks.

## Functional scope

### Parent / app shell

1. Register/login parent via ABP/OpenIddict.
2. Create/select one or more child profiles: nickname, avatarKey, ageBand.
3. World map showing three MVP worlds and locked/unlocked state.
4. Open a level.
5. Return from Phaser to map after completion.
6. Parent dashboard: levels completed, stars, vocabulary attempted, review list.

### Game

Implement these mechanics:

- `word_shot`
- `balloon_pop`
- `drag_sort`
- `letter_puzzle`
- `boss_challenge`

The other two mechanics must exist as TypeScript interface/registry placeholders only, not production gameplay in MVP.

### Progress

- Save attempt on level completion.
- Unlock next level in same world.
- Unlock next world after MVP boss of previous world.
- Reload app and preserve state.
- Queue attempt locally if request fails, retry later.

## UX rules

- Landscape gameplay baseline 16:9; responsive down to phone landscape.
- Every instruction has speaker replay control.
- No game-over/lives.
- Wrong answer: gentle bounce/shake + pronounce selected item, then retry.
- Hint after two wrong attempts.
- Main touch targets large; no critical hover-only action.
- Pause overlay provides Resume / Restart / Exit Level.

## MVP level list

| ID | Mechanic | Instruction | Target | Difficulty |
|---|---|---|---|---|
| W01-L01 | word_shot | Find the red. | red | D1 |
| W01-L02 | balloon_pop | Pop the blue. | blue | D1 |
| W01-L03 | drag_sort | Put the yellow in the right group. | yellow | D1 |
| W01-L04 | word_shot | Find the green. | green | D1 |
| W01-L05 | letter_puzzle | Build the word ORANGE. | orange | D1 |
| W01-L06 | word_shot | Find the pink. | pink | D2 |
| W01-L07 | drag_sort | Put the purple in the right group. | purple | D2 |
| W01-L08 | balloon_pop | Pop the circle. | circle | D2 |
| W01-L09 | word_shot | Find the square. | square | D2 |
| W01-L10 | boss_challenge | Boss review: find triangle and finish the Word Star challenge. | triangle | D2 |
| W02-L01 | word_shot | Find the cat. | cat | D1 |
| W02-L02 | balloon_pop | Pop the dog. | dog | D1 |
| W02-L03 | drag_sort | Put the bird in the right group. | bird | D1 |
| W02-L04 | word_shot | Find the fish. | fish | D1 |
| W02-L05 | letter_puzzle | Build the word DUCK. | duck | D1 |
| W02-L06 | word_shot | Find the cow. | cow | D2 |
| W02-L07 | drag_sort | Put the horse in the right group. | horse | D2 |
| W02-L08 | balloon_pop | Pop the rabbit. | rabbit | D2 |
| W02-L09 | word_shot | Find the frog. | frog | D2 |
| W02-L10 | boss_challenge | Boss review: find monkey and finish the Word Star challenge. | monkey | D2 |
| W03-L01 | word_shot | Find the bed. | bed | D1 |
| W03-L02 | balloon_pop | Pop the chair. | chair | D1 |
| W03-L03 | drag_sort | Put the table in the right place. | table | D1 |
| W03-L04 | word_shot | Find the sofa. | sofa | D1 |
| W03-L05 | letter_puzzle | Build the word LAMP. | lamp | D1 |
| W03-L06 | word_shot | Find the clock. | clock | D2 |
| W03-L07 | drag_sort | Put the mirror in the right place. | mirror | D2 |
| W03-L08 | balloon_pop | Pop the door. | door | D2 |
| W03-L09 | word_shot | Find the window. | window | D2 |
| W03-L10 | boss_challenge | Boss review: find toy and finish the Word Star challenge. | toy | D2 |

Source of truth: `data/mvp_levels_30.json`.

## Phaser architecture

```text
GameBootstrap
  SceneRegistry
    BootScene
    PreloadScene
    LevelScene
    ResultScene
  MechanicRegistry
    WordShotMechanic
    BalloonPopMechanic
    DragSortMechanic
    LetterPuzzleMechanic
    BossChallengeMechanic
  Services
    LevelContentService
    AudioService
    AssetService
    AttemptTracker
    HintService
    GameEventBus
```

`LevelScene` must not branch on world-specific IDs. It loads a `LevelDefinition` and delegates to a mechanic implementation.

## Required shared TypeScript contracts

```ts
export type MechanicId =
  | 'word_shot'
  | 'balloon_pop'
  | 'drag_sort'
  | 'letter_puzzle'
  | 'rescue_mission'
  | 'adventure_commands'
  | 'boss_challenge';

export interface LevelDefinition {
  id: string;
  worldId: string;
  order: number;
  title: string;
  mechanic: MechanicId;
  difficulty: 1 | 2 | 3 | 4;
  targetVocabulary: string[];
  reviewVocabulary: string[];
  instruction: string;
  instructionAudioKey: string;
  targets: Array<{ value: string; correct: boolean; assetKey?: string }>;
  hintPolicy: { afterWrongAttempts: number; audioReplay: boolean; visualPulse: boolean };
  estimatedSeconds: number;
  learningObjective: string;
  isBoss: boolean;
}
```

## Placeholder art policy

MVP must not depend on external copyrighted game assets. Generate simple original SVG/shape assets for:

- fruit, shapes, basic animals, furniture;
- Poki/Pip/Momo simplified avatar tokens;
- world backgrounds as layered gradients + original vector decoration.

Create an asset mapping layer so production assets can replace placeholders without changing mechanics.

## Audio

Interface:

```ts
interface AudioService {
  playInstruction(level: LevelDefinition): Promise<void>;
  speakWord(word: string): Promise<void>;
  stopAll(): void;
}
```

Order:

1. Try packaged/cached audio asset.
2. If missing in dev/MVP, use browser `speechSynthesis` English voice.
3. Never block gameplay forever if audio fails.

## Backend entities

See `backend/abp_domain_model.md`. Minimum MVP entities:

- ChildProfile
- GameWorld
- GameLevel
- VocabularyTerm
- LevelAttempt
- WordMastery
- PlayerProgress

Content may initially be seeded from JSON into DB.

## MVP admin

Admin UI can be basic CRUD. Must support:

- world list;
- level list/filter by world;
- edit instruction, difficulty, mechanic, publish flag;
- validate a level before publish.

Do not build a visual level editor in MVP.

## Error handling

- Missing level -> friendly error + Back to Map.
- Missing target asset -> generic labeled placeholder.
- Missing audio -> speech synthesis fallback.
- API offline -> queue attempt locally.
- Expired auth -> preserve pending attempt, refresh/login, sync afterward.

## Testing

At minimum:

- unit test level schema parsing;
- unit test unlock calculation;
- unit test star calculation;
- unit test idempotent sync DTO mapping;
- Playwright/e2e: login -> select child -> play one level -> complete -> map shows next level unlocked;
- responsive smoke test desktop and mobile landscape viewport.

## Deliver in milestones

### M1 — Skeleton

ABP entities/API + React routes + Phaser canvas boot + load JSON level.

### M2 — Core mechanic

Word Shot fully playable, attempt save, stars, result screen.

### M3 — 4 more mechanics

Balloon Pop, Drag Sort, Letter Puzzle, Boss Challenge.

### M4 — 30 levels

Seed 30 definitions, world map/unlocking, dashboard.

### M5 — Offline/resilience

IndexedDB queue, retries, audio fallback, missing asset fallback.

### M6 — Deploy

Docker backend on Render, React static site on Render, PostgreSQL connection, environment configs.

## Acceptance

Use `qa/acceptance_criteria.md` as release gate. Do not declare MVP done while any P0 acceptance item fails.
