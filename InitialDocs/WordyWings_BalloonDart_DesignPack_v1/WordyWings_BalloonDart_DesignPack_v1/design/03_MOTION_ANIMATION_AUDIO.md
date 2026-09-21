# 03 – Motion, Animation & Audio Specification

## Balloon idle motion

- Scale breathing: 0.99 ↔ 1.01, 2.2–3.8 s.
- Rotation: ±1.5° max.
- String sway: 3–6°.
- Motion patterns should have randomized phase offsets but deterministic seeded values per round.

## Launcher

- Barrel rotation follows aim with smoothing (`lerp` / damp).
- Character eyes/head may follow aim target.
- Fire recoil: 5–10 px / 80–120 ms.
- Reload bounce: 100–160 ms.

## Dart

- Small rotation in flight aligned to velocity vector.
- Trail: 3–5 pooled particles, lifespan 120–200 ms.
- No screen shake on miss.
- Correct pop: optional tiny camera bump ≤ 1.5 px, 80 ms; disable under reduced motion.

## Correct-pop sequence

Recommended timeline:

- T+0 ms: collision detected, input lock for current round.
- T+0–90: balloon squash.
- T+90–180: dart embed/contact beat.
- T+150–420: balloon pop + 8–14 soft fragments/particles.
- T+180: SFX `balloon_pop_soft`.
- T+250: target voice starts: “Circle!”
- T+350–700: star flies/fills progress slot.
- T+450–900: mascot celebration.
- T+900–1300: next-round spawn begins.

## Wrong-hit sequence

- Balloon squash to 0.90–0.94, 80 ms.
- Elastic rebound to 1.03, then 1.00 within 220 ms.
- Dart rotates away and fades within 300–450 ms.
- SFX `boing_soft`.
- Voice says struck item name.
- Round remains active.

## Audio priority

1. Safety/system modal audio
2. Target instruction
3. Correct target word
4. Wrong target word
5. Supportive phrases
6. SFX
7. Ambient/music

Never overlap two voice lines.

## Required audio assets

- `ui_instruction_replay`
- `dart_fire_soft`
- `dart_whoosh`
- `balloon_pop_soft`
- `balloon_wrong_boing`
- `star_fill`
- `round_complete`
- target instruction voice lines
- vocabulary word voice lines

## Music

- 70–100 BPM playful loop.
- Keep music 12–18 dB below spoken instruction.
- Duck music by ~6 dB during voice playback.

## Reduced motion

When `prefers-reduced-motion` or game accessibility setting is enabled:

- Remove camera bump.
- Reduce balloon path amplitude by ~35%.
- Replace fragment burst with opacity/scale fade.
- Keep gameplay readable and fully functional.
