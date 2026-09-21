# TECH ARCHITECTURE — ABP.IO + React + Phaser + Capacitor

## 1. Chốt stack

| Layer | Technology |
|---|---|
| Backend | ABP.io / ASP.NET Core |
| API | ABP Application Services exposed as REST |
| Auth | ABP Identity + OpenIddict/OIDC |
| ORM | EF Core |
| Database | PostgreSQL |
| Web app | React + TypeScript |
| Game runtime | Phaser |
| Physics | Phaser Matter only where mechanic needs physics |
| Build | Use toolchain of current ABP React template; keep game package TypeScript-first |
| Assets | Cloudflare R2 / S3-compatible object storage + CDN |
| Web deploy | Render Static Site |
| API deploy | Render Web Service via Docker |
| Mobile later | Capacitor Android/iOS |
| Local/offline | IndexedDB on web; adapter can switch to SQLite-capable storage in Capacitor |

## 2. High-level topology

```text
React + Phaser (Web) ─────┐
                          ├── HTTPS/REST ──> ABP.IO API ──> PostgreSQL
Capacitor (Android/iOS) ──┘                       │
                                                 └──> R2/S3 asset metadata + signed/public CDN URLs
```

Backend is the source of truth for user/progress. Client maintains a local progress queue so play can continue through temporary connectivity loss.

## 3. Monorepo recommendation

```text
WordyWings/
  aspnet-core/
    src/
      WordyWings.Domain.Shared/
      WordyWings.Domain/
      WordyWings.Application.Contracts/
      WordyWings.Application/
      WordyWings.EntityFrameworkCore/
      WordyWings.HttpApi/
      WordyWings.HttpApi.Host/
      WordyWings.DbMigrator/
  react/
    src/
      app/
      features/parent/
      features/child-profile/
      features/world-map/
      game/
        bootstrap/
        scenes/
        mechanics/
        services/
        components/
        types/
      generated-api/
  content/
    worlds.json
    levels.json
    vocabulary.json
  mobile/
    # add later with Capacitor; do not fork game logic
```

## 4. Boundary rules

- React owns application shell, auth, parent screens, route transitions, settings.
- Phaser owns moment-to-moment gameplay, scene lifecycle, physics, game input and in-game feedback.
- React must not implement physics/game loop.
- Phaser must not own authentication or direct database logic.
- All content is data-driven from JSON/API.
- Client never decides mastery authoritatively; it submits attempts, backend calculates persistent mastery.

## 5. ABP modules / capabilities

Use built-in ABP facilities where practical:

- Identity / users / roles.
- OpenIddict authentication.
- Permission Management for admin/content roles.
- Setting Management for game/content flags.
- Blob metadata abstraction if useful, but binary assets stay in object storage/CDN.

Suggested roles:

- `Admin`
- `ContentEditor`
- `Parent`

Children are **ChildProfile entities owned by a Parent user**, not independent login accounts in MVP.

## 6. API design

```http
GET    /api/game/worlds
GET    /api/game/worlds/{worldId}/levels
GET    /api/game/levels/{levelId}
GET    /api/game/children
POST   /api/game/children
GET    /api/game/children/{childId}/progress
POST   /api/game/attempts
POST   /api/game/progress/sync
GET    /api/game/children/{childId}/review-queue
GET    /api/game/children/{childId}/dashboard
```

Attempt DTO minimum:

```json
{
  "attemptId": "client-generated-guid",
  "childId": "guid",
  "levelId": "W01-L01",
  "startedAt": "ISO-8601",
  "completedAt": "ISO-8601",
  "wrongAttempts": 1,
  "hintCount": 0,
  "stars": 2,
  "targetResults": [
    { "term": "red", "correct": true, "responseMs": 3200 }
  ]
}
```

`attemptId` makes sync idempotent.

## 7. Offline strategy

Client maintains:

- cached content manifest;
- cached level JSON;
- local progress snapshot;
- pending attempt queue.

On reconnect:

1. authenticate/refresh token;
2. POST pending attempts using idempotent IDs;
3. backend merges progress;
4. client pulls canonical progress.

MVP may implement browser IndexedDB adapter only, but interfaces must allow a Capacitor implementation later.

## 8. Content versioning

Every published level has `contentVersion` and `updatedAt`.

Client caches by version. A content change must never corrupt an in-progress session. Download new version on next level entry or next session.

## 9. Asset strategy

Store only metadata/keys in PostgreSQL:

```text
assets/worlds/w01/background.webp
assets/vocab/apple/card.webp
audio/en/apple.mp3
audio/en/w01/01_instruction.mp3
```

Use a configurable `AssetBaseUrl`. Never save production uploads to ephemeral Render filesystem.

## 10. Mobile migration

Capacitor wraps the same React + Phaser web build. Mobile work later is limited mainly to:

- native project bootstrap;
- secure token storage adapter;
- offline storage adapter;
- app icon/splash/store metadata;
- push notification if added;
- mobile deep link/OIDC redirect configuration.

No new game backend is required.
