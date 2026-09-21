# Acceptance Criteria

## Functional

- [ ] Level is created from JSON only; no word/shape-specific gameplay branches.
- [ ] 3–5 balloons can be configured per round.
- [ ] Balloons move continuously according to configured path.
- [ ] Drag/mouse aim works.
- [ ] Touch aim works.
- [ ] Tap-to-shoot accessibility mode works when enabled.
- [ ] Dart uses pooled/reusable objects.
- [ ] Correct target pops and advances exactly one round.
- [ ] Wrong target remains in play.
- [ ] Miss does not advance the round.
- [ ] One star fills for each completed round.
- [ ] Three completed rounds finish the level.
- [ ] Correct answer is not visually revealed before hint activation.
- [ ] Instruction replay works.
- [ ] Voice lines do not overlap.
- [ ] Hint thresholds behave as specified.

## UX for age 6

- [ ] No timer.
- [ ] No lives/ammo limitation.
- [ ] No game over.
- [ ] No large red X or harsh failure buzzer.
- [ ] Wrong-hit feedback names the struck item.
- [ ] Child can recover after any number of mistakes.
- [ ] Main action is understandable without reading a paragraph of instructions.

## Responsive

- [ ] 1440×810 works.
- [ ] 1280×720 works.
- [ ] 844×390 landscape works.
- [ ] Safe areas do not cover pause/audio/progress/launcher.
- [ ] No balloon crosses top prompt safe zone.
- [ ] No balloon crosses launcher safe zone.

## Engineering

- [ ] TypeScript strict build passes.
- [ ] Existing tests pass.
- [ ] Scene restart leaves no duplicate listeners.
- [ ] Scene restart leaves no orphan Matter/physics bodies.
- [ ] Tweens/timers are cleaned on shutdown.
- [ ] No API request is sent per shot.
- [ ] Level completion can reuse current progress API/service.
- [ ] No console warnings/errors during normal play.

## Performance

- [ ] 60 FPS target on desktop.
- [ ] ≥30 FPS on representative mid-range mobile.
- [ ] ≤5 active balloons in MVP.
- [ ] Pop particles are pooled or bounded.
- [ ] Assets for unrelated worlds are not preloaded.
