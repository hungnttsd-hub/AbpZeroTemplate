# Test Matrix

| ID | Scenario | Expected |
|---|---|---|
| BD-T01 | Load BD-01 | 3 rounds load from JSON |
| BD-T02 | Shoot correct balloon | Balloon pops, word plays, 1 star fills, next round starts |
| BD-T03 | Shoot wrong balloon | Balloon rebounds, wrong word plays, round remains active |
| BD-T04 | Miss all balloons | Dart recycles, round stays active |
| BD-T05 | 2 errors same round | Instruction replays once |
| BD-T06 | 3 errors same round | Target gets delayed subtle hint halo |
| BD-T07 | Correct hit while second pointer input arrives | Round completes once only |
| BD-T08 | Restart scene 10 times | No duplicated events/tweens/bodies |
| BD-T09 | Compound target blue circle | Only shape=circle AND color=blue resolves correct |
| BD-T10 | Same shape, different colors | Color target works |
| BD-T11 | Same color, different shapes | Shape target works |
| BD-T12 | 844×390 | All controls usable, no clipped launcher/prompt |
| BD-T13 | Pointer drag outside canvas | Aim clamps safely, no stuck input |
| BD-T14 | Rapid fire attempts | Cooldown prevents duplicate active dart if configured |
| BD-T15 | Audio replay during SFX | Voice queue remains intelligible |
| BD-T16 | Reduced motion | Mechanic remains fully playable |
| BD-T17 | API temporarily unavailable at completion | Local/game state completes; existing retry/offline behavior handles sync |
| BD-T18 | Tab/app loses focus | Pause/suspend according to existing game policy; no runaway tweens |
