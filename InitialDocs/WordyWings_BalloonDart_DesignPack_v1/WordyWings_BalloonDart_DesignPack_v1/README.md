# Wordy Wings – Balloon Dart Design Pack v1

Production specification for upgrading the current **“Pop the circle”** screen into a moving-balloon + dart-shooter learning mechanic.

## Canonical gameplay

- The child hears an English instruction such as **“Pop the circle.”**
- 3–5 balloons drift through the playfield.
- Each balloon carries a picture/shape/word option.
- A launcher at the bottom aims a soft toy-like dart.
- Drag / move pointer to aim, then release / tap to fire.
- Correct balloon pops with a soft burst, word audio, character celebration, and one progress star.
- Wrong balloon does **not** punish the child: it squashes/bounces, speaks its own word, and remains in play.
- A level contains 3 short rounds. Completing each round fills one star, so finishing the level always feels successful.

## Stack assumptions

- Backend: ABP.io / ASP.NET Core
- Frontend shell: React + TypeScript
- Game: Phaser
- Existing physics: preserve project convention; this pack uses a Matter-compatible design and does not require architectural rewrite
- Data: JSON-driven levels
- Mobile path: Capacitor

## Start here

1. `design/01_VISUAL_DESIGN_SPEC.md`
2. `design/02_GAMEPLAY_SPEC.md`
3. `engineering/06_CODEX_IMPLEMENTATION_PLAN.md`
4. `engineering/07_CODEX_MASTER_PROMPT.md`
5. `data/demo-levels.json`
6. `qa/acceptance_criteria.md`

## Included references

- `references/current-screen.png` – current implementation supplied by the user.
- `references/selected-balloon-dart-concept.png` – selected visual direction from this conversation.

The concept image is a visual direction, not pixel-perfect implementation geometry. Follow the numeric layout and behavior specs in this pack.
