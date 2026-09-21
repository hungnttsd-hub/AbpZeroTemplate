# CODEX MASTER PROMPT

Copy/paste the prompt below into Codex from the repository root.

---

You are implementing the MVP of a children's English-learning adventure game named **Wordy Wings**.

Read these files before changing code:

1. `README.md`
2. `01_GDD.md`
3. `02_TECH_ARCHITECTURE.md`
4. `03_CODEX_MVP_SPEC.md`
5. `backend/abp_domain_model.md`
6. `frontend/phaser_architecture.md`
7. `qa/acceptance_criteria.md`
8. `data/mvp_levels_30.json`
9. `schemas/level.schema.json`

## Non-negotiable architecture

- Backend: ABP.io / ASP.NET Core / EF Core / PostgreSQL.
- Auth: ABP Identity + OpenIddict.
- Frontend: React + TypeScript.
- Gameplay: Phaser isolated behind a React `GameContainer`.
- Content: data-driven JSON/API; never hard-code individual levels in source files.
- Mobile future: preserve the React + Phaser game so it can be wrapped by Capacitor; do not introduce browser-hostile dependencies into game core without an adapter.
- Assets: use asset keys/base URL; never persist uploads to local Render filesystem.

## Product constraints

The player is around six years old. Gameplay must be understandable primarily by audio, pictures and animation. No lives, no game-over punishment, no loot boxes, no ads in child mode. Wrong answers should teach and allow immediate retry.

## First task

Inspect the existing repository and adapt to its current ABP version/template. Do not overwrite existing infrastructure blindly. Then produce a short implementation checklist and start implementing M1 immediately.

If the repository is empty, scaffold the current supported ABP React solution using the installed/current ABP tooling. Do not guess obsolete CLI flags; check local help/tooling. Keep the generated ABP project boundaries intact.

## Implementation order

1. Domain entities and EF mappings for MVP.
2. Database migration and seed from `data/mvp_levels_30.json`.
3. Application contracts/services and REST endpoints.
4. React child selection + world map routes.
5. Phaser bootstrap + `LevelDefinition` parsing.
6. Implement `word_shot` first end-to-end.
7. Attempt persistence + star/unlock logic.
8. Implement remaining MVP mechanics.
9. Local IndexedDB pending-attempt queue.
10. Parent dashboard.
11. Automated tests.
12. Render deployment files/config documentation.

## Code quality

- TypeScript strict mode.
- Small testable services; avoid giant Phaser scenes.
- No `any` in shared game contracts unless unavoidable and documented.
- Validate level JSON at runtime before launching a scene.
- All network calls behind a service/repository abstraction.
- All storage behind an adapter interface.
- Make sync idempotent using client-generated attempt IDs.
- Log technical detail server-side, show child-friendly errors in game.

## Definition of done

A parent can authenticate, create/select a child, enter World 1, play all MVP mechanics, complete levels, see stars and unlocks persist, close/reopen and retain progress, and view a basic dashboard. Temporary API failure must not erase a completed attempt; it should sync later.

Work iteratively and keep the application runnable after each milestone. Never claim completion without running the relevant tests/builds.
---
