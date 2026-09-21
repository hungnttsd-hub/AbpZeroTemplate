# 04 – Responsive & Accessibility Specification

## Supported gameplay orientation

Primary gameplay orientation is **landscape**.

- Desktop: 16:9 or wider.
- Tablet: landscape preferred.
- Mobile native app: landscape gameplay screen recommended.

If mobile is portrait, show the normal app shell and a friendly rotate-device panel before entering this mechanic rather than compressing the shooter into an unusable portrait area.

## Scaling rules

Use a virtual design size of 1440×810 and `Phaser.Scale.FIT` or equivalent existing project scaling strategy.

- Preserve aspect ratio.
- Letterbox/pillarbox if necessary.
- Respect safe areas on iOS/Android when wrapped in Capacitor.

## Minimum target sizes

- Pause/audio buttons: ≥ 48 CSS px interaction area.
- Balloon collision/hit area: at least 90% of visible balloon body; never only the icon inside it.
- Replay prompt button: ≥ 52 px.

## Color and recognition

Never rely on color alone when teaching objects/shapes. Use clear silhouettes/icons.

For color-specific rounds, the spoken color is the learning target and balloon colors must be distinct enough to differentiate.

## Text

- High contrast.
- No thin font weights for target words.
- Avoid long bilingual instructions in the main playfield.
- Vietnamese helper text is tutorial-only and can disappear after the first few sessions.

## Accessibility input

Support both:

- drag-to-aim + release
- tap balloon to aim/fire

The second mode is especially useful for children with fine-motor difficulties.

## No stressful mechanics

- No countdown clock.
- No limited lives.
- No “FAIL”, “GAME OVER”, large red X, or loud buzzer.
- Errors trigger learning feedback, not punishment.

## Network robustness

Gameplay must not block on API calls after scene load.

- Load level config/assets before round begins.
- Cache vocabulary audio as appropriate.
- Save completion locally first if offline support exists.
- Sync progress after level completion/checkpoint.
