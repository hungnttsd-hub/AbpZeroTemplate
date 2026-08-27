# CatsBack Shopee Sync - Technical Spec v0.4

## Muc tieu

Moi 60 phut, dung phien Chrome Shopee Affiliate hien tai de export Conversion Report, tai dung CSV vua tao va day file sang CatsBack. CatsBack authentication dung machine credential: Client ID/Client Secret doi Bearer token ngan han.

## Browser side

Chrome Extension Manifest V3, khong Playwright.

- dung session Chrome that;
- khong automation login;
- khong replay Cookie/CSRF/dynamic Shopee security headers;
- click UI Shopee de frontend tu tao request hop le.

## Export correlation

1. mo/reuse `/export_management`;
2. doc ready download anchors hien co -> `baselineTaskIds`;
3. click `Xuat du lieu` tai `/report/conversion_report`;
4. poll DOM `/export_management`;
5. tim ready anchor co `taskId` khong thuoc baseline;
6. chi nhan `AffiliateCommissionReport_*.csv`;
7. neu nhieu candidate, chon taskId lon nhat;
8. click anchor dung task;
9. theo doi `chrome.downloads` den `state=complete`.

## Shopee HAR-derived behavior

- create operation: `submitAsyncExportTask`;
- task type: `export_website_report`;
- list operation: `AsyncExportTaskListQuery`;
- success: `taskStatus = TaskStatusSuccess`, `progress = 100`;
- download row dung native `<a ... download>` voi URL co `task_id`.

## CatsBack automation API

Default configuration:

```text
apiBaseUrl = https://catsback.onrender.com
tokenPath  = /api/public/shopee-automation/token
importPath = /api/public/shopee-automation/reports/import
formFieldName = report
```

Credentials:

```text
clientId
clientSecret
```

### Token request

```http
POST {apiBaseUrl}{tokenPath}
Content-Type: application/json

{
  "client_id": "<clientId>",
  "client_secret": "<clientSecret>"
}
```

Expected fields:

```text
access_token
token_type
expires_in
expires_at_utc
```

Token cache:

- memory only;
- khong persist vao `config.json` / `state.json`;
- refresh neu con it hon `tokenRefreshSkewSeconds` (default 60s);
- neu import tra HTTP 401: invalidate token, force token refresh, retry import mot lan.

### Import request

```http
POST {apiBaseUrl}{importPath}
Authorization: Bearer <access_token>
Content-Type: multipart/form-data

report=<CSV/TXT>
```

## Local settings API

Extension Options ket noi Helper qua:

```text
GET  http://127.0.0.1:32145/api/settings
POST http://127.0.0.1:32145/api/settings
POST http://127.0.0.1:32145/api/test-connection
GET  http://127.0.0.1:32145/health
```

Secret handling:

- GET settings chi tra `hasClientSecret`, khong tra Client Secret;
- test connection chi tra token type + expiry, khong tra access token;
- log sanitizer che Client Secret/access token neu chung vo tinh xuat hien trong exception.

## Khi credentials chua cau hinh

Helper khong upload va khong archive, ghi `WAIT_API_CONFIG`.

API duoc xem la san sang khi co:

- API base URL;
- token/import path;
- Client ID;
- Client Secret.

## Idempotency

1. Helper SHA-256 chong upload exact file hai lan.
2. CatsBack backend UPSERT conversion/order theo business key, vi report overlap co the update status/commission cu.

## Failure handling

- `LOGIN_REQUIRED`: dung run va notify.
- `EXPORT_TIMEOUT`: khong tim thay export moi thanh cong truoc timeout.
- `DOWNLOAD_TIMEOUT`: export co nhung Chrome chua tai xong.
- `WAIT_API_CONFIG`: giu CSV, chua upload.
- token endpoint non-2xx: upload fail va retry co gioi han.
- import HTTP 401: refresh token mot lan ngay trong attempt.
- `UPLOAD_FAIL`: retry theo config.

## Security

Khong dong goi/replay:

- Shopee Cookie;
- CSRF token;
- dynamic security headers;
- Shopee password/OTP;
- CatsBack access token;
- HAR that.

`config.json` tren may local chua Client Secret. File nay phai nam ngoai source control va duoc bao ve nhu secret local.
