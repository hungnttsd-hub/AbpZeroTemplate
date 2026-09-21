# 02 – Gameplay Specification: Balloon Dart

## 1. Core learning loop

`LISTEN → AIM → FIRE → FEEDBACK → HEAR WORD → NEXT ROUND`

Example:

1. Audio: **“Pop the circle.”**
2. Four balloons drift gently: ball / circle / star / flower.
3. Child aims launcher.
4. Child fires a toy dart.
5. If correct: circle balloon pops → voice says **“Circle!”** → one star fills.
6. New group enters for the next round.

## 2. Input model

### Primary – drag-to-aim

- Pointer down in the lower playfield or on launcher aim area.
- Aim direction follows pointer.
- Release fires.
- Launcher rotation range: approximately **20°–160°** measured from horizontal depending on coordinate convention.
- Clamp aim so the player cannot shoot downward/off the game surface.

### Alternative – tap-to-shoot

For accessibility and very young players:

- Tap a balloon.
- Launcher rotates toward the tapped balloon over 120–180 ms.
- Dart fires automatically.

This may be enabled via level config (`inputMode: "tap"`) or accessibility settings.

### Desktop

Mouse move/drag and click supported.

### Touch

Do not require pixel-perfect drag. Aim handle and hit regions must be forgiving.

## 3. Projectile rules

- One active dart by default.
- Fire cooldown: 250–350 ms after dart despawn/hit.
- Unlimited darts; no ammo count.
- Dart speed at 1440×810 baseline: 850–1050 px/s.
- No lethal/explosive effects.
- Dart disappears cleanly after leaving playfield bounds.

## 4. Balloon motion patterns

Movement is deterministic from config so QA can reproduce behavior.

### `gentleBob`

- Horizontal speed: 0–15 px/s.
- Vertical sine amplitude: 12–24 px.
- Period: 2.8–4.5 s.

### `driftHorizontal`

- Horizontal path amplitude: 80–180 px.
- Vertical bob: 8–18 px.
- Period: 4–7 s.

### `driftVertical`

- Vertical amplitude: 60–120 px.
- Horizontal bob: 8–20 px.

### `figureEight`

- Only difficulty 4+.
- X amplitude: 70–120 px.
- Y amplitude: 25–60 px.
- Slow period: 5–8 s.

### `crossLane`

- Balloon enters from one side, travels across a lane, loops or respawns.
- Only one or two balloons use it at once.

## 5. Collision behavior

### Correct balloon

1. Freeze that balloon’s path.
2. Squash 80–120 ms.
3. Pop VFX + soft SFX.
4. Speak target word.
5. Mascot cheers.
6. Fill one progress star.
7. Wait 700–1100 ms.
8. Transition to next round.

### Wrong balloon

1. Balloon squashes and rebounds.
2. Dart bounces/falls away.
3. Speak that balloon’s word, e.g. **“Star.”**
4. Optional supportive voice: **“Try circle.”** after audio queue clears.
5. No health/life reduction.
6. Balloon remains available.

### Miss

- Dart exits bounds.
- Soft “whoosh” only.
- Reload immediately.
- Do not interrupt the instruction voice.

## 6. Hint system

Hints are progressive and non-punitive.

- First wrong hit or miss: no visual answer hint.
- Second error in same round: replay target instruction automatically once.
- Third error: correct balloon receives a 1.2 s soft halo pulse every 2.5 s.
- Fifth error: optional short dotted guide briefly points generally toward target region, not an exact auto-shot.

Hint count is recorded for parent analytics but never shown as failure to the child.

## 7. Round and level structure

Recommended MVP structure:

- 1 level = 3 rounds.
- 1 round = 1 target prompt.
- Each completed round fills one star.
- Completing all 3 rounds means **3 stars**, regardless of mistakes.
- Accuracy and attempts are analytics, not visible punishment.

This makes the star system represent **progress**, not performance pressure.

## 8. Learning modes

### `pictureListening`
Instruction audio + picture balloons, labels optional.

### `wordRecognition`
Instruction audio + printed word on balloon, picture optional.

### `colorRecognition`
Example: “Pop the red balloon.” Balloons must vary in color without extra semantic icons.

### `compoundListening`
Example: “Pop the blue circle.” Requires matching two attributes.

### `sizeAndShape`
Example: “Pop the big circle.”

The same Balloon Dart engine must support all modes without word-specific code.

## 9. Difficulty ladder

| Difficulty | Balloons | Motion | Labels | Aim assist | Distractor complexity |
|---|---:|---|---|---|---|
| 1 | 3 | static/gentle bob | visible | long | very different |
| 2 | 4 | gentle bob | visible | long | same category |
| 3 | 4 | horizontal drift | fade after 2 s | medium | same category |
| 4 | 5 | mixed drift | hidden | medium | visually similar |
| 5 | 5 | figure-eight/cross-lane | hidden | short | compound attributes |

No timers in MVP.

## 10. Session pacing

- Target round length: 8–25 seconds.
- Target level length: 45–90 seconds.
- Correct-hit celebration: under 1.8 seconds.
- Avoid long blocking animations.

## 11. Fail-safe behavior

If no shot occurs for 8 seconds:

- replay instruction once or animate mascot pointing at the playfield.

If no shot occurs for 15 seconds:

- show gentle “Aim and shoot” hand animation.

Never auto-complete a round without input.
