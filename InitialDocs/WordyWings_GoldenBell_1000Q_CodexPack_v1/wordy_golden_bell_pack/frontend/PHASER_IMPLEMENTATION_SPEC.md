# Phaser / React Implementation Spec

## Boundary
React: route, auth, loading shell, parent dashboard, session summary.
Phaser: Bell Arena, character animations, question renderers, input, audio, feedback, bell payoff.

## Scene proposal
- `GoldenBellBootScene` — preload minimal common assets.
- `GoldenBellArenaScene` — persistent arena + characters + bell energy.
- `GoldenBellQuestionLayer` — mounts/unmounts question renderer.
- `GoldenBellCelebrationLayer` — checkpoints + final bell.

## Services
- `GoldenBellSessionController`
- `QuestionRendererRegistry`
- `GoldenBellAudioManager` (reuse existing AudioManager)
- `HintController`
- `AnswerEvaluator`
- `BellEnergyController`
- `GoldenBellEventBus`

## Registry
`questionType -> renderer`. Không switch khổng lồ trong scene. Mỗi renderer implement:
- `mount(question, context)`
- `submit(input)`
- `showHint(level)`
- `dispose()`

## Events
- GOLDEN_BELL_SESSION_STARTED
- QUESTION_PRESENTED
- ANSWER_SUBMITTED
- ANSWER_CORRECT
- ANSWER_INCORRECT
- HINT_SHOWN
- CHECKPOINT_REACHED
- FINAL_BELL_READY
- FINAL_BELL_RUNG
- SESSION_COMPLETED

## State machine
`INTRO -> PRESENTING -> WAITING_INPUT -> FEEDBACK -> CHECKPOINT? -> NEXT -> FINAL_BELL -> SUMMARY`

## Mobile
Landscape-first. Khi viewport portrait: hiện rotate suggestion nhưng không block nếu vẫn đủ chiều rộng. Touch targets >= 48 CSS px, đáp án chính >= 72px.

## Performance
Không preload 1000 câu/assets. Mỗi session preload common + assets của 12 câu. Dispose renderer và listeners giữa câu. Object pool cho sparkle/confetti.
