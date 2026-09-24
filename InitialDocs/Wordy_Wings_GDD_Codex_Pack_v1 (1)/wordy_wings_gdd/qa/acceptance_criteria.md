# MVP ACCEPTANCE CRITERIA

## P0 — must pass

- [ ] Parent can register/login.
- [ ] Parent can create a child profile.
- [ ] Child map loads W01–W03.
- [ ] Exactly 30 MVP level definitions validate and load.
- [ ] Five MVP mechanics are playable with mouse and touch.
- [ ] Speaker/replay works or gracefully falls back.
- [ ] Wrong answer never causes hard fail/game-over.
- [ ] Two wrong attempts trigger hint.
- [ ] Completing level persists attempt and unlocks next level.
- [ ] Retry cannot reduce best stars.
- [ ] Reload preserves progression.
- [ ] API outage after level completion queues pending attempt locally.
- [ ] Reconnect syncs pending attempt without duplicate progress.
- [ ] Missing asset does not crash level.
- [ ] Missing audio does not block completion.
- [ ] World 2 unlocks after W01-L10; World 3 after W02-L10.
- [ ] Parent dashboard shows completed levels, best stars and review terms.
- [ ] Build succeeds in production mode.

## P1 — should pass

- [ ] Chrome desktop current stable.
- [ ] Android Chrome landscape.
- [ ] iPad/Safari landscape smoke test.
- [ ] No console error during normal 5-level session.
- [ ] Average asset request path uses CDN/base URL abstraction.
- [ ] Gameplay target areas remain comfortably tappable at 667x375 CSS viewport.

## P2 — polish

- [ ] Reduced-motion preference disables nonessential looping animation.
- [ ] Mute state persists locally.
- [ ] Level result animation completes within ~2.5 seconds or can be skipped.
