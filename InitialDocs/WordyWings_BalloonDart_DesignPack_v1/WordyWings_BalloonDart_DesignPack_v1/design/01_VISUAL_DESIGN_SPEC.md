# 01 – Visual Design Specification

## 1. Design goal

Upgrade the current quiet flashcard screen into a **playful shooting range** while preserving the calm, child-friendly Wordy Wings visual language. The screen must read as a game first and a vocabulary exercise second.

The visual hierarchy must always be:

1. **Instruction / target word**
2. **Moving balloons**
3. **Launcher + aim feedback**
4. **Progress / pause / replay audio**
5. Decorative environment and mascot

Do not let scenery compete with the answer choices.

## 2. Base canvas

Canonical design canvas: **1440 × 810 (16:9)**.

Use a camera/world that scales proportionally. Gameplay coordinates should be normalized so the same config works at desktop, tablet landscape, and mobile landscape.

### Safe zones at 1440 × 810

| Zone | Bounds | Purpose |
|---|---:|---|
| Top HUD | y 16–126 | Pause, prompt, audio, progress |
| Balloon playfield | x 90–1350, y 155–575 | Moving answer targets |
| Launcher zone | x 510–930, y 590–795 | Launcher, character, aiming origin |
| Bottom hint | x 440–1000, y 748–800 | Temporary tutorial text only |

Balloons must never travel under the prompt panel or through the launcher zone.

## 3. Final screen composition

### Top-left

- Pause button: 52–60 px visual size, ≥ 48 px hit target.
- Round/level code may be shown subtly inside the prompt panel, not as a separate competing label.

### Top-center prompt

Wood / soft-cloud / cream panel, approximately 620 × 96 on the base canvas.

Content:

- speaker/replay button
- sentence: **“Pop the circle.”**
- target word may use a teaching accent color but do not color-code it to the answer balloon.

Important: never visually reveal the correct balloon by matching target word color to target balloon color.

### Top-right

- Three progress stars, one star per completed round.
- Audio settings/replay may sit adjacent if the existing shell expects it.

### Main playfield

- 3–5 balloons, each 180–240 px high on desktop.
- Balloon body contains the semantic target: icon, simple illustration, shape, color patch, or optional word label.
- Large whitespace between balloons. Minimum center-to-center spacing: 220 px desktop, 130 px mobile landscape.
- Balloons can cross paths only at higher difficulty and never overlap more than 20% of their width.

### Launcher

Canonical placement: **bottom-center**.

- Toy-like launcher/cannon, not a weapon-realistic object.
- Barrel pivots around a fixed base.
- Friendly character can sit beside/behind it, reacting to aim and hits.
- Dart resembles a foam/suction-cup toy dart.

### Aim guide

- Dotted or soft glowing trajectory.
- Tutorial/easy levels: line reaches 70–90% toward target region.
- Normal levels: 40–55% preview.
- Harder levels: 25–35% preview.
- Never snap directly onto the correct answer.

## 4. Visual tokens

Suggested palette, adjustable to the existing Wordy Wings theme:

| Token | Suggested value | Use |
|---|---|---|
| Sky | `#9FDCFF` | background |
| Grass light | `#BFE59B` | midground |
| Grass dark | `#6FB86B` | foreground |
| Cream UI | `#FFF8E8` | prompt plates |
| Wood light | `#DDA15E` | sign/launcher details |
| Wood dark | `#8D5524` | outlines/details |
| Text | `#24483F` | primary text |
| Success | `#FFD65A` | stars/sparkles |
| Hint glow | `#FFF1A6` | delayed target hint |
| Balloon blue | `#74C9FF` | one color variant |
| Balloon coral | `#FF8F86` | one color variant |
| Balloon yellow | `#FFD768` | one color variant |
| Balloon violet | `#C8A0FF` | one color variant |
| Balloon green | `#94D985` | one color variant |

Do not bind semantic correctness to a fixed color.

## 5. Balloon art states

Every balloon requires these visual states:

1. `idle` – glossy, calm.
2. `hover/bob` – minor deformation and string sway.
3. `hit-wrong` – quick squash to ~0.90 scale, elastic rebound, no destruction.
4. `hint` – warm halo/pulse; only after hint threshold.
5. `hit-correct` – 80–120 ms squash, then pop.
6. `pop` – fragments/confetti/soft ring; no harsh explosion.
7. `respawn` – next-round balloons float in from edges/below.

The correct balloon must **not** glow before the hint system activates.

## 6. Dart visual

- Foam/suction-cup dart.
- 44–64 px long on base canvas.
- Bright but distinct from balloons.
- Small motion trail after firing.
- On wrong hit: toy-like bounce away and fade.
- On correct hit: briefly sticks/squashes balloon for 60–100 ms, then pop.

## 7. Background depth

Three layers:

- Far: sky, clouds, mountains.
- Mid: hills, waterfall, village/windmill/tree silhouettes.
- Near: grass, flowers, fences, leaves framing edges.

Use subtle parallax only. Background motion must not make balloon tracking difficult.

## 8. Typography

- Rounded sans-serif, high x-height, easy for a 6-year-old to read.
- Target instruction: ~38–44 px desktop equivalent.
- Balloon label: 28–34 px.
- Avoid all-caps for sentences; vocabulary labels may be lowercase to match early reading conventions.

## 9. What to remove from current screen

- Flat rectangular vocabulary cards.
- Thin stems/lines under cards.
- Huge empty lower area with no gameplay purpose.
- Static answer layout.
- Any permanent highlight that reveals the correct answer.

## 10. What must remain recognizable

- Calm pale-green / nature-friendly atmosphere.
- Simple instruction at top.
- Replay-audio control.
- Very low cognitive load.
- One clear action per round.
