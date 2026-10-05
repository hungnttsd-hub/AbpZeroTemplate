# CatBack — TikTok Shop Affiliate Creator API

Ngày đối chiếu: 05/10/2026 (Asia/Bangkok). Nguồn: tài liệu chính thức TikTok Shop Partner Center, đọc nội dung trang đã tải JavaScript. Đây là bảng đối chiếu để triển khai; chưa phải xác nhận API đã gọi thành công bằng token CatBack.

**Cập nhật cho Review Demo:** chủ dự án xác nhận thêm scope Active `creator.affiliate.share_link.read` trong yêu cầu xây module mock. Các bảng ba scope bên dưới ghi lại bằng chứng HTML trước đó; scope mới chưa có bản chụp Manage scope bổ sung. Kế hoạch API thật là Creator Generate General Link `202505`, đã đối chiếu Required scope ở API Reference. Chưa có CreatorAccessToken vì đang chờ Creator testing account Vietnam. Giai đoạn hiện tại không gọi API thật; xem `tiktok-affiliate-review-demo.md`.

## Thông tin app do chủ dự án cung cấp

- App: CatBack; Custom; category Creator collaborations; target market Vietnam.
- App status On; Partner registration Approved.
- HTML Manage scope của CatBack do chủ dự án cung cấp xác nhận `Active 3` và ba scope sau. Đây là bằng chứng cấu hình app từ bản chụp HTML; chưa có Creator token để kiểm tra `granted_scopes`:

| Scope ID | Scope name | Scope key trong HTML Manage scope |
| --- | --- | --- |
| 434372 | Affiliate Information | `creator.affiliate.info` |
| 1873988 | Manage Affiliate Tracking Links | `creator.affiliate.link.write` |
| 1021508 | Read Creator Affiliate Collaborations | `creator.affiliate_collaboration.read` |

HTML cũng hiển thị target Vietnam / Beta Testing, giới hạn beta 25 authorized users và Redirect URL `https://catback.id.vn/api/tiktok-shop/oauth/callback`. Không suy ra production/public app đã được review từ trạng thái On. Mục Authorization trong bản chụp hiển thị Seller; luồng Creator vẫn phải dùng hướng dẫn Creator authorization riêng.

## Nguồn tham chiếu

- [Affiliate Integration](https://partner.tiktokshop.com/docv2/page/affiliate-integration): bảng mapping scope ID và phân biệt Creator/Seller authorization.
- [Creator Authorization Guide](https://partner.tiktokshop.com/docv2/page/678e3a362dccb8030ea6f98c): hiện chuyển tới [creator-authorization-guide](https://partner.tiktokshop.com/docv2/page/creator-authorization-guide).
- URL được cung cấp làm API Documentation, [r1wchdjn](https://partner.tiktokshop.com/docv2/page/r1wchdjn), hiện là changelog “EU Markets: Category Rules Update – Responsible Person Attribute Now Mandatory”. Dùng các trang API Reference cụ thể bên dưới để xác minh hợp đồng API.

## API đã đối chiếu

| API / API Reference | Method và path đang được tài liệu hiển thị | Required scope trên API Reference | Kết luận cho CatBack |
| --- | --- | --- | --- |
| [Get Creator Profile](https://partner.tiktokshop.com/docv2/page/get-creator-profile-202508) | `GET /affiliate_creator/202508/profiles` | `creator.affiliate.info` hoặc `creator.video.write` | Scope app được cung cấp khớp `creator.affiliate.info`; chỉ gọi sau khi Creator token thực sự cấp scope này. |
| [Creator Search Open Collaboration Product](https://partner.tiktokshop.com/docv2/page/creator-search-open-collaboration-product-202405) | `POST /affiliate_creator/202405/open_collaborations/products/search` | `creator.affiliate_collaboration.read` | HTML Manage scope xác nhận thuộc scope ID 1021508 và key khớp; cần Creator token cấp scope này. |
| [Search Creator Affiliate Orders](https://partner.tiktokshop.com/docv2/page/search-creator-affiliate-orders-202410) | `POST /affiliate_creator/202410/orders/search` | `creator.affiliate_collaboration.read` | HTML xác nhận thuộc scope ID 1021508; cần Creator token cấp scope này. Đây là API thay thế trace orders do TikTok chỉ định. |
| [Search Creator Target Collaborations](https://partner.tiktokshop.com/docv2/page/search-creator-target-collaborations-202405) | `POST /affiliate_creator/202405/target_collaborations/search` | `creator.affiliate_collaboration.read` | HTML xác nhận thuộc scope ID 1021508; cần Creator token cấp scope này. |
| [Get Open Collaboration Product List By Product Ids](https://partner.tiktokshop.com/docv2/page/get-open-collaboration-product-list-by-product-ids-202509) | `POST /affiliate_creator/202509/open_collaborations/products` | `creator.affiliate_collaboration.read` | HTML xác nhận thuộc scope ID 1021508; cần Creator token cấp scope này. |

Các version trong bảng là version hiển thị trên API Reference tại ngày đối chiếu. Không dùng quy tắc chọn số version lớn nhất: TikTok chỉ định `202410` thay thế trace orders `202505`.

### Scope collaboration đã xác minh từ Manage scope

HTML được gửi bổ sung xác nhận scope ID 1021508 dùng `creator.affiliate_collaboration.read`, khớp API Reference. Thông tin ban đầu dùng dấu chấm là sai khác đã được giải quyết; không dùng `creator.affiliate.collaboration.read` làm key hoặc alias trong code.

Danh sách 11 API trong nhóm Read Creator Affiliate Collaborations của bản chụp:

1. Creator Get Sample Request Deeplink.
2. Creator Search Open Collaboration Product.
3. Creator Search Sample Application Fulfillments.
4. Creator Select Affiliate Product.
5. Get Creator Applicable Sample Label.
6. Get Creator Sample Application Detail.
7. Get Open Collaboration Product List By Product Ids.
8. Get Toko Product Mappers.
9. Search Creator Affiliate Orders.
10. Search Creator Sample Applications.
11. Search Creator Target Collaborations.

Bốn API trong bảng phía trên đã đọc API Reference. Bảy API còn lại mới xác nhận mapping từ cấu hình app; phải đọc API Reference để xác minh method, path, version, parameter, trạng thái hỗ trợ và điều kiện thị trường trước khi triển khai. Nút API Doc của Creator Select Affiliate Product đang disabled trong HTML; không tự dựng endpoint. Get Toko Product Mappers được mô tả là chuyển ID sản phẩm Tokopedia sang TikTok Shop; việc xuất hiện trong scope không chứng minh phù hợp thị trường Vietnam.

Khi chạy thật, vẫn kiểm tra `user_type == 1` và `granted_scopes` chứa chính xác key yêu cầu. Scope Active của app chưa chứng minh creator đã cấp đủ quyền.

### Tracking link: đã xác nhận tên API, chưa xác nhận endpoint

Hai bản HTML Manage scope bổ sung xác nhận nhóm Manage Affiliate Tracking Links (ID 1873988, key `creator.affiliate.link.write`) có đúng **1 API: Generate Affiliate Sharing Link**. Mô tả trong Partner Center: API tạo affiliate sharing link có chain keys từ material ID, distributor customized tags, promotion channel và các tham số cần thiết khác. Đây chỉ là mô tả chức năng, chưa phải tên/kiểu/độ bắt buộc của request fields.

Trong danh mục Affiliate Creator đã đọc, hai API tạo sharing link có quyền khác với scope 1873988 được cung cấp:

| API Reference | Method / path | Required scope |
| --- | --- | --- |
| [Creator Generate General Link](https://partner.tiktokshop.com/docv2/page/creator-generate-general-link-202505) | `POST /affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch` | `creator.affiliate.share_link.read` |
| [Creator Generate Publisher Link](https://partner.tiktokshop.com/docv2/page/creator-generate-publisher-link-202504) | `POST /affiliate_creator/202504/affiliate_sharing_links/publisher/{publisher_id}/generate_batch` | `creator.affiliate.share_link.read` |

Nút API Doc của Generate Affiliate Sharing Link trong HTML là button không chứa URL tài liệu. Ảnh do chủ dự án gửi bổ sung xác nhận nút này đang disabled dù scope đang bật; không tiếp tục yêu cầu chủ dự án bấm nút đó. Chưa xác định được nguyên nhân disabled từ bằng chứng hiện có; không suy ra thiếu scope, cần đổi loại app hoặc API không hỗ trợ Vietnam.

Tìm kiếm trên Partner Center và web chính thức chưa trả về API Reference đúng tên này. Vì vậy chưa xác minh method, path, version, schema hoặc điều kiện thị trường. Cần tài liệu chính thức hoặc xác nhận từ TikTok Partner Support/Partner Manager về API Reference của Generate Affiliate Sharing Link cho scope 1873988 và market Vietnam. Không tự suy ra endpoint từ tên API, hai API sharing link trên hoặc ví dụ dành cho thị trường Indonesia. Không yêu cầu thêm scope trong bước này.

### Affiliate Information: danh sách 9 API trong Manage scope

Bản HTML mở nhóm Affiliate Information (ID 434372, key `creator.affiliate.info`) liệt kê:

| Tên hiển thị | Nhãn trong Manage scope | Kết luận triển khai |
| --- | --- | --- |
| Check Anchor Content | New | Changelog chính thức đã đánh dấu deprecated; loại khỏi triển khai. |
| Check Anchor Prerequisites | New | Changelog chính thức đã đánh dấu deprecated; loại khỏi triển khai. |
| Get Creator Profile | New | Đã đối chiếu API Reference `202508` và key scope. |
| Get Creator Profile (old) | New | Loại API old/deprecated; dùng Get Creator Profile `202508`. |
| Get Live Account Info | Legacy | Chưa xác minh Reference/trạng thái hỗ trợ; chưa chọn để triển khai. |
| Get Live Room Info | Legacy | Chưa xác minh Reference/trạng thái hỗ trợ; chưa chọn để triển khai. |
| Get Live Room Info | New | Có mapping scope, nhưng chưa xác minh Reference, endpoint/version/schema hoặc deprecation. |
| Get Shop Products(legacy) | New | Changelog đã đánh dấu deprecated; loại khỏi triển khai. Không thay bằng Get Shop Products `202509` vì scope khác. |
| get user online room's product infomation | Legacy | Chưa xác minh Reference/trạng thái hỗ trợ; chưa chọn để triển khai. |

Manage scope hiện liệt kê tổng cộng **21 mục API** trong ba scope Active (9 + 1 + 11). Đây là số mục được gán quyền, không phải 21 API hiện hành có thể triển khai cho Vietnam. Nhãn New không chứng minh API chưa deprecated: quyết định dùng API phải dựa trên API Reference và changelog hiện tại.

## API loại khỏi triển khai hiện tại

- [Creator Search Affiliate Trace Orders — deprecation](https://partner.tiktokshop.com/docv2/page/si5r1bv3): `POST /affiliate_creator/202505/orders/trace/search` đã retired ngày 15/08/2026. Dùng Search Creator Affiliate Orders `202410`; bộ lọc là `create_time_ge` / `create_time_lt`, không sao chép `time_ge` / `time_lt` / `time_type` của API cũ.
- [Affiliate API — Legacy Versions and Endpoints Sunset](https://partner.tiktokshop.com/docv2/page/1wqfoc2s): các endpoint legacy được liệt kê ngừng hoạt động ngày 12/10/2026 lúc 10:00 UTC+8 (09:00 giờ Việt Nam). Không triển khai API đã được đánh dấu deprecated, gồm `/affiliate/{version}/profiles`, Get Shop Products legacy, Check Anchor Content, Check Anchor Prerequisites và Generate Affiliate Product Promotion Link phía Seller trong danh sách này.
- [Get Showcase Products 202405](https://partner.tiktokshop.com/docv2/page/get-showcase-products-202405) yêu cầu `creator.showcase.read` hoặc `creator.video.write`; chưa có trong ba scope được cung cấp. Việc legacy Get Shop Products được thay bằng API này không có nghĩa scope cũ tự cấp quyền gọi API mới.
- [Get Shop Products 202509](https://partner.tiktokshop.com/docv2/page/get-shop-products-202509) yêu cầu `creator.video.write`; chưa có trong ba scope được cung cấp.
- Các chức năng Seller, quản lý collaboration, sửa showcase và Partner Campaign không được suy ra từ quyền đọc collaboration hoặc quyền tracking link của CatBack.

## Quy tắc triển khai Creator authorization

Theo Creator Authorization Guide:

1. Creator phải là TikTok Shop creator và selection region phải thuộc target market của app. Kiểm tra điều kiện rollout/allowlist nếu áp dụng; app On và scope Active chưa thay thế việc creator cấp quyền.
2. Link authorization: `https://shop.tiktok.com/alliance/creator/auth?app_key={app_key}&state={state}`. Tạo state ngẫu nhiên phía server, gắn với phiên authorization, xác minh nghiêm ngặt tại callback và chỉ dùng một lần.
3. Callback trả `code`; giá trị này được truyền dưới tên `auth_code` khi đổi token. Dừng nếu state không hợp lệ, code thiếu hoặc creator từ chối.
4. Guide chỉ định `GET https://auth.tiktok-shops.com/api/v2/token/get`, với `app_key`, `app_secret`, `auth_code`, `grant_type=authorized_code`. Khi viết client token, kiểm tra thêm API Reference Get Access Token được liên kết từ trang endpoint, gồm schema và thời hạn token.
5. Sau đổi token, kiểm tra `code == 0`, `user_type == 1` và `granted_scopes`. Lưu Creator token riêng với Seller token; kiểm tra quyền trước từng chức năng và sau refresh.
6. Guide chỉ định refresh qua `GET https://auth.tiktok-shops.com/api/v2/token/refresh` với `app_key`, `app_secret`, `refresh_token`, `grant_type=refresh_token`. Kiểm tra API Reference trước khi triển khai client refresh.
7. Các API Creator đã đối chiếu dùng header `x-tts-access-token` với Creator token; query chung gồm `app_key`, `sign`, `timestamp`. Kiểm tra tài liệu signing/common parameters và schema riêng trước khi tạo request; không tự thêm tham số Seller như `shop_cipher`.

API Search Creator Affiliate Orders `202410` đã kiểm tra phần request: query `page_size` bắt buộc trong khoảng 1–100, `page_token` tùy chọn; body có `create_time_ge` / `create_time_lt` dạng Unix timestamp tùy chọn. Các schema khác, nested fields và response phục vụ đối soát phải được đọc thêm trước khi viết DTO. Không giả định order ID đủ để gán đơn hàng cho từng người dùng CatBack; cần xác minh dữ liệu attribution thực tế từ API tracking link và đơn hàng.

## Hiện trạng codebase và bước tiếp theo

### Yêu cầu thử Creator Generate General Link

Chủ dự án đã yêu cầu thử trực tiếp Creator Generate General Link `202505` để xem TikTok có cho phép tạo link hay không, dù Required scope công khai là `creator.affiliate.share_link.read`. Đây là lần thử API được người dùng cho phép, không phải xác nhận hai scope link là tương đương hoặc cho phép bật API này trong production.

Đã đọc lại API Reference: request body tối thiểu có dạng `{"material":{"ids":["<product_id>"],"type":"PRODUCT"}}`; `ids` là mảng string bắt buộc, tối đa 50 ID, `type` bắt buộc và giá trị hiện hỗ trợ là `PRODUCT`. `campaign_id` là tùy chọn theo trường hợp sản phẩm campaign; `link_type` có thể bỏ để dùng TikTok Shop URL. Không tự điền product/campaign ID ví dụ vào request thật.

Chưa gửi request: chưa tìm thấy cấu hình TikTok hoặc biến môi trường Creator token/app secret trong phạm vi đã kiểm tra, và chủ dự án chưa chỉ định Product ID. Cần vị trí cấu hình cục bộ chứa app secret, Creator access token và Product ID để thực hiện thử có chữ ký hợp lệ. Chưa có kết luận thành công hay lỗi quyền từ TikTok.

- `src/WebHoanTien.Web/Controllers/TikTokShopOAuthController.cs`: callback hiện là placeholder, không đổi code/token; trang result trả HTTP 503. Chưa có Creator API client trong phần source đã rà soát.
- Các route, middleware và log filter OAuth đã tồn tại. Luồng thật cần tích hợp state validation, lưu token bảo mật, token refresh và scope checks theo các tài liệu trên.
- Khác biệt collaboration key và mapping tên API của cả ba scope đã giải quyết bằng HTML Manage scope. OAuth và các client đọc đã đối chiếu có thể được triển khai sau khi đọc schema/signing đầy đủ; chúng không phụ thuộc việc xác minh tracking link. Riêng chức năng tracking link cần API Reference của Generate Affiliate Sharing Link trước khi viết client hoặc bật chức năng.
- Không chạy test, Playwright hoặc kiểm thử trình duyệt tự động trong lần đối chiếu này. Trình duyệt chỉ được dùng để đọc tài liệu TikTok chính thức.
