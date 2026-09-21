# TEST MATRIX

| Area | Case | Expected |
|---|---|---|
| Auth | Expired token during save | attempt stays queued; user can recover after auth |
| Content | Invalid mechanic ID | level rejected before scene launch |
| Content | Missing imageKey | generic placeholder shown |
| Audio | MP3 404 | fallback voice or silent playable state |
| Progress | same attempt submitted twice | one logical attempt, no duplicate reward |
| Progress | replay with fewer stars | best star unchanged |
| Offline | complete 3 levels offline | all 3 queue and sync later |
| Input | touch drag slingshot | launch vector updates smoothly |
| Input | mouse drag | same outcome as touch |
| Balloon | rapid double tap | single scoring event |
| Sort | drop outside zone | object returns safely |
| Puzzle | repeated wrong letter | no lock; hint after threshold |
| Boss | fail round 3 | restart at round 3/checkpoint per design |
| Responsive | 667x375 | no clipped primary controls |
| Responsive | 1366x768 | canvas centered, no stretched distortion |
| Child safety | external link in kid mode | none present |
