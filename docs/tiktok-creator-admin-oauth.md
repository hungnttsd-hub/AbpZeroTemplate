# TikTok Shop Creator OAuth của admin CatBack

Triển khai ngày 12/10/2026. Phạm vi trước mắt: authorize tài khoản Creator chính của admin, lưu token bền vững trong PostgreSQL, tự refresh và cung cấp token dùng chung cho client Creator hiện có. Chưa xác nhận nhận token thật cho tới khi admin hoàn tất authorize trên deployment production.

## Tài liệu đã đối chiếu

Đã đọc nội dung tải JavaScript từ Partner Center trong lần triển khai này:

- [Creator Authorization Guide](https://partner.tiktokshop.com/docv2/page/creator-authorization-guide): authorize URL, state, callback `code`, `user_type = 1`, granted scopes và lỗi Creator `105001`, `105002`, `105005`, `101000`.
- [Authorization overview — Token API reference](https://partner.tiktokshop.com/docv2/page/authorization-overview-202407): method GET, request/schema Get Access Token và Get Refresh Token. `grant_type=authorized_code` khi đổi code; `grant_type=refresh_token` khi refresh. Hai trường `*_expire_in` là Unix **expiration timestamp**, dùng `DateTimeOffset.FromUnixTimeSeconds`, không cộng vào thời điểm hiện tại. Refresh có cùng schema với exchange.
- Mapping business API tiếp tục theo [bảng đối chiếu Creator API](tiktok-shop-creator-api.md). Không thêm endpoint GetOrders hay mapping tracking link chưa được xác minh trong bước OAuth này.

## Cấu hình trước khi sử dụng

Đặt trên server bằng secret file/environment variables, không commit giá trị thật:

```text
TikTokShop__Creator__AppKey=<app key của CatBack>
TikTokShop__Creator__AppSecret=<app secret của CatBack>
TikTokShop__Creator__RedirectUrl=https://catback.id.vn/api/tiktok-shop/oauth/callback
TikTokShop__Creator__TokenRefreshBeforeExpiryMinutes=5
```

Code vẫn đọc cấu hình cũ `TikTokShop:AppKey` / `TikTokShop:AppSecret` nếu không có các key Creator tương ứng. Ưu tiên cấu hình Creator khi tồn tại. Authorize URL và hai endpoint token được cố định theo tài liệu; không có cấu hình đổi sang Login Kit/Seller.

`TikTokShop:Creator:RequiredScopes` mặc định `[]`: cho phép authorize và lưu token trước, mỗi business API kiểm tra scope riêng trước khi gọi. Có thể đặt quyền bắt buộc cho nghiệp vụ đã xác minh; code chỉ chấp nhận bốn key CatBack hiện có: `creator.affiliate.info`, `creator.affiliate_collaboration.read`, `creator.affiliate.link.write`, `creator.affiliate.share_link.read`. Không coi các scope link là alias của nhau. Thiếu quyền cấu hình vẫn lưu token với trạng thái `MissingScopes` và hiển thị danh sách thiếu; không chặn API đọc có đủ scope riêng.

Token được mã hóa bằng ASP.NET Core Data Protection, purpose riêng theo app key và Creator open ID. Key ring lưu ở `DataProtectionKeys` hiện có trong cùng database. Trên production phải cấu hình chứng chỉ DataProtection có private key, ví dụ `DataProtection:CertificatePath` và `DataProtection:CertificatePassword`, bằng secret storage hiện có. Tất cả instance phải dùng cùng database, `DataProtection:ApplicationName` và bộ chứng chỉ giải mã. Giữ chứng chỉ cũ trong cấu hình `PreviousCertificates` khi rotate. Code kiểm tra cấu hình trước khi authorize/đọc token; không tạo khóa bảo vệ token từ source code.

Kiểm tra cấu hình cục bộ trong lần sửa này: secret file Web đã có AppKey/AppSecret theo key cũ (không đọc ra log), nhưng các file JSON đã kiểm tra chưa có nguồn chứng chỉ DataProtection. Server có thể cấu hình bằng environment/secret mount riêng; cần bảo đảm nguồn chứng chỉ production thực sự được nạp trước authorize. Không sửa secret file thật trong lần triển khai source này.

HTTP client riêng tắt HttpClientFactory loggers, redirect, cookies và proxy; timeout 20 giây, connect timeout 10 giây, response tối đa 1 MiB. `SocketsHttpHandler.ActivityHeadersPropagator = null` bỏ DiagnosticsHandler trên .NET 8, ngăn URL token được phát qua Activity/HttpHandlerDiagnosticListener. Client không log raw response, URL, code authorize, token, secret hay exception message; DTO token cũng có `ToString` redacted. Không bật low-level HTTP EventSource request tracing/body capture cho client này. Nếu dùng agent APM ngoài ứng dụng, phải exclude các token requests tới `auth.tiktok-shops.com` và loại query của callback khỏi incoming telemetry. Proxy phía trước CatBack phải log callback bằng path đã bỏ query; log filter trong app không điều khiển được access log của reverse proxy. Không bật EF sensitive-data logging cho dữ liệu token/state.

## Migration và triển khai

Migration `20261012153552_AddTikTokCreatorCredentials` chỉ tạo:

- `affiliate.TikTokCreatorCredentials`: encrypted tokens, expiry UTC, granted scopes, status, admin khởi tạo, primary selection, token version và refresh lease.
- `affiliate.TikTokCreatorAuthStates`: SHA-256 state, app key, admin khởi tạo, expiry UTC, used time và trạng thái.
- Unique `(AppKey, OpenId)` và partial unique `AppKey WHERE IsPrimary = TRUE`; unique state hash và index expiry.

Áp dụng migration bằng quy trình DbMigrator hiện có của CatBack trước khi mở màn hình admin. Đây là bước triển khai cần thực hiện trên database phục vụ `catback.id.vn`; lần sửa source này chưa chạy DbMigrator và chưa thay đổi database production. Các token cache thử nghiệm cũ không được import; admin authorize lại để tạo credential database.

## Dùng token tại local với cùng database

Theo yêu cầu chủ dự án, production và local dùng trực tiếp **cùng database**; không thêm chức năng export/import hoặc sao chép token.

1. Áp dụng migration vào database dùng chung, publish production và authorize Creator ở `/Admin/TikTokCreator` trên `catback.id.vn`.
2. Local đặt `ConnectionStrings:Default` trỏ đúng database đó, dùng cùng AppKey/AppSecret TikTok và `DataProtection:ApplicationName` (hiện mặc định `CatsBack`).
3. Local dùng cùng chứng chỉ DataProtection có private key với production để đọc key ring được bảo vệ trong `DataProtectionKeys`. Đường dẫn file chứng chỉ có thể khác giữa hai máy; nội dung chứng chỉ/khóa phải khớp. Không cần lấy token từ trình duyệt hoặc authorize lại ở local.
4. Client Creator tại local đọc bản ghi `affiliate.TikTokCreatorCredentials` theo AppKey, giải mã ở backend và dùng token đó khi thử API. Khi production hoặc local refresh, cả hai đọc được token mới từ cùng bản ghi và dùng cùng database lease để tránh refresh đồng thời.

Callback vẫn là URL production đã đăng ký. `App:SelfUrl` của local giữ localhost; không cần đổi nó sang production để dùng token. Module giao diện hiện mặc định `TikTokAffiliate:Mode = RioHub`; cấu hình database chung giúp client Creator thật dùng được token, không tự đổi nghiệp vụ module RioHub thành API Creator.

## Admin authorize lần đầu

1. Deploy code và migration lên hệ thống phục vụ **https://catback.id.vn**; restart với cấu hình secret/chứng chỉ đúng.
2. Đăng nhập tài khoản **host admin**, role `admin` và permission `Affiliate.Admin.Settings`. Luồng này độc lập với quyền `IsTiktokDemo`.
3. Mở **Quản trị → TikTok Creator** tại `/Admin/TikTokCreator`, bấm **Authorize TikTok Creator**.
4. Đăng nhập đúng tài khoản TikTok Shop Creator và cấp các quyền mong muốn trên TikTok.
5. TikTok quay về đúng **https://catback.id.vn/api/tiktok-shop/oauth/callback**. Backend dùng state trong DB xác định admin; không yêu cầu cookie/session admin tại callback. State có 256 bit entropy, lưu hash, hạn 10 phút và bị consume atomic trước exchange. Callback lỗi/thiếu code/state sai/replay không gọi token exchange.
6. Backend kiểm tra HTTP, `code=0`, access/refresh tokens, open ID, `user_type=1` và timestamp. Token được mã hóa rồi commit cùng state `Completed`; thiếu scope lưu `MissingScopes`. Không gọi profile làm điều kiện nhận token.
7. Trang kết quả có URL sạch, không chứa code/token. Mở lại trang admin để xem trạng thái, open ID, expiry UTC, lần refresh và granted scopes. Không có endpoint trả plaintext token cho browser.

Trang admin khi chạy trên localhost/domain khác sẽ chuyển về trang admin production **trước khi tạo state**. Không authorize trên một database cục bộ rồi mong callback production đọc được state đó. Giữ Redirect URL đăng ký chính xác, không thêm `redirect_uri` vào URL authorize.

Authorize lại cùng open ID cập nhật credential và tăng version. Nếu tài khoản TikTok khác authorize, transaction từ chối ghi và giữ nguyên Creator chính, kể cả Creator chính đang cần authorize lại. Không có chức năng tự động thay Creator chính trong bước này. Ngắt kết nối workspace của user vẫn giữ token admin chung; nó không thu hồi quyền trên TikTok.

## Endpoints

| Endpoint | Quyền / kết quả |
| --- | --- |
| `GET /api/admin/tiktok-shop/creator/status` | Host admin + `Affiliate.Admin.Settings`; DTO an toàn, không token/secret. |
| `POST /api/admin/tiktok-shop/creator/authorize` | Cùng quyền, antiforgery; trả `{ authorizationUrl }`. Chỉ khởi tạo trên callback deployment. |
| `POST /api/admin/tiktok-shop/creator/refresh` | Cùng quyền, antiforgery; refresh qua lease chung rồi trả status. |
| `GET /api/tiktok-shop/oauth/callback` | Anonymous; state database là điều kiện bắt buộc trước exchange. |
| `GET /tiktok-shop/connection-result` | Anonymous; thông báo an toàn sau redirect. |

UI admin dùng Razor Page handlers với cơ chế quyền/antiforgery tương đương; handler Authorize redirect thẳng tới TikTok. API POST dùng cơ chế antiforgery hiện có của CatBack; integration frontend phải gửi token chống CSRF cùng cookie đăng nhập. Không tạo endpoint authorize bằng GET.

## Refresh và concurrency

`ITikTokCreatorTokenService` là dependency backend, không được expose qua conventional application API. Đọc trực tiếp credential DB ở mỗi request, không cache token. Nếu access token còn hơn khoảng an toàn (mặc định 5 phút), giải mã để dùng; nếu gần hết hạn, refresh bằng token đã lưu. Nếu refresh token hết hạn, chuyển `ReauthRequired` bằng conditional update, không gọi refresh API.

- Acquire lease atomic bằng DB time, `TokenVersion` và `RefreshLockUntilUtc`; lease 60 giây, dài hơn HTTP timeout.
- Instance không lấy được lease poll DB có jitter 400–800 ms, giới hạn chờ 75 giây; kiểm tra version trên mỗi lần đọc.
- Không giữ transaction DB trong khi gọi TikTok. Refresh kiểm tra lại Creator identity/open ID/scopes, mã hóa **cả access và refresh token mới** rồi conditional update theo lease ID, version và lease chưa hết hạn. Kết quả cũ không ghi đè được reauthorization/refresh mới.
- Sau lost lease/version, đọc lại DB. Lease tự hết hạn nếu instance chết. Temporary network/timeout/429/5xx giữ trạng thái credential, đặt cooldown DB 1–60 giây (đọc `Retry-After`, mặc định 10 giây). Request hiện tại nhận lỗi an toàn; request tiếp theo chỉ refresh sau cooldown. Không tự retry code exchange/refresh HTTP khi kết quả rotation chưa rõ.
- `105001` ở refresh hoặc Creator business call đánh dấu `ReauthRequired` có điều kiện version; `105005` chỉ báo scope; `101000` báo pairing/configuration, không tự xóa token. Timeout/429/5xx không bị coi là thu hồi quyền.
- `TikTokCreatorConnection.GetProfile` dùng token chung; `105002` được forced-refresh có kiểm tra version, retry profile đúng một lần. `GetLink` hiện có cũng lấy token qua service chung, giữ scope check của Creator Generate General Link và không tự retry thao tác ghi.

Ghi chú lịch sử của bước OAuth: lúc đó chưa thêm GetOrders thật. Cập nhật 13/10/2026: luồng chính đã dùng Creator API cho sinh link và lấy/lưu đơn bằng nút admin, xem [hiện trạng Creator API](tiktok-shop-creator-api.md). Chưa triển khai đối soát/cộng ví. Người dùng thông thường không thể authorize/thay credential admin.

## Files chính

- Domain: `TikTokAffiliate/TikTokCreatorCredentials.cs`, `ITikTokCreatorCredentialRepository.cs`.
- EF Core: `TikTokAffiliate/EfCoreTikTokCreatorCredentialRepository.cs`, `EntityFrameworkCore/TikTokCreatorDbContextModelCreatingExtensions.cs`, `WebHoanTienDbContext.cs`, migration/designer/model snapshot.
- Contracts: `TikTokAffiliate/ITikTokCreatorTokenService.cs`.
- Web: `Integrations/TikTokCreatorTokenService.cs`, `TikTokCreatorOAuthClient.cs`, `TikTokCreatorConnection.cs`; controller OAuth; Razor admin page; menu; OAuth result view; DI/HTTP client; appsettings và secrets example.

## Kiểm chứng

Build cuối: `dotnet build src/WebHoanTien.Web/WebHoanTien.Web.csproj --no-restore -p:SkipClientAssets=true -v:quiet` thành công, 0 warning, 0 error. `git diff --check` không có lỗi whitespace. Migration được scaffold bằng EF runtime 8.0.8; CLI hiện có phiên bản 6.0.35 phát cảnh báo cũ hơn runtime, nhưng scaffold hoàn tất và migration chỉ chứa hai bảng/index TikTok mới. Chưa chạy test, Playwright hoặc kiểm thử trình duyệt tự động theo AGENTS.md và phạm vi ưu tiên authorize trước. Browser chỉ dùng đọc tài liệu chính thức. Chưa áp dụng migration vào database thực tế, chưa đăng nhập/cấp quyền thay admin, chưa exchange code thật hay chứng minh concurrency bằng hai instance đang chạy. Những kết quả đó cần kiểm tra sau khi triển khai và admin tự hoàn tất authorize.
