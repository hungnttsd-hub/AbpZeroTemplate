# Tích hợp RioHub vào CatBack

Ngày đối chiếu: 10/10/2026. Nguồn: tài liệu người dùng cung cấp và [Developer API của RioHub](https://riohub.vn/tiktok-shop-developer), bản cập nhật hiển thị 09/10/2026.

## Phạm vi đã triển khai

Module server `IRioHubAffiliateClient` và API quản trị hỗ trợ tạo link có sub_id, tạo link kèm thông tin sản phẩm, deep link theo lô, lấy link, sản phẩm và đơn hàng. Phản hồi thành công giữ nguyên JSON của RioHub (`JsonElement`): ID và tiền dạng chuỗi không bị chuyển sang floating point; các field mới của nhà cung cấp được giữ lại. Request được kiểm tra trước khi gửi.

Đây là API trung gian RioHub, dùng key RioHub. Nó không thay thế hay xác minh scope của app TikTok CatBack. Theo yêu cầu chủ dự án, trang `/tiktok-affiliate` dùng service RioHub, kèm rule Sun Set và kết nối Creator được mô tả bên dưới. Không chọn backend Mock. OAuth trực tiếp TikTok xác minh hồ sơ của người dùng, không cấp quyền RioHub. Endpoint tìm kiếm sản phẩm theo từ khóa chưa nằm trong module này.

Việc tạo link trong workspace `/tiktok-affiliate` vẫn dùng Creator RioHub cấu hình ở server, không lấy username người dùng nhập để thay tài khoản RioHub. Trạng thái “Đã kết nối” lấy từ kết nối cục bộ riêng của `hungnttsd` hoặc hồ sơ OAuth TikTok thật của username khác. Sau khi kết nối, dán URL (gồm link rút gọn) hoặc ID rồi bấm **Tạo link TikTok**: server gọi `/product-links`, lưu tracking và trả link CatBack `/go/{token}` để sao chép/mở; riêng Sun Set dùng rule bên dưới. Metadata sản phẩm là best-effort; thiếu tên/ảnh/giá/hoa hồng không làm mất link đã tạo. API `GET /products` được dùng cho tra cứu riêng theo ID/link trực tiếp có ID; không bịa endpoint resolve URL. Màn hình cũ `/tiktok-affiliate/demo/authorize` vẫn chuyển về workspace.

## Kết nối tài khoản tại CatBack

Tài khoản có `IsTiktokDemo = true` phải nhập **Tên tài khoản TikTok (Username TikTok)** rồi POST **Kết nối tài khoản** với antiforgery. Server chuẩn hóa khoảng trắng, dấu @ đầu và chữ hoa/thường; lưu lựa chọn theo tài khoản CatBack trong `TikTokPendingCreatorUsername`.

- `hungnttsd`: mở `/tiktok-affiliate/authorize`, trang cục bộ dựng theo ảnh tham chiếu. Checkbox xanh thao tác được; Select all đồng bộ quyền, I acknowledge độc lập; Reject/menu ngôn ngữ không thao tác. POST Authorize chỉ lưu `TikTokWorkspaceConnected=true` và `TikTokWorkspaceCreatorUsername=hungnttsd`, không gọi OAuth và không sửa granted_scopes. GET/POST trang này đều kiểm tra lựa chọn phía server; URL hoặc POST trực tiếp không kích hoạt luồng cục bộ cho username khác.
- Username khác: chuyển tới luồng Creator authorization thật đã có. Cần cấu hình AppKey/AppSecret, HTTPS và Redirect URL trùng domain đã đăng ký. State ngẫu nhiên 256 bit, cookie Secure/HttpOnly/Lax và cache 15 phút gắn tenant/user/app/username; callback kiểm tra state, app_key, token Creator `user_type=1`, scope `creator.affiliate.info`, hồ sơ thật và region VN. Username trong hồ sơ phải khớp lựa chọn; nếu khác thì xóa token và báo kết nối lại. Không dùng hồ sơ hungnttsd hardcode. Callback thành công về workspace; không qua hai trang cục bộ. Không đưa query callback/code/token vào log hoặc thông báo.

Nếu bắt đầu từ localhost/host/port khác callback, chuyển sang workspace HTTPS trên domain callback với username điền sẵn. Người dùng đăng nhập trên domain đó và bấm Kết nối, không chuyển phiên/code/token giữa hai bản triển khai. Nếu domain đã khớp, backend HTTP sau TLS proxy không làm chặn nút kết nối; cookie state vẫn Secure, callback vẫn HTTPS. Query username chỉ điền form, không tự kết nối hay ngắt tài khoản qua GET.

Form Kết nối dùng điều hướng native (`data-turbo=false`), chỉ hiện loading tại nút “Đang kết nối…” và tắt overlay toàn màn hình của CatBack (`data-page-loading=false`) để giảm nháy khi chuyển sang trang authorize. Chặn submit lần hai trong khi chờ; trở lại bằng nút Back khôi phục nút để kết nối lại. Không can thiệp màn hình loading của domain TikTok.

Bấm **Ngắt kết nối** POST với antiforgery, xóa token OAuth trong cache và đặt trạng thái/lựa chọn cục bộ về rỗng, rồi quay về workspace. Form tạo link trong workspace cần kết nối. Mọi đường tạo link cần đăng nhập CatBack; hàm TikTok dùng chung còn kiểm tra user ID của chủ link trùng user đang đăng nhập. Trang Links tiếp tục dùng Creator RioHub cấu hình, không yêu cầu kết nối workspace. Trang chủ chỉ tạo link Shopee. Lịch sử link/đơn, key và Creator cấu hình vẫn giữ nguyên.

Sau POST **Authorize** cục bộ, server redirect tới `/tiktok-affiliate/authorization-success`. Trang chỉ hiện khi đang kết nối cục bộ `hungnttsd`; trường hợp khác về workspace. Giao diện dùng biểu tượng success SVG trích nguyên từ HTML người dùng cung cấp, hiện **Authorization successful** và nút **Done(5)**. Bộ đếm giảm mỗi giây về **Done(0)** rồi chuyển cố định tới `/tiktok-affiliate`; bấm Done quay lại ngay, không gửi lại POST. TempData giữ thông báo kết nối thành công qua trang trung gian. Không có JavaScript vẫn có link Done để quay lại thủ công.

Tab link lấy `AffiliateTracking` của tài khoản CatBack hiện tại và đúng Creator RioHub. “Ẩn liên kết” chỉ ẩn lịch sử trong CatBack; không gọi một API xóa RioHub chưa được tài liệu hóa. Tab đơn lấy sổ `RioHubOrderSku` đã sync, lọc chính xác theo tracking của người dùng ở `sub_id` hoặc `trace_id` SPECIFIC; không trả mọi đơn của Creator. General link không được gán cho người dùng dựa trên `sub_id`. SKU của đơn bị ẩn link vẫn được giữ trong lịch sử đơn.

Đơn hiển thị riêng tiền ước tính/thực nhận và trạng thái 1/2/3. Spec không cung cấp field giá trị đơn nên màn hình không tự tính/điền giá trị đó. Tỷ lệ sản phẩm ưu tiên `observed_commission.commission_rate`, rồi `commission.rate` + `shop_ads_commission.rate`, chia 10000 để chuyển số thô thành tỷ lệ decimal; dữ liệu `global` được ghi là tham khảo. Thiếu tiền/giá/rate hiển thị thiếu dữ liệu, không giả thành 0. Hoa hồng Creator không được tự coi là số hoàn tiền của khách.

Quyền truy cập workspace vẫn dùng cờ người dùng hiện có `IsTiktokDemo` để giữ dữ liệu/phân quyền; nhãn quản trị đã đổi thành “Cho phép tiếp thị liên kết TikTok”. Không cần migration đổi tên cờ. Các fixture service, store và tài nguyên màn hình cấp quyền mô phỏng đã được gỡ khỏi luồng chạy.

Giao diện khách hỗ trợ tạo TikTok qua **`/Links`** và workspace riêng. Chọn **TikTok Shop**, dán link sản phẩm và bấm **Tạo link TikTok**. Paste URL TikTok tự chọn đúng sàn. Kết quả có ảnh/tên sản phẩm khi RioHub trả metadata, sao chép link CatBack, mở TikTok, lưu danh sách và ẩn link. Link được phân quyền theo tài khoản CatBack.

Theo yêu cầu ngày 11/10/2026, **trang chủ `/` chỉ tạo link Shopee**: không render lựa chọn/hướng dẫn tạo link TikTok, không tự chọn sàn TikTok khi paste và từ chối URL TikTok trước khi gọi validate/create hoặc tạo pending action đăng nhập. Form `/Links` giữ cấu hình mặc định của platform picker. Không đổi CSS, API, phân loại link, tracking, đơn hàng hay hoàn tiền Shopee. Lịch sử link đã lưu không bị xóa.

Customer server dùng `RioHub:CreatorUsername` đã cấu hình, không nhận username Creator hoặc sub_id tùy ý từ trình duyệt. Lưu `AffiliateTracking` với platform TikTok; token `cb_<guid dạng N>` được gửi nguyên vẹn làm `sub_id`, mapping trực tiếp về chủ sở hữu link. `RioHubCreatorUsername` nằm trong ExtraProperties của tracking, giúp xử lý khi đổi Creator cấu hình. `/go/{token}` chuyển sang affiliate URL đã được RioHub trả về và kiểm tra host HTTPS TikTok; `/go/{token}/click` ghi nhận lượt bấm cho điều hướng di động. Phần tạo link dùng bảng hiện có.

Job Hangfire `riohub-tiktok-orders` đồng bộ đơn và hoa hồng vào hai bảng mới `affiliate."RioHubOrderSku"` và `affiliate."RioHubSyncCursor"`. Có migration `20261010082630_AddRioHubOrderSync`. Đây là sổ dữ liệu hoa hồng Creator; chưa ghi có ví khách hoặc ghép vào luồng quyết toán Shopee. Giao diện không hiển thị số hoàn tiền suy đoán cho link TikTok.

## Cấu hình

Mặc định `RioHub:Enabled=false` trong cấu hình chung. Để nhập key cục bộ, dùng file **`src/WebHoanTien.Web/appsettings.secrets.json`** (đã nằm trong `.gitignore`, tên file có `secrets` số nhiều). Thêm/cập nhật:

```json
"RioHub": {
  "Enabled": true,
  "ApiKey": "",
  "CreatorUsername": "hungnttsd",
  "SyncEnabled": false
}
```

Nhập key RioHub vào `ApiKey`, rồi khởi động lại ứng dụng. `CreatorUsername` phải là Creator đã kết nối với tài khoản RioHub cấp key; username `hungnttsd` được chủ dự án cung cấp cho luồng khách. Không đưa key thật vào file `.example.json`, cấu hình chung, trình duyệt, repository hay log.

Client đọc `RioHub:ApiKey` qua cấu hình .NET: biến `RioHub__ApiKey` và `CATBACK_RioHub__ApiKey` vẫn được hỗ trợ, nhưng file secrets được nạp sau nên ưu tiên cao hơn. Nếu giá trị cấu hình trống, client fallback sang `RIOHUB_API_KEY`. File `/etc/secrets/appsettings.secrets.json`, khi có, được nạp sau file secrets cục bộ.

Docker Compose đã ánh xạ các biến trong `.env.example`:

```dotenv
RIOHUB_ENABLED=true
RIOHUB_API_KEY=<đặt key thực tế trong .env cục bộ, không commit>
RIOHUB_CREATOR_USERNAME=hungnttsd
RIOHUB_SYNC_ENABLED=false
RIOHUB_SYNC_CRON="*/5 * * * *"
RIOHUB_INITIAL_SYNC_LOOKBACK_DAYS=30
RIOHUB_BASE_URL=https://riohub.vn/api/v1
RIOHUB_FALLBACK_BASE_URL_1=https://riohub.riokupon.com/api/v1
RIOHUB_FALLBACK_BASE_URL_2=https://riohub.riokupon.me/api/v1
```

Chạy trực tiếp bằng .NET: dùng file secrets trên hoặc đặt `RioHub__Enabled=true`, `RIOHUB_API_KEY` và `RioHub__CreatorUsername` trong môi trường của tiến trình web/secret manager. `.env` không được .NET tự nạp. Có thể cấu hình base bằng `RioHub__BaseUrl` và hai biến `RioHub__FallbackBaseUrls__0`, `RioHub__FallbackBaseUrls__1`. Khởi động lại tiến trình sau khi đổi cấu hình/key.

Các tùy chọn không bí mật trong `appsettings.json`:

- `TimeoutSeconds=15`: thời gian chờ header và một khoảng riêng để đọc body.
- `MaxRateLimitRetries=2`: tối đa hai lần retry khi HTTP 429, dùng lại tên miền hiện tại.
- `MaxRetryAfterSeconds=30`: nếu nhà cung cấp yêu cầu chờ lâu hơn thì trả lỗi kèm `Retry-After` để caller lên lịch lại; không rút ngắn thời gian nhà cung cấp yêu cầu.
- `SyncEnabled=false`: bật sau khi áp dụng migration; job chạy mỗi 5 phút theo `SyncCron`.
- `InitialSyncLookbackDays=30`: chính sách CatBack cho lần đầu, không phải giới hạn API. Đặt `InitialSyncFromUnix` để chọn mốc Unix UTC cụ thể. Mốc đầu được lưu bền trước khi kéo trang, không bị trượt khi lần đầu thất bại. Sau khi đã có cursor, thay hai tùy chọn này không tự reset cursor.

Client dùng HTTPS với kiểm tra chứng chỉ mặc định; tắt redirect và cookie. Base URL chỉ được nằm trên ba tên miền RioHub đã nêu, đường dẫn `/api/v1`, không query/userinfo/port tùy chỉnh. Header key được che trong logging của HttpClient.

## API quản trị CatBack

Tất cả route yêu cầu đăng nhập và quyền `Affiliate.Admin.Settings`. Chúng cho phép truy vấn dữ liệu theo Creator trong tài khoản RioHub nên không dùng trực tiếp làm API đơn hàng của khách. POST tuân theo cơ chế chống CSRF hiện tại của ứng dụng khi dùng cookie.

| Method | Route CatBack | Thao tác |
| --- | --- | --- |
| POST | `/api/app/admin/riohub/links` | Tạo link có sub_id |
| POST | `/api/app/admin/riohub/product-links` | Tạo link kèm dữ liệu sản phẩm nếu có |
| POST | `/api/app/admin/riohub/general-links` | Tạo deep link, tối đa 50 ID |
| GET | `/api/app/admin/riohub/links` | Đọc danh sách link |
| GET | `/api/app/admin/riohub/orders` | Đọc đơn, có lọc thời gian cập nhật |
| GET | `/api/app/admin/riohub/products` | Đọc tối đa 100 ID sản phẩm |

Tên request của CatBack là **camelCase**, client chuyển sang snake_case khi gọi RioHub. Phản hồi thành công giữ nguyên **snake_case**.

Body tạo link:

```json
{
  "creatorUsername": "your_creator",
  "productUrl": "https://vt.tiktok.com/REPLACE_WITH_REAL_LINK/",
  "subId": "catback-campaign-placement-variant"
}
```

Dùng đúng một trong `productUrl` hoặc `productId`. `productUrl` phải thuộc tên miền HTTPS TikTok; ID phải là chuỗi số. `subId` có tối đa bốn vị trí, tổng 1–128 ký tự, mỗi vị trí chỉ dùng chữ ASCII, số hoặc `_`. Không đặt UUID có dấu `-` vào một vị trí; dùng dạng `Guid.ToString("N")`. `abc-def--` giữ hai vị trí cuối rỗng. Caller chịu trách nhiệm lưu mã tracking và chủ sở hữu trước khi dùng cho hoàn tiền. `channel` tùy chọn: 1–64 ký tự `[A-Za-z0-9_-]`.

Body deep link:

```json
{
  "creatorUsername": "your_creator",
  "productIds": ["REPLACE_WITH_NUMERIC_PRODUCT_ID"]
}
```

`campaignId` tùy chọn. Đọc cả `links` lẫn `failed` trong kết quả; không coi HTTP 200 là mọi sản phẩm thành công. Module không tự gắn mã theo dõi vào deep link.

Ví dụ query phía CatBack (thay giá trị minh họa trước khi gọi):

```text
GET /api/app/admin/riohub/products?creatorUsername=your_creator&productId=123,456
GET /api/app/admin/riohub/links?creatorUsername=your_creator&sub1=catback&page=1&pageSize=50
GET /api/app/admin/riohub/orders?creatorUsername=your_creator&updateTimeStart=1791504000&updateTimeEnd=1791590400&page=1&pageSize=200
```

Orders hỗ trợ `orderId` (CSV tối đa 200), `productId` (CSV tối đa 100), `traceId` (CSV tối đa 200), `status`, `settlementStatus`, `contentType`, `fullyRefunded`, `subId`, `sub1`…`sub4`, `timeStart/End`, `updateTimeStart/End`, `page/pageSize`. Các mốc thời gian nhận Unix giây; cận cuối loại trừ. Route proxy này chỉ trả một trang; job đồng bộ riêng tự duyệt hết các trang. Không suy ra đã lấy đủ SKU chỉ từ số order ID yêu cầu.

Lỗi validation trả 400. Lỗi upstream trả 502 và `upstreamStatus` (401, 403, 404, 422, 5xx...), tránh biến lỗi key RioHub thành lỗi đăng nhập CatBack. Riêng 429 giữ status 429 và header `Retry-After`. Khi nhận mã `product_not_promotable`, trả `code` và thông báo sản phẩm không có hoa hồng/chưa được duyệt/bị hạn chế. Không retry 422. Spec không chốt envelope lỗi, nên client chỉ nhận diện literal mã đã biết trong JSON, không giả định field `code`/`error` của upstream và không chuyển tiếp message/body tùy ý.

## Hàm tạo link trong C#

Inject `IRioHubAffiliateClient` vào service phía server rồi gọi helper. Creator lấy từ cấu hình; helper nhận URL hoặc ID, kiểm tra request trước khi gửi `POST /partner/tiktok/affiliate/links`, kiểm tra Creator/sub_id/link trả về và trả chuỗi `affiliate_link` nguyên bản.

```csharp
// Ví dụ trong một service đã inject IRioHubAffiliateClient vào _rioHub.
// Thay URL minh họa bằng sản phẩm thật trước khi gọi.
string link = await _rioHub.CreateAffiliateLinkAsync(
    "https://vt.tiktok.com/REPLACE_WITH_REAL_LINK/",
    "fb-ads-01", cancellationToken);

// Hoặc: await _rioHub.CreateAffiliateLinkAsync("<product_id dạng số>", "fb-ads-01");
```

`RioHubApiException.ErrorCode == "product_not_promotable"` là lỗi nghiệp vụ, hiển thị thông báo cho người dùng và chọn sản phẩm khác. Helper không lưu mapping CatBack; giao diện khách dùng `TikTokAffiliateLinkCreator` để lưu tracking và lấy thêm metadata qua `/product-links`.

## Job đồng bộ đơn và hoa hồng

`RioHubOrderSyncJob` lấy một mốc `end` UTC Unix cố định đầu mỗi lần chạy, gửi `update_time_start = watermark - 600` (không âm), `update_time_end = end`, `page_size = 200`, tăng `page` đến đủ `total`. Lần đầu dùng mốc cấu hình. Khóa Hangfire ngăn hai worker chạy đồng thời; không bật auto retry toàn job. Lần lịch tiếp theo dùng watermark cũ nếu lần trước lỗi. Retry 429 trong client vẫn có giới hạn theo `Retry-After`.

Mỗi trang lưu trong một transaction. Nếu trang sau lỗi, dữ liệu trang trước có thể đã lưu nhưng watermark không thay đổi; chạy lại upsert cùng khóa nên không tạo dòng trùng. Chỉ sau khi lấy và lưu đủ tất cả trang, transaction cuối cập nhật watermark thành `end`. Metadata sai, thiếu dòng, trùng khóa giữa các trang hoặc `total` thay đổi khiến job thất bại và giữ watermark cũ.

Unique key là `(CreatorUsername, OrderId, SkuId)`: trong từng Creator, đúng khóa `(order_id, sku_id)`; SKU cập nhật không xóa SKU khác của cùng đơn. Chuyển Creator không dùng lại cursor/dòng của Creator cũ. Dòng có `update_time` cũ hơn bản đã lưu không ghi đè. Cùng timestamp vẫn cập nhật để phản ánh thay đổi quyết toán RioHub. Khi `update_time` thiếu, chỉ coi snapshot GET là trạng thái hiện tại; không bịa timestamp từ `settled_at` hoặc `create_time`.

Logic trạng thái dùng đúng `status`: 1 chờ, 2 đã chốt, 3 hủy/hoàn. Các trạng thái passthrough lưu nguyên bản trong `ProviderJson`. `EstimatedCommission` và `ActualCommission` là `decimal`/PostgreSQL `numeric`, không qua float, không làm tròn tiền khi sync. `actual_commission` null được giữ null; không thay bằng tiền ước tính hoặc cộng thêm breakdown. Dữ liệu vượt khả năng biểu diễn chính xác của .NET decimal làm job dừng thay vì âm thầm làm tròn.

`CreatedAtUtc` ưu tiên `create_time`, rồi `time_created_iso`, rồi `time_created` trần đọc UTC. `ProviderUpdatedAtUtc` dùng `update_time` khi có. `SettledAtUtc` dùng `settled_at_iso`, fallback `settled_at` trần UTC; trạng thái 3 xóa mốc này. Mốc quyết toán không được suy ra từ `update_time`. Chỉ đổi UTC sang giờ Việt Nam khi hiển thị.

### Chạy tích hợp

1. Điền key và `CreatorUsername=hungnttsd` vào secrets hoặc môi trường như trên. Giữ `SyncEnabled=false` trong lúc cập nhật database.
2. Áp dụng migration bằng DbMigrator hiện có, với connection string của database cần dùng trong `src/WebHoanTien.DbMigrator/appsettings.secrets.json` hoặc môi trường. Chạy từ đúng thư mục vì DbMigrator đọc config theo working directory:

   ```powershell
   Set-Location E:\HungNT\WebHoanTien\src\WebHoanTien.DbMigrator
   dotnet run
   ```

3. Đặt `RioHub:SyncEnabled=true` trong secrets web (Docker: `RIOHUB_SYNC_ENABLED=true`). Có thể chọn mốc bắt đầu cụ thể bằng `RioHub:InitialSyncFromUnix`; mặc định lấy 30 ngày gần nhất ở lần đầu.
4. Khởi động lại web. Trong Visual Studio dùng profile web hiện có; từ terminal:

   ```powershell
   Set-Location E:\HungNT\WebHoanTien\src\WebHoanTien.Web
   dotnet run
   ```

5. Tạo link ở `/` hoặc `/Links` → chọn TikTok Shop → dán URL thật. Hoặc vào `/tiktok-affiliate` nếu được cấp quyền → dán URL/ID → **Tạo link TikTok**. Quản trị viên xem `/hangfire` → Recurring Jobs → `riohub-tiktok-orders`, dùng **Trigger now** để chạy ngay hoặc chờ lịch 5 phút. Đây là thao tác gọi API thật.

Xem dữ liệu đã sync bằng công cụ PostgreSQL:

```sql
SELECT "OrderId", "SkuId", "SubId", "Status", "Currency",
       "EstimatedCommission", "ActualCommission", "SettledAtUtc"
FROM affiliate."RioHubOrderSku"
WHERE "CreatorUsername" = 'hungnttsd'
ORDER BY "CreatedAtUtc" DESC;

SELECT "InitialStartUnix", "WatermarkUnix", "LastSucceededAtUtc", "LastFetchedCount"
FROM affiliate."RioHubSyncCursor"
WHERE "CreatorUsername" = 'hungnttsd';
```

Không có route đồng bộ mở cho khách; đây là job server và dữ liệu cấp Creator. Workspace chỉ đọc các SKU đã đối chiếu chính xác với tracking người dùng; bấm “Làm mới” đọc database, không kéo toàn bộ Creator qua API hoặc chạy job. Trước khi ghi có ví cần áp dụng tỷ lệ chia hoa hồng và quy trình quyết toán CatBack. Với general link, attribution phải dùng `trace_id`, không dùng sub_id `open_api_san`.

## Thông tin mặc định cho sản phẩm Sun Set

`TikTokProductOverrides` áp dụng riêng cho product ID `1732226714601031550`: tên `Cây Sun Set Thủy Sinh – gắn sẵn giá thể _Không Cần CO₂`, ảnh tĩnh `/products/tiktok/tiktok-product-1732226714601031550.png`, giá 36.100 VND và hoa hồng Creator cố định 3.600 VND (không phải rate thô 3600). Với ID hoặc link trực tiếp `/view/product/{id}` hay `/vn/pdp/{id}`, không gọi RioHub, không cần key/Creator hợp lệ để trả metadata và lưu link gốc. Giữ lại link affiliate thật đã tạo trước đó nếu có cùng URL, ID và Creator. Link gốc được đánh dấu `TikTokProductUrlFallback=true` trong ExtraProperties, hiển thị trạng thái riêng và không dùng token này để truy vấn đơn RioHub. Rule không ghi đè hoa hồng đơn hàng hoặc số tiền hoàn khách. Link rút gọn mới vẫn cần RioHub giải ra ID trước khi xác định sản phẩm; không suy đoán ID nếu API lỗi.

## Đơn Sun Set tạo thủ công riêng cho hungnttsd

Tab Đơn hàng Affiliate bổ sung đúng một dòng `CB-TT-20261009-01` chỉ khi người dùng đang hoạt động có `IsTiktokDemo = true`, `TikTokWorkspaceConnected=true` và username đã kết nối là `hungnttsd`, đọc từ database phía server. Ngắt kết nối sẽ ẩn đơn; username khác không có đơn test. Sản phẩm `1732226714601031550`, một sản phẩm giá 36.100 VND, hoa hồng ước tính 3.600 VND, trạng thái Pending, ngày 09/10/2026 lúc 12:00 UTC+7; hoa hồng thực nhận và ngày chốt để trống. Dòng có `Source = Manual`, nhãn “Tạo thủ công”; không cần link đã tạo hoặc RioHub API. Đây là dòng cố định trong ứng dụng, không insert vào sổ RioHub, không đồng bộ provider hoặc ghi có ví. Làm mới nhiều lần vẫn chỉ có một dòng. Các dòng RioHub thật vẫn được lọc theo token thuộc người dùng và hiển thị cùng danh sách.

## Kết nối và retry

Theo thứ tự base cấu hình rồi hai host dự phòng. Singleton ghi nhớ host đã trả HTTP trong phạm vi một tiến trình, dùng nó trước ở request sau. Khi host đó lỗi kết nối, thử host tiếp theo theo vòng; mỗi host tối đa một lượt failover cho một thao tác. HTTP bất kỳ (kể cả 3xx/4xx/5xx) không gây đổi host. Retry 429 có giới hạn và tôn trọng `Retry-After` dạng giây hoặc HTTP date. Không retry 5xx.

DNS, lỗi kết nối, TLS/chứng chỉ hoặc timeout trước khi nhận header cho phép failover. Hủy từ caller không gây failover. Body quá 4 MiB, lỗi đọc body, timeout sau header hay JSON không hợp lệ đều dừng, không gửi lại. Hạn mức phía RioHub vẫn được áp dụng; module chưa có bộ đếm quota chia sẻ giữa các replica.

Timeout POST có thể xảy ra sau khi nhà cung cấp đã tạo link. Tài liệu chưa xác nhận idempotency key cho thao tác này: failover có nguy cơ tạo link trùng. Dùng cùng sub_id xuyên suốt và tra danh sách link trước khi caller chủ động thử lại. Không dùng client này để tự động ghi tiền.

## Webhook

Đã có helper `RioHubWebhookSignature.Verify(rawBody, header, signingSecret, now, timestampTolerance)` theo HMAC-SHA256 trên `t + "." + raw_body`, so sánh constant-time. Đọc signing secret riêng từ biến môi trường `RIOHUB_SIGNING_SECRET` tại nơi dùng helper. Truyền byte body nguyên bản trước parse JSON; không serialize lại để kiểm chữ ký. `timestampTolerance` là chính sách replay tùy chọn; để null nếu chưa xác minh RioHub có ký timestamp mới khi retry muộn hay không.

**Chưa mở route nhận webhook.** Helper xác minh chữ ký không phải inbox hay xử lý đơn. Chưa cấu hình Callback URL ở RioHub vào ứng dụng này. Để bật cần thêm inbox lưu bền với unique event_id, queue và worker đối soát: chỉ trả 2xx sau khi nhận bền vững trong 5 giây, giữ khoảng 5 phút, gom tối đa 200 order ID mỗi request và duyệt đủ trang. Chống event cũ ghi đè và xác minh trạng thái hiện tại từ API trước khi cập nhật đơn.

## Trước khi đưa vào hoàn tiền

Lưu mapping người dùng–tracking ở server và kiểm tra attribution/currency theo từng dòng SKU. Đơn đã nằm trong sổ RioHub riêng, chưa được nhập vào bảng giao dịch/ví khách CatBack. Không dùng tiền ước tính để chi trả; giữ riêng giá trị thực nhận và các thành phần chi tiết để tránh cộng hai lần. General link và specific link cần cách đối chiếu mã khác nhau; không suy ra người mua từ Creator.

Build web thành công ra thư mục tạm (0 lỗi; 3 cảnh báo nullable ở `TikTokCreatorConnection.cs`, ngoài phần RioHub). Tạo migration EF thành công; migration chỉ thêm hai bảng RioHub và index. EF CLI hiện cài bản 6.0.35, runtime 8.0.8 nên tool có cảnh báo phiên bản; đã rà migration/snapshot. Chưa áp dụng migration vào database, chạy test/Playwright, gọi API thật hoặc đăng ký webhook trong phiên làm việc này.
