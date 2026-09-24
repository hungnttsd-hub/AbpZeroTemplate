# DEPLOYMENT — RENDER

## Services

### 1. Backend

Render Web Service using Docker.

Environment variables (names can adapt to actual ABP configuration):

- ASPNETCORE_ENVIRONMENT=Production
- ConnectionStrings__Default=<PostgreSQL connection>
- App__SelfUrl=https://api.example.com
- App__CorsOrigins=https://game.example.com
- AuthServer__Authority=https://api.example.com
- AuthServer__RequireHttpsMetadata=true
- Assets__BaseUrl=https://cdn.example.com

Run migrations through DbMigrator as a controlled release step; do not blindly run concurrent migrations from every API instance.

### 2. Frontend

Render Static Site from React build output.

- API base URL points to backend.
- Configure SPA rewrite fallback to `/index.html`.
- Set immutable cache for hashed game bundles; content manifest should use version-aware cache policy.

### 3. Database

Recommended: existing Supabase PostgreSQL or managed paid PostgreSQL. Do not rely on temporary/free DB for production child progress.

### 4. Assets

Cloudflare R2/S3-compatible storage. Public CDN for non-sensitive game assets. Do not store user-sensitive uploads in the same public bucket.

## Docker backend checklist

- multi-stage .NET build;
- expose configured port;
- bind to `0.0.0.0`;
- health endpoint;
- no local persistent assumption;
- production logging;
- data protection keys configured appropriately for multi-instance deployment if needed.

## Mobile later

Android/iOS Capacitor clients continue calling the same HTTPS API and CDN. Add OIDC redirect/deep-link settings and secure token storage adapter; backend topology stays unchanged.
