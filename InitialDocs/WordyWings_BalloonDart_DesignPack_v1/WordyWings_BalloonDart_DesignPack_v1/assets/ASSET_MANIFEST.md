# Asset Manifest

This pack includes prototype SVGs only. Production art should follow the selected concept image and the Wordy Wings character/world art direction.

## Required production assets

### Environment

| Asset | Suggested source size | Runtime |
|---|---:|---|
| `bg_sky_meadow` | 2048×1152 | WebP |
| `bg_mountains` | 2048×1152 | WebP/atlas layer |
| `bg_hills_waterfall` | 2048×1152 | WebP |
| `fg_grass_flowers` | 2048×512 | transparent WebP/PNG |
| `cloud_01..03` | 512×256 | atlas |

### Launcher

- `launcher_base`
- `launcher_barrel`
- `launcher_recoil_fx`
- `dart_foam_blue`
- `dart_trail_particle`

Suggested authoring: 512–1024 px per major launcher piece; scale down in runtime.

### Balloons

Create balloon skins as reusable bodies so semantic icons are separate overlays.

- blue
- coral
- yellow
- violet
- green
- orange

Balloon body source: approximately 512×640 transparent PNG/WebP or packed atlas.

States may use skeletal deformation, mesh/tween deformation, or a short sprite sequence. Prefer tween deformation for MVP.

### Semantic icons

At least 256×256 source assets for:

- ball
- circle
- star
- flower
- square
- triangle
- heart
- sun
- moon
- common color-only marker shapes

### Mascot

Minimum states:

- idle
- aim/look
- fire/react
- correct cheer
- supportive try-again

### VFX

- soft pop ring
- star sparkle
- confetti pieces
- hint halo
- dart trail

## Audio naming

```text
audio/sfx/dart_fire_soft.*
audio/sfx/dart_whoosh.*
audio/sfx/balloon_pop_soft.*
audio/sfx/balloon_wrong_boing.*
audio/sfx/star_fill.*
audio/voice/instructions/pop_the_circle.*
audio/voice/words/circle.*
```

Provide browser-compatible formats according to the existing project audio pipeline (commonly `.mp3` plus `.ogg` where needed).

## Prototype SVG assets included

- `prototype/launcher.svg`
- `prototype/dart.svg`
- `prototype/balloon.svg`
- `prototype/pop-burst.svg`

These are implementation placeholders, not final visual art.
