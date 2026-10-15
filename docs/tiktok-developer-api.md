# TikTok Developer API qua CatBack

Triển khai source ngày 15/10/2026. Mở bốn API nghiệp vụ Creator giữ method/path và JSON TikTok, cùng endpoint riêng của CatBack để lấy token. Xác thực bằng API key CatBack; không phải proxy mở cho mọi endpoint TikTok.

## Cấp quyền và kết nối

1. Quản trị vào **Quản lý người dùng**, sửa user, bật **Cho phép TikTok Developer API (Creator riêng)** (`CanUseTikTokDeveloperApi = true`). Mặc định tắt; quyền này độc lập với `IsTiktokDemo` và quyền quản trị Creator chung. User không tự sửa flag từ hồ sơ cá nhân.
2. User đăng nhập CatBack, mở **TikTok Developer API** tại `/Developer/TikTok`, bấm **Đăng nhập TikTok và cấp quyền Creator của tôi** và cấp quyền trên TikTok thật. Thao tác bắt đầu trên `https://catback.id.vn` để giữ đúng phiên callback đã đăng ký. CatBack lưu token mã hóa phía server; backend của user có thể lấy token riêng qua endpoint token bên dưới. Trang quản lý không hiển thị token TikTok.
3. User tạo API key, sao chép ngay và lưu trong secret của backend riêng. Key đầy đủ chỉ hiện ở phản hồi tạo key, không thể đọc lại. Có thể thu hồi từng key tại trang này.
4. Backend của user gọi `https://catback.id.vn` với header `X-CatBack-Api-Key`. Không cần gửi AppKey, chữ ký, timestamp hoặc access token TikTok.

CatBack xác định chủ tài khoản từ key, lấy **Creator riêng của chính chủ key** để gọi TikTok. Không có tham số chọn user/Creator khác, không dùng cookie đăng nhập để thay chủ key và không fallback sang Creator chung, RioHub hoặc mock. Trả nguyên JSON upstream thay vì DTO đơn/link nội bộ CatBack. Các request này không tự ghi đơn vào sổ hoàn tiền hoặc cộng ví.

## API

### Lấy access token và refresh token

**POST `https://catback.id.vn/api/developer/tiktok/token`**. Đây là endpoint riêng của CatBack, không phải endpoint TikTok và không thực hiện OAuth từ đầu. User phải kết nối Creator riêng thành công trước đó.

- Header bắt buộc: `X-CatBack-Api-Key: <key của user>`.
- Chỉ chấp nhận HTTPS. Không gửi query/body, user ID, Open ID, refresh token, auth code hoặc AppSecret.
- Chủ key phải còn hoạt động và còn quyền `CanUseTikTokDeveloperApi`; key phải chưa thu hồi/hết hạn. Dùng chung quota mỗi user với các API Developer khác.
- Chỉ lấy token từ `TikTokDeveloperAccounts` đúng owner của key. Không trả token Creator chung của admin, kể cả người gọi là admin; chưa có kết nối riêng thì trả HTTP 409.
- Nếu access token sắp hết hạn trong 5 phút, CatBack tự refresh qua client hiện có, lưu cặp mới theo lease/version rồi trả về. Muốn lấy cặp token hiện hành thì gọi lại endpoint này; không cần truyền refresh token lên CatBack.

```powershell
$headers = @{ 'X-CatBack-Api-Key' = $env:CATBACK_TIKTOK_API_KEY }
# Không in $result hoặc ghi response vào log: chứa credential TikTok thật.
$result = Invoke-RestMethod -Method Post -Headers $headers `
    -Uri 'https://catback.id.vn/api/developer/tiktok/token'
# Lưu $result.data.access_token / refresh_token vào secret phía backend của bạn.
```

Response mẫu chỉ chứa placeholder, các mốc bên dưới là ví dụ:

```json
{
  "code": 0,
  "message": "Success",
  "request_id": "<CatBack trace identifier>",
  "data": {
    "access_token": "<CREATOR_ACCESS_TOKEN>",
    "refresh_token": "<CREATOR_REFRESH_TOKEN>",
    "access_token_expire_in": 1792036800,
    "refresh_token_expire_in": 1823572800,
    "open_id": "<YOUR_CREATOR_OPEN_ID>",
    "user_type": 1,
    "granted_scopes": ["creator.affiliate.info", "creator.affiliate_collaboration.read", "creator.affiliate.share_link.read"]
  }
}
```

Hai trường `*_expire_in` là **mốc Unix giây UTC tuyệt đối**, cùng ý nghĩa mà client OAuth TikTok hiện có đọc, không phải số giây còn lại. Danh sách scope thực tế có thể chỉ là một phần quyền app đã bật. Response có `Cache-Control: no-store`, `Pragma: no-cache`, không audit/log request trong ứng dụng và không đưa token vào query/cookie/trang quản trị.

API key CatBack giờ có quyền lấy token thô của chính owner. Chỉ lưu key/token trong backend và không log response. Thu hồi key CatBack chặn lần lấy tiếp theo nhưng **không thu hồi token TikTok đã xuất ra**. Token vẫn chịu vòng đời/thu hồi phía TikTok; luôn lưu lại cả cặp token mới nhất và expiry khi lấy lại qua CatBack.

Token thuộc app CatBack. API này không trả AppSecret; gọi trực tiếp TikTok hoặc refresh trực tiếp theo OAuth vẫn cần các thông tin/chữ ký app tương ứng. Bốn API nghiệp vụ CatBack đã ký request giúp backend user; có thể tiếp tục dùng API key cho các endpoint đó mà không quản lý chữ ký TikTok.

### Theo dõi kết nối trong quản trị

Admin có quyền `Affiliate.Admin.Settings` vào **Kết nối Creator của user** tại `/Admin/TikTokConnections`. Trang hiển thị Creator hoàn tiền chính và danh sách riêng của Developer: user CatBack, Username TikTok/Open ID, trạng thái, access/refresh expiry, scope, số key đang hiệu lực và thời gian cập nhật. Có tìm kiếm, lọc trạng thái, phân trang 50 user.

Số đếm là dữ liệu CatBack ghi nhận, **không phải số tài khoản đang online hoặc tổng mọi tài khoản đã authorize trong TikTok Partner Center**. Chưa tìm thấy API Creator công khai để liệt kê tổng tài khoản đã cấp quyền cho app trong các nguồn chính thức đã kiểm tra ngày 15/10/2026. OAuth là luồng từng Creator; CatBack lưu từng callback thành công và kiểm tra token/scope khi dùng API. Không thay bằng Get Authorized Shops (Seller) hoặc API Partner. Không gọi API/refresh token khi mở danh sách quản trị, nên việc thu hồi bên TikTok có thể chưa phản ánh cho đến request tiếp theo.

Các số tổng: user có hồ sơ Developer (kể cả chỉ tạo key/chưa authorize), user có kết nối Active với refresh token chưa hết hạn, số Open ID riêng biệt trong nhóm đó, và kết nối cần cấp quyền lại. User bị khóa/thu hồi quyền Developer vẫn có thể còn metadata kết nối nhưng không qua được kiểm tra quyền khi gọi API.

### Tách token hoàn tiền và token Developer

- Luồng hoàn tiền (`TikTokCreatorApi`, `TikTokCreatorConnection`, đồng bộ đơn) chỉ đọc credential `IsPrimary` trong `TikTokCreatorCredentials`, được ghi qua state admin. Không nhận user ID/Creator tùy ý và không chọn token theo người đang đăng nhập.
- Chỉ admin đang hoạt động có quyền Settings mới bắt đầu kết nối hoặc ngắt Creator hoàn tiền. Callback kiểm tra người khởi tạo vẫn là admin đang hoạt động; state Developer không thể ghi vào kho này. Flag `IsTiktokDemo` không còn cho phép thay đổi kết nối chung, các quyền xem/đồng bộ đơn đã có giữ nguyên.
- Luồng Developer chỉ đọc `TikTokDeveloperAccounts` đúng owner từ API key, mã hóa bằng purpose riêng theo owner; callback ghi vào đây và không nâng thành Creator chính. Kho Creator chính không sẵn sàng thì hoàn tiền báo lỗi, không fallback token riêng.
- Danh sách quản trị chỉ đọc metadata, không giải mã/hiển thị token hoặc key. Thay đổi này không cần migration mới ngoài `AddTikTokDeveloperApi` đã thêm trước đó, chưa được tự áp dụng trong phiên sửa source.

### Các endpoint được mở

| Chức năng | Method và path | Scope Creator cần có |
| --- | --- | --- |
| Hồ sơ Creator | `GET /affiliate_creator/202508/profiles` | `creator.affiliate.info` |
| Sản phẩm theo ID | `POST /affiliate_creator/202509/open_collaborations/products` | `creator.affiliate_collaboration.read` |
| Tạo general sharing link | `POST /affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch` | `creator.affiliate.share_link.read` |
| Đơn hàng | `POST /affiliate_creator/202410/orders/search` | `creator.affiliate_collaboration.read` |

Mỗi request kiểm tra quyền user, key còn hiệu lực và `granted_scopes` của Creator. Thiếu một scope chỉ chặn endpoint tương ứng. API tạo link này không dùng hoặc coi `creator.affiliate.link.write` là alias. Chưa mở các API tracking link chưa xác minh mapping.

Các ví dụ PowerShell sau lấy key từ biến môi trường `CATBACK_TIKTOK_API_KEY` do người dùng tự thiết lập; không đưa giá trị thật vào source hoặc command history.

```powershell
$headers = @{ 'X-CatBack-Api-Key' = $env:CATBACK_TIKTOK_API_KEY }
$baseUrl = 'https://catback.id.vn'

# Hồ sơ
Invoke-RestMethod -Method Get -Headers $headers -Uri "$baseUrl/affiliate_creator/202508/profiles"

# Sản phẩm: tối đa 50 ID, CSV trong query; body {} theo sample TikTok
$productIds = '1732226714601031550'
Invoke-RestMethod -Method Post -Headers $headers -ContentType 'application/json' -Body '{}' `
    -Uri "$baseUrl/affiliate_creator/202509/open_collaborations/products?product_ids=$productIds"

# Tạo link: material.ids là mảng string; tối đa 50 ID
$body = @{ material = @{ ids = @($productIds); type = 'PRODUCT' } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Headers $headers -ContentType 'application/json' -Body $body `
    -Uri "$baseUrl/affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch"

# Đơn: page_size 1–100. Bộ lọc create_time_ge/create_time_lt là Unix giây.
$body = @{
    create_time_ge = [DateTimeOffset]::UtcNow.AddDays(-7).ToUnixTimeSeconds()
    create_time_lt = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
} | ConvertTo-Json
$result = Invoke-RestMethod -Method Post -Headers $headers -ContentType 'application/json' -Body $body `
    -Uri "$baseUrl/affiliate_creator/202410/orders/search?page_size=100"

# Trang kế tiếp: giữ bộ lọc, URL-encode data.next_page_token vào query page_token.
# Kiểm tra code và data; HTTP 200 không tự đồng nghĩa nghiệp vụ thành công.
```

Với bốn API nghiệp vụ, CatBack ký query và body JSON thực gửi, tự thêm `app_key`, `timestamp`, `sign` và header `x-tts-access-token`. Query/body theo API Reference TikTok, không đổi tên field hay chuẩn hóa tiền/ID. Phản hồi upstream giữ HTTP status và toàn bộ JSON `code`, `message`, `request_id`, `data`, bao gồm lỗi nghiệp vụ, link/deep link và phân trang; chuyển tiếp `Retry-After` khi có. Các API nghiệp vụ không chuyển token, secret hoặc nội dung không phải JSON nghiệp vụ ra client; xuất token chỉ qua endpoint token riêng đã mô tả.

JSON sản phẩm trả nguyên giá/hoa hồng TikTok; không tự áp tỷ lệ hoàn tiền CatBack. Đơn trả nguyên dữ liệu Creator được TikTok cho phép, không lấy sổ đơn chung CatBack. Với attribution, bên tích hợp tự quản lý click/event ID theo hướng dẫn của TikTok; CatBack không sửa link trả về hoặc gán đơn bằng product ID/thời gian.

## Lỗi của CatBack

Lỗi xảy ra trước khi nhận phản hồi nghiệp vụ TikTok dùng HTTP status tương ứng và envelope cùng tên field, với `code` âm để phân biệt mã TikTok:

```json
{"code":-401,"message":"API key CatBack thiếu hoặc không hợp lệ.","request_id":"<CatBack trace identifier>","data":null}
```

| HTTP / code | Ý nghĩa |
| --- | --- |
| 400 / -400 | Query/body không hợp lệ hoặc chứa credential TikTok |
| 401 / -401 | Thiếu key, key sai, đã thu hồi hoặc hết hạn |
| 403 / -403 | User bị khóa/chưa được cấp quyền hoặc Creator thiếu scope |
| 409 / -409 | Chưa kết nối, đã ngắt, token cần cấp quyền lại |
| 413 / -413 | Body lớn hơn 64 KiB |
| 429 / -429 | Vượt quota CatBack hoặc refresh TikTok bị giới hạn; đọc `Retry-After` |
| 502, 503, 504 | Lỗi kết nối/phản hồi upstream hoặc dịch vụ tạm thời chưa sẵn sàng |

Không tự replay thao tác tạo link khi timeout/lỗi; phía gọi cần kiểm tra trước khi gửi lại. API đọc chỉ tự refresh rồi thử lại tối đa một lần khi TikTok trả mã token hết hạn `105002`. Mã token không hợp lệ `105001` đánh dấu kết nối riêng cần cấp quyền lại.

## Vận hành

- API key ngẫu nhiên 256 bit; DB chỉ lưu SHA-256 và phần hiển thị đã che. Tối đa 10 key đang hiệu lực mỗi user, mặc định hết hạn sau 90 ngày.
- Mặc định 60 request/phút **mỗi user**, gộp mọi key và instance bằng bộ đếm DB; TikTok vẫn áp quota riêng của app/API. Chưa có hàng đợi tăng quota của TikTok.
- Cấu hình không bí mật tùy chọn: `TikTokDeveloper:RequestsPerMinute` (1–600, mặc định 60), `TikTokDeveloper:KeyLifetimeDays` (1–365, mặc định 90). AppKey/AppSecret dùng cấu hình Creator phía server đang có, không đưa vào browser hoặc trả qua API.
- Token mã hóa Data Protection, purpose riêng theo AppKey/user/OpenID; tự refresh bằng lease/version trong DB. Mỗi user có một Creator đang kết nối cho AppKey cấu hình. Chỉ hỗ trợ user host, chưa mở theo tenant.
- State ngẫu nhiên, lưu hash, hết hạn 10 phút, dùng một lần và buộc đúng user/phiên trình duyệt. Callback chung đã đăng ký `/api/tiktok-shop/oauth/callback` phân biệt Developer bằng prefix state; không ghi vào credential Creator chính.
- Ngắt kết nối xóa credential riêng tại CatBack và vô hiệu phiên authorize đang chờ; không thu hồi quyền phía TikTok. Key cũ không dùng được khi chưa kết nối lại. Thu hồi key hoặc bỏ quyền user chặn request mới; request đã gửi sang TikTok có thể hoàn tất.
- Chỉ truyền key qua HTTPS từ backend. Tránh log header, body trang tạo key hoặc callback OAuth tại reverse proxy/APM. Code ứng dụng tắt audit/log request nhạy cảm và dùng HTTP client TikTok không log token URL.

Migration `20261015033312_AddTikTokDeveloperApi` tạo ba bảng `affiliate.TikTokDeveloperAccounts`, `TikTokDeveloperAuthStates`, `TikTokDeveloperApiKeys`. Cần áp dụng migration qua quy trình triển khai hiện có **trước khi sử dụng**. Lần sửa source này không chạy migrator, không thay quyền/key/token user hoặc gọi API Creator thật. Không đổi luồng Shopee và không nhập credential Creator chung vào bảng riêng.

## Nguồn đối chiếu

Ngày 15/10/2026 đã đọc lại metadata API Reference chính thức (`api/v1/document/api_meta?src_document_id=...`) của cả bốn API. Path/method/query/body khớp các nguồn sau; key scope đối chiếu thêm bảng đã xác minh tại [TikTok Creator API](tiktok-shop-creator-api.md).

- [Get Creator Profile 202508](https://partner.tiktokshop.com/docv2/page/get-creator-profile-202508).
- [Get Open Collaboration Product List By Product Ids 202509](https://partner.tiktokshop.com/docv2/page/68d4e70c0b3977048877ec65).
- [Generate General Sharing Links 202505](https://partner.tiktokshop.com/docv2/page/68465b060dbfb004a8c313b6).
- [Search Creator Affiliate Orders 202410](https://partner.tiktokshop.com/docv2/page/6736985bd99b7d02fd715482).
- [Creator authorization guide](https://partner.tiktokshop.com/docv2/page/678e3a362dccb8030ea6f98c).
