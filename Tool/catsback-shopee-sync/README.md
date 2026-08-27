# CatsBack Shopee Affiliate Sync v0.4

v0.4 da cau hinh theo API automation CatsBack: **Client ID + Client Secret -> Bearer token ngan han -> upload report multipart**. Endpoint production duoc dien san; ban chi can nhap Client ID/Client Secret khi co credential.

## Kien truc

Tool gom 2 phan:

1. **Chrome Extension (Manifest V3)**
   - chay moi 60 phut;
   - dung Chrome profile/session Shopee hien tai;
   - khong doc password, khong bypass OTP/CAPTCHA;
   - click `Xuat du lieu` tren Conversion Report;
   - theo doi `export_management`, nhan dien dung export task moi;
   - click link download cua dung task va doi Chrome tai CSV xong.

2. **Local Helper (Node.js 18+)**
   - theo doi `%USERPROFILE%\Downloads`;
   - chi bat `AffiliateCommissionReport_*.csv` / `.txt` moi;
   - cho file tai on dinh;
   - SHA-256 chong upload exact file hai lan;
   - tu xin access token bang Client ID/Client Secret;
   - cache token trong RAM den gan het han, khong luu access token vao config;
   - upload `multipart/form-data` field `report` sang CatsBack;
   - neu import tra 401: bo token cu, xin token moi mot lan va retry;
   - retry HTTP theo cau hinh;
   - archive file sau khi import HTTP thanh cong;
   - settings UI tai `http://127.0.0.1:32145/settings`.

## 1. Cai Chrome Extension

1. Giai nen ZIP.
2. Mo `chrome://extensions/`.
3. Bat **Developer mode**.
4. Chon **Load unpacked**.
5. Chon thu muc `extension`.
6. Pin `CatsBack Shopee Affiliate Sync`.
7. Dang nhap `https://affiliate.shopee.vn/` binh thuong.
8. Mo Conversion Report, dat filter ngay mong muon.
9. Bam extension -> **Dong bo ngay** de test.

## 2. Chay Local Helper

Yeu cau: **Node.js 18+**.

Chay:

`local-helper/start-helper.cmd`

Helper tu tao `config.json` neu chua co.

## 3. Cau hinh CatsBack API

Mo Extension -> **Cai dat** (hoac `local-helper/open-settings.cmd`).

Endpoint da co san:

- Base URL: `https://catsback.onrender.com`
- Token endpoint: `/api/public/shopee-automation/token`
- Import endpoint: `/api/public/shopee-automation/reports/import`
- Multipart field: `report`

Ban chi can nhap:

- `Client ID`
- `Client Secret`

Sau do bam **Luu & kiem tra ket noi API**.

### Token flow

Helper goi:

```http
POST https://catsback.onrender.com/api/public/shopee-automation/token
Content-Type: application/json

{
  "client_id": "...",
  "client_secret": "..."
}
```

Response duoc ky vong co:

```json
{
  "access_token": "...",
  "token_type": "Bearer",
  "expires_in": 1800,
  "expires_at_utc": "..."
}
```

Access token chi giu trong RAM cua Helper. Helper uu tien `expires_at_utc`; neu khong co se dung `expires_in`. Mac dinh refresh truoc 60 giay.

### Import flow

```http
POST https://catsback.onrender.com/api/public/shopee-automation/reports/import
Authorization: Bearer <access_token>
Content-Type: multipart/form-data

report=<AffiliateCommissionReport CSV/TXT>
```

## 4. Neu chua co Client ID/Client Secret

Shopee export/download van chay. Khi Helper bat duoc report ma credentials chua du:

- khong upload;
- khong archive;
- ghi `WAIT_API_CONFIG`;
- giu file trong Downloads.

Khi credentials da co, cac file export moi se tu upload.

## 5. Kiem tra ket noi

Trong Extension Settings:

**Luu & kiem tra ket noi API**

Helper se xin mot access token moi va chi tra ve:

- token type;
- thoi diem het han.

Access token that khong duoc tra ve Extension UI va khong ghi log.

## 6. Log

`local-helper/logs/helper.log`

Log quan trong:

- `API_CONFIGURED true/false`
- `WAIT_API_CONFIG`
- `TOKEN_OK type=Bearer expiresAt=...`
- `UPLOAD_START`
- `IMPORT_401 refreshing_access_token=true`
- `UPLOAD_OK`
- `UPLOAD_FAIL`
- `SKIP_DUP`
- `ARCHIVE`

Khong log Client Secret hoac access token.

## 7. Bao mat

- Khong luu password Shopee.
- Khong doc Chrome Password Manager.
- Khong hard-code Cookie/CSRF/dynamic Shopee headers tu HAR.
- Khong luu Bearer access token vao disk; token chi cache trong RAM.
- Client Secret duoc luu trong `local-helper/config.json` tren may local va phai coi la secret local configuration.
- `config.json`, `state.json`, `logs/` khong commit Git.
- Neu Client Secret bi rotate, token cu co the mat hieu luc; Helper se refresh khi gap HTTP 401.
- Neu Shopee yeu cau CAPTCHA/OTP/device verification, tool dung va can login thu cong.

## 8. Cai Helper chay cung Windows

Sau khi test on dinh, mo PowerShell Run as Administrator trong `local-helper`:

```powershell
./install-startup.ps1
```

Task Scheduler se khoi dong Helper khi login Windows.

## 9. Flow end-to-end

```text
Chrome Extension
    -> Shopee Conversion Report
    -> tao export task
    -> cho TaskStatusSuccess
    -> download AffiliateCommissionReport_*.csv
    -> Local Helper
    -> Client ID + Client Secret
    -> token endpoint
    -> Bearer access token ngan han
    -> multipart report
    -> CatsBack import/upsert
    -> archive CSV neu HTTP thanh cong
```
