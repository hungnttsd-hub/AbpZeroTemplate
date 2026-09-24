# Technical references / source notes

Checked: **2026-09-23**. These references support implementation guidance, not claims that this project has already been integrated or tested on a device. The game's timings, layouts and learning rules are proposed design decisions.

| ID | Official source | Guidance supported |
|---|---|---|
| P1 | [Phaser Input concepts](https://docs.phaser.io/phaser/concepts/input) | Unified input, interactive objects, drag handling and input gating |
| P2 | [Phaser 3.90 Mask component](https://docs.phaser.io/api-documentation/3.90.0/namespace/gameobjects-components-mask) | Version-specific mask behaviour; visual masking does not itself disable input/physics; bitmap masks require WebGL in this version |
| P3 | [Phaser Scenes API](https://docs.phaser.io/api-documentation/namespace/scenes) | Scene lifecycle states, pause, shutdown and destruction. This URL follows the current version; match the repository's pinned version before using its API |
| P4 | [Phaser 3.90 InputPlugin](https://docs.phaser.io/api-documentation/3.90.0/class/input-inputplugin) | Pinned input reference for projects that actually use Phaser 3.90; not a request to install or upgrade to that version |
| A1 | [ABP authorization](https://abp.io/docs/latest/framework/fundamentals/authorization?LanguageCode=en) | Reuse ABP authorization; the per-child ownership checks and idempotency design in this pack remain application responsibilities |

## Reference-image provenance

The two PNGs in `references/` were supplied in this conversation as the existing Hide & Seek concept and interaction board. No third-party stock images have been added. `design/image-map.json` records the crop origin and native dimensions for each detail. D36 replaces only the incorrect English question heading; all other source text is interpreted under the written rules.

## Version warning

Do not copy a mask example from a current Phaser API page into a repository using an older major version without checking compatibility. This handoff deliberately keeps scoring/state-machine examples engine-independent. No live application repository was inspected when creating this pack.
