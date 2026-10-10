# CatBack — Điều tra TikTok Shop Vietnam Sandbox, Creator OAuth và Affiliate APIs

> **Cập nhật sau điều tra:** Đã kiểm tra trực tiếp phiên đăng nhập CatBackTest VN theo yêu cầu của chủ dự án. Xem phụ lục cuối báo cáo. Các ghi chú “chưa truy cập console” trong Parts 1–8 mô tả lần điều tra ban đầu; phụ lục bổ sung bằng chứng UI, không phải kết quả gọi Creator API.

Ngày điều tra: **09/10/2026, Asia/Bangkok**. Repository: `E:\HungNT\WebHoanTien`; HEAD `8a455f975ce9aa2d5625a0c786392b1e14c091bf`, audit **working copy**, bao gồm thay đổi chưa commit.

Phạm vi: đọc tài liệu chính thức đã tải JavaScript, xem API Testing Tool công khai, audit source và lập kế hoạch. **Không sửa code/cấu hình, không gọi business/token API, không tạo dữ liệu, không gửi ticket hoặc App Review. Không chạy test, build hay kiểm thử trình duyệt.** Trình duyệt chỉ dùng để đọc tài liệu.

Quy ước bằng chứng:

- **Đã xác minh**: đọc được tài liệu chính thức hoặc source cụ thể. Luôn ghi rõ đó là hợp đồng API, hành vi code hay kết quả chạy thật.
- **Không hỗ trợ**: có quy định/giới hạn rõ; không dùng sự vắng mặt trong tài liệu để kết luận này.
- **Chưa xác minh**: cần API response hợp lệ, kiểm tra trong tài khoản của chủ dự án hoặc xác nhận TikTok.
- Các nguồn `[S1]`… là liên kết tài liệu chính thức; danh mục và cách đọc nằm cuối Part 8.

## Part 1 — Executive Summary

**Development Shop CatBackTest chưa có bằng chứng đáp ứng bất kỳ yêu cầu Creator nào trong bốn yêu cầu review. Tuy nhiên, chưa đủ bằng chứng để kết luận Vietnam Sandbox không hỗ trợ toàn bộ Affiliate.**

Có hai chặn độc lập: quyền Creator OAuth của app/thị trường/tài khoản và phần triển khai CatBack chưa hoàn chỉnh. Gỡ chặn OAuth không tự triển khai product/link/orders; triển khai thêm client cũng không tự mở quyền beta.

### Thông tin hồ sơ được cung cấp

| Thuộc tính | Giá trị | Nguồn và mức xác minh |
|---|---|---|
| App | CatBack; Custom; Creator collaborations; Vietnam | Chủ dự án cung cấp; chưa đối chiếu console đăng nhập |
| App Key | `6ldqvd4gsbe2m` | Chủ dự án cung cấp; không phải secret |
| Global service | `7690151491252094727`, LIVE | Chủ dự án cung cấp |
| Vietnam local service | `7690151491252111111`, BETA_TESTING | Chủ dự án cung cấp |
| Function review | REJECTED; yêu cầu OAuth/API thật và tiếng Việt | Nội dung reviewer do chủ dự án cung cấp |
| Scopes Active | `creator.affiliate.info`, `creator.affiliate.link.write`, `creator.affiliate_collaboration.read`, `creator.affiliate.share_link.read` | Chủ dự án cung cấp; chưa có token để đọc `granted_scopes` |
| Creator hiện có | Tạo affiliate link nhận hoa hồng được; authorize CatBack bị lỗi region | Chủ dự án cung cấp; chưa tái hiện bằng phiên Creator |
| Development shops | CatBack/Full Function cần onboarding; CatBackTest/Core Function có Seller/Customer View và menu Open Collaboration | Chủ dự án cung cấp; chưa chứng kiến thao tác tạo collaboration thành công |

### Kết luận quan trọng

| Kết luận | Trạng thái | Bằng chứng |
|---|---|---|
| Creator beta cần điều kiện rollout/allowlist của app; Creator testing account được yêu cầu qua App Store Manager | Đã xác minh **quy trình**, chưa xác minh CatBack được enable | [S1] |
| Affiliate Integration có hướng dẫn Development Shop/Open Collaboration và ví dụ response shop `SANDBOX…`, region US | Đã xác minh **ví dụ tài liệu**, không suy ra VN Core | [S3] |
| General Link `202505` yêu cầu `creator.affiliate.share_link.read`; danh mục tài liệu có Creator collaborations khi lọc Vietnam/Vietnam | Đã xác minh **Reference/category**, chưa gọi bằng CatBack | [S6] |
| OAuth, đổi/refresh token và Get Creator Profile thật đã có trong source | Đã xác minh **code**, chưa có bằng chứng chạy thành công | Part 4 |
| Product/link thật chưa triển khai; orders ở adapter Api là mảng rỗng cục bộ | Đã xác minh **code** | `TikTokAffiliateApiService.cs:16–29` |
| UI workflow chính vẫn có tiếng Anh; bật/tắt nhãn Demo không đổi nguồn dữ liệu | Đã xác minh **code/config cục bộ** | Part 4 |

### Lỗi “Not available in your region”

**Nguyên nhân chính xác: Chưa xác minh.** Thông báo này không đủ phân biệt beta enablement, target-market mapping, selection region của Creator hay phiên đăng nhập đang dùng. Global LIVE cũng chưa chứng minh Vietnam local service được mở Creator Authorization.

Hướng kiểm tra có giá trị nhất là yêu cầu TikTok xác nhận rollout/allowlist và mapping của **App Key + Vietnam Local Service**, rồi xác nhận Creator cụ thể đủ điều kiện. Đây là giả thuyết điều tra, không phải kết luận hệ thống TikTok đã lỗi.

Source đang tạo URL Creator với `app_key`, không dùng Seller `service_id`. State được tạo ngẫu nhiên và kiểm tra ở callback. Bởi vậy không có bằng chứng source đang dùng sai loại authorization URL hoặc thiếu state. Lỗi hiển thị trên trang TikTok xảy ra trước khi CatBack có thể đổi code lấy token.

## Part 2 — Sandbox Capability Matrix

Nền tảng Seller Sandbox và Creator beta là hai cơ chế thử nghiệm cần phân biệt. Matrix dưới đây mô tả khả năng được tài liệu xác nhận, không phải kết quả CatBack đã chạy. [S1], [S2], [S4]

| Khả năng | VN Core Function | VN Full Function | Creator Beta Testing Account | Production Creator |
|---|---|---|---|---|
| Seller product/order/fulfillment test | Đã xác minh | Đã xác minh, sau setup | Không phải Seller account | Không phải Seller account |
| KYC cơ bản | Không cần cho Core setup | Có yêu cầu thông tin hợp lệ/onboarding | Điều kiện cấp do TikTok xác nhận | Điều kiện Creator riêng |
| Tạo order không thanh toán qua Buyer View | Đã xác minh | Buyer checkout khác Core | Không được suy ra | Không được suy ra |
| Discovery bằng tài khoản TikTok thường | Không hỗ trợ | Không hỗ trợ; dùng linked test accounts | Kết nối Sandbox chưa xác minh | Không được suy ra truy cập Sandbox |
| Finance/settlement/customer service Seller | Không hỗ trợ/không phù hợp | Đã xác minh sau setup | Không phải Seller chức năng | Không phải Seller chức năng |
| Tự cấp Creator token từ Seller authorization | Không hỗ trợ thay thế identity | Không hỗ trợ thay thế identity | Cần Creator authorization | Cần Creator authorization |
| Tự tạo Creator Beta bằng Development Shops | Chưa xác minh | Official/Buyer Test Account không chứng minh Creator Beta | Qua Manager; provisioning chi tiết chưa xác minh | Không tự thành Beta |
| VN Open Collaboration hoạt động thực tế | Chưa xác minh | Chưa xác minh | Phụ thuộc dataset/token | Phụ thuộc app/token/market |
| Creator API đọc Sandbox product/campaign | Chưa xác minh | Chưa xác minh | Chưa xác minh cặp beta account + VN dataset | Chưa xác minh |
| Affiliate conversion có Creator attribution | Chưa xác minh | Chưa xác minh tại VN | Chưa xác minh fixture/quy trình | Chưa xác minh cho CatBack |

**Lưu ý tài khoản:** liên kết Official/Buyer Test TikTok Account trong Full Function có thể chuyển tài khoản thành test account vĩnh viễn. Không dùng Creator production hiện có để thử chuyển đổi này. Đây là quy định về tài khoản Sandbox, không phải bằng chứng có thể chuyển thành Creator Beta. [S4]

### Nhiệm vụ A — 10 câu trả lời trực tiếp

| # | Câu hỏi | Trả lời / trạng thái | Bằng chứng hoặc việc còn thiếu |
|---|---|---|---|
| A1 | Vietnam Core có Affiliate Open Collaboration? | **Chưa xác minh** | Menu do chủ dự án báo cáo; cần tạo/lưu thành công trong CatBackTest. Ví dụ Sandbox ở [S3] không ghi VN Core. |
| A2 | Tạo Open Collaboration Campaign? | **Chưa xác minh** tại CatBackTest | Cần phân biệt Open Collaboration với Affiliate Partner Campaign; không coi hai tên là một thực thể. |
| A3 | Thêm Sandbox product vào campaign? | **Chưa xác minh** | Cần xác định loại campaign, người sở hữu, thị trường và publication/eligibility của product. |
| A4 | Cấu hình commission rate? | Seller Open Collaboration API có trường này; **VN Core thực tế chưa xác minh** | [S16]; có thể kiểm tra UI trước, không yêu cầu thêm scope CatBack chỉ để sửa commission. |
| A5 | Có Campaign ID thật trong Sandbox? | **Chưa xác minh** | ID do Open Collaboration tạo không tự trở thành Partner Campaign ID. Lưu ID cùng loại thực thể. |
| A6 | Sandbox Campaign ID dùng với Creator API? | **Chưa xác minh** | Cần Creator token đúng dataset và response endpoint cụ thể. |
| A7 | Nối Creator Testing Account với Seller Development Shop? | **Chưa xác minh** cặp VN này | Official/Buyer account linking trong [S2] chưa chứng minh Creator Beta linking. |
| A8 | Tạo affiliate order/test conversion? | **Chưa xác minh** tại VN Core | Generic Buyer View order không chứng minh affiliate attribution; cần đối chiếu Creator Orders response. |
| A9 | Core/Full khác nhau thế nào cho Affiliate? | **Chưa xác minh** riêng Affiliate | Khác biệt Seller được mô tả ở matrix; không suy ra Full Function có Creator OAuth chỉ vì có buyer checkout. |
| A10 | Chung môi trường/token/dữ liệu? | Token Seller/Creator tách identity; dữ liệu Sandbox tách production. Routing cụ thể của Creator Beta: **Chưa xác minh** | [S4], [S16]; dùng chung tên miền Open API không chứng minh chung dataset. |

Đối với A2/A5/A6, API Seller Create Open Collaboration trả/thao tác với **Open Collaboration**. API General Link mô tả `campaign_id` lấy từ Affiliate Partner Campaign. Order schema cũng có các trường `campaign_id`, `open_collaboration_id`, `target_collaboration_id` riêng. Giữ ID cùng namespace; nếu cần chuyển/mapping, yêu cầu TikTok cung cấp quy tắc. [S6], [S7], [S16]

## Part 3 — API Compatibility Matrix

### Nhiệm vụ B — Creator account, beta và OAuth

| # | Câu hỏi | Trả lời / trạng thái |
|---|---|---|
| B1 | Development Shop tạo được Creator Testing Account? | **Chưa xác minh**. Tài liệu Seller account không đủ xác nhận cơ chế provisioning Creator. |
| B2 | Creator Testing Account được cấp bằng cách nào? | **Đã xác minh** điểm liên hệ App Store Manager. Cách cấp/allowlist cụ thể cho CatBack cần Manager xác nhận. [S1] |
| B3 | Chuyển/liên kết Creator VN hiện có vào Beta? | **Chưa xác minh**. Yêu cầu allowlist/enrollment cụ thể; không chuyển Creator thật thành Official/Buyer Sandbox để thử. |
| B4 | OAuth khi Vietnam service BETA_TESTING? | **Chưa xác minh cho CatBack**; trạng thái BETA không đủ chứng minh từng app/Creator được enable. |
| B5 | Enablement ngoài Active scopes? | **Đã xác minh** tài liệu có điều kiện rollout/allowlist của app. Active scopes là lớp quyền API, không tự chứng minh rollout. [S1], [S3] |
| B6 | Creator OAuth endpoint riêng Sandbox? | **Chưa xác minh**; không tìm thấy endpoint riêng trong nguồn đã đọc. Không tự dựng domain sandbox hoặc parameter môi trường. |
| B7 | Seller Test token có `user_type=1`? | **Không hỗ trợ** dùng token Seller thay Creator: Seller Reference chỉ định `user_type=0`, Creator Reference chỉ định `1`. [S6], [S16] |
| B8 | Test Creator API không cần Production Creator OAuth? | Có hướng **Creator testing account qua beta**; vẫn cần credential/authorization Creator hợp lệ. Không có bằng chứng bypass OAuth cho CatBack. |
| B9 | REJECTED được xin Creator Beta không? | **Chưa xác minh** policy eligibility. Có thể gửi yêu cầu Support; không khẳng định sẽ được cấp hoặc bắt buộc phải qua review trước. |
| B10 | Quy trình xin mở quyền? | Liên hệ Manager và quy trình Partner Assistant → Submit a Ticket, kèm hồ sơ Part 7. Quyền phê duyệt thuộc TikTok. [S1], [S13] |

Phân biệt identity: **Sandbox Seller** quản lý test shop; **Official/Buyer Test Account** phục vụ các luồng Full Function; **Creator Beta** phục vụ thử Creator theo rollout được TikTok cấp; **Production Creator** là tài khoản affiliate thông thường. Không thay tên loại tài khoản chỉ vì cùng đăng nhập bằng TikTok.

### Endpoint và khả năng tương thích

| API / chức năng | Method + path hiện hành | Scope/token | Sandbox và VN | Triển khai CatBack |
|---|---|---|---|---|
| Creator authorization | `https://shop.tiktok.com/alliance/creator/auth?app_key=…&state=…` | Creator authorize; không có token trước bước này | Beta enablement của CatBack chưa xác minh | Có redirect/state/callback thật [S1] |
| Exchange / refresh | `GET https://auth.tiktok-shops.com/api/v2/token/get` / `token/refresh` | `authorized_code` / `refresh_token`; kiểm tra Creator identity | Chưa có response hợp lệ của CatBack | Có client thật [S1], code Part 4 |
| Get Creator Profile | `GET /affiliate_creator/202508/profiles` | `creator.affiliate.info`; Creator `user_type=1` | Beta dataset chưa xác minh | Có client/profile adapter [S5] |
| Get Open Collaboration Product List By Product Ids | `POST /affiliate_creator/202509/open_collaborations/products` | `creator.affiliate_collaboration.read`; Creator | Product Sandbox VN chưa xác minh | Chưa triển khai [S9] |
| Creator Search Open Collaboration Product | `POST /affiliate_creator/202405/open_collaborations/products/search` | Mapping lịch sử: `creator.affiliate_collaboration.read`; Creator | Sandbox VN chưa xác minh | Chưa triển khai; đọc lại Reference trước viết DTO¹ |
| Creator Generate General Link | `POST /affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch` | `creator.affiliate.share_link.read`; Creator | Category VN được liệt kê; Sandbox chưa xác minh | Chưa triển khai [S6] |
| Search Creator Affiliate Orders | `POST /affiliate_creator/202410/orders/search` | `creator.affiliate_collaboration.read`; Creator | Attribution Sandbox VN chưa xác minh | Stub rỗng, chưa HTTP request [S7] |
| Generate Affiliate Sharing Link của scope ID 1873988 | **Chưa xác minh Reference/path/version/schema** | `creator.affiliate.link.write`, theo mapping được lưu trong repo | Chưa xác minh | Không tự dùng endpoint General Link thay thế² |
| Create Open Collaboration (Seller) | `POST /affiliate_seller/202412/open_collaborations` | `seller.affiliate_collaboration.write`; Seller `user_type=0`; `shop_cipher` | API có; CatBackTest thực tế chưa xác minh | Không có client; bốn Creator scopes không cấp Seller write [S16] |
| Affiliate Partner Campaign list / material inclusion | Chưa chốt Reference cho use case CatBack | Không suy ra từ Creator collaboration scope | Chưa xác minh | Dùng UI/Support cung cấp ID hợp lệ; không tự thêm scope |

¹ Bảng lịch sử trong [tiktok-shop-creator-api.md](E:/HungNT/WebHoanTien/docs/tiktok-shop-creator-api.md:31) có Reference cụ thể; lần điều tra này đọc trực tiếp Product by IDs `202509`. Không coi tìm thấy mapping lịch sử là đã audit đầy đủ schema của Search `202405`.

² Đã có `creator.affiliate.share_link.read` theo chủ dự án, nên kế hoạch General Link không phụ thuộc việc tìm ra Reference của API scope 1873988. Không yêu cầu scope mới chỉ để thay thế mapping chưa rõ.

### Nhiệm vụ C — General Link: 10 điểm đã kiểm tra

Đã mở và mở rộng schema Request/Response trên [S6].

| # | Điểm kiểm tra | Kết quả |
|---|---|---|
| C1 | Reference hiện hành | `creator-generate-general-link-202505`; không thấy nhãn deprecated trên Reference đã đọc |
| C2 | URL đầy đủ | `https://open-api.tiktokglobalshop.com/affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch`; host theo ví dụ signing [S8] |
| C3 | Scope | `creator.affiliate.share_link.read` |
| C4 | Token | Header `x-tts-access-token`, Creator `user_type=1` |
| C5 | `campaign_id` | String; không có nhãn Required; mô tả yêu cầu include khi product từ campaign |
| C6 | `material` | Required object; `ids`: Required `[]string`, tối đa 50; `type`: Required string, giá trị được nêu là `PRODUCT` |
| C7 | Phải ở campaign? | Phần mô tả yêu cầu material trong campaign, nhưng `campaign_id` có mô tả điều kiện; **chưa chốt phạm vi non-campaign** |
| C8 | Sandbox IDs | **Chưa xác minh**; không có đảm bảo VN Sandbox trên Reference |
| C9 | Vietnam | View app categories với registration Vietnam / market Vietnam có Creator collaborations; không chứng minh app-specific access |
| C10 | Request/response | Mẫu cấu trúc dưới đây; không phải response thực tế |

Field tùy chọn thêm: `link_type` mặc định rỗng; `TOKO` dành cho Tokopedia. `material.promotion_campaign_schema` có mô tả khi type `CAMPAIGN`, mâu thuẫn với mô tả hiện chỉ hỗ trợ `PRODUCT`: **không chọn CAMPAIGN khi chưa có xác nhận**. [S6]

Request minh họa theo schema, **không gửi trong lần điều tra**:

```http
POST https://open-api.tiktokglobalshop.com/affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch?app_key=6ldqvd4gsbe2m&timestamp={UNIX_SECONDS}&sign={SERVER_GENERATED_SIGN}
content-type: application/json
x-tts-access-token: {VALID_CREATOR_TOKEN}

{"material":{"ids":["{ELIGIBLE_PRODUCT_ID}"],"type":"PRODUCT"},"campaign_id":"{VALID_PARTNER_CAMPAIGN_ID}"}
```

Nếu TikTok xác nhận product không cần campaign cho trường hợp của CatBack, bỏ `campaign_id`; không dùng ID Open Collaboration hoặc ID ví dụ. Đây là lựa chọn cần xác minh, không phải bảo đảm request tối thiểu sẽ thành công.

Response minh họa; các placeholder là **giá trị chưa biết**, không phải dữ liệu TikTok trả về:

```json
{
  "code": 0,
  "message": "{PLATFORM_MESSAGE}",
  "request_id": "{PLATFORM_REQUEST_ID}",
  "data": {
    "sharing_links": [
      {
        "material_id": "{ELIGIBLE_PRODUCT_ID}",
        "sharing_link": "{TIKTOK_ISSUED_URL}",
        "deep_link": "{TIKTOK_ISSUED_DEEP_LINK}",
        "one_link": "{TIKTOK_ISSUED_ONE_LINK}"
      }
    ],
    "failed_materials": []
  }
}
```

Không giả định tất cả link fields luôn có giá trị. Với batch, phải đọc `failed_materials` theo từng material thay vì chỉ thấy `code=0` rồi xác nhận tất cả thành công. Schema của các phần tử lỗi phải đối chiếu thêm trước viết DTO. [S6]

### Nhiệm vụ D — Creator Affiliate Orders

Reference [S7] xác nhận scope/token/path ở matrix; query `page_size` Required, 1–100; `page_token` tùy chọn. Body dùng `create_time_ge` / `create_time_lt` Unix seconds. Response có `orders`, `next_page_token`, `total_count`; order chứa SKU, product/collaboration/campaign IDs và commission. Đây là dữ liệu gắn Creator identity, không thay bằng Seller orders.

| Vấn đề cần chứng minh | Trạng thái và điều kiện |
|---|---|
| API đọc orders thật | Đã xác minh hợp đồng; chưa có request CatBack thành công |
| Điều kiện có dữ liệu | Phải có order được attributed cho Creator/token, đúng thời gian và dataset; Seller order cùng product chưa đủ |
| Sandbox affiliate orders | Chưa xác minh tại VN Core; guide affiliate có luồng order chung nhưng không bảo đảm use case này |
| Test conversion | Chưa xác minh cách tạo; yêu cầu TikTok cung cấp fixture/quy trình đúng Creator + shop |
| Response rỗng có được reviewer chấp nhận? | **Chưa xác minh**. Không tìm thấy quy định miễn chứng minh conversion; cần reviewer/Support xác nhận bằng văn bản |
| Mảng rỗng hiện tại của CatBack | Không phải bằng chứng request thành công: adapter chưa gọi endpoint |

Creator Orders response có `tag`, nhưng chưa xác minh mapping giữa tag đó với từng loại link/click/publisher của CatBack. Không hứa đối soát hoàn tiền theo user chỉ bằng order ID.

**Deprecation đã kiểm tra lại:** Trace Orders `POST /affiliate_creator/202505/orders/trace/search` bị retired từ 15/08/2026; replacement là Orders `202410`, không chọn version chỉ vì số lớn hơn. [S10] Seller Create Open Collaboration `202405` nằm trong sunset 12/10/2026 10:00 UTC+8, tương đương 09:00 tại Việt Nam; dùng Reference `202412`. API Seller promotion link legacy không có replacement trong bảng sunset; không dùng làm lối tắt Creator link. [S11]

## Part 4 — Existing Code Audit

Các dòng bên dưới là vị trí trong working copy tại ngày audit. “Có code” không có nghĩa đã nhận Creator token hay response TikTok thành công.

| Module | File path | Hiện trạng | Vấn đề | Đề xuất |
|---|---|---|---|---|
| Chọn adapter | [ApplicationModule.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application/WebHoanTienApplicationModule.cs:43) | Mock/Api chọn theo cấu hình; mode sai làm startup lỗi | Default Mock; đổi mode chưa làm đủ workflow thật | Giữ mode rõ, xây capability từng chức năng |
| Cấu hình UI | [appsettings.json](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/appsettings.json:2) | Working copy hiện `Mode=Mock`, `ShowDemoLabel=false` | Ẩn nhãn không biến fixture thành API thật; không biết production nạp gì | Review dùng evidence API riêng; không lấy UI ẩn nhãn làm chứng minh |
| Authorization URL / token client | [TikTokCreatorOAuthClient.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Integrations/TikTokCreatorOAuthClient.cs:33) | URL Creator, exchange/refresh thật; kiểm tra HTTP/code, `user_type=1`, profile scope | Chưa có bằng chứng runtime; chỉ kiểm scope profile hiện tại | Sau enablement, lưu evidence đã redact; kiểm scope trước từng API mới |
| OAuth connect/callback | [TikTokShopOAuthController.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Controllers/TikTokShopOAuthController.cs:50) | POST antiforgery; HTTPS/callback authority; state 256 bit, TTL 15 phút, owner tenant/user/app, so cookie constant-time | Get/remove cache là hai thao tác, chưa bảo đảm atomic consume khi callback đồng thời | Xác minh replay/concurrency; consume atomic khi triển khai production |
| Callback completion | [TikTokShopOAuthController.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Controllers/TikTokShopOAuthController.cs:111) | Gọi `Complete(code)`, chỉ báo thật sau exchange/profile | Lỗi region phía TikTok có thể chưa tới callback | Tách telemetry các stage; không gửi code giả để thử |
| Creator token storage/refresh | [TikTokCreatorConnection.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Integrations/TikTokCreatorConnection.cs:26) | Token mã hóa Data Protection, cache theo tenant/user; refresh kiểm open_id; hạn lưu tối đa 8 giờ | Trial store; chưa thấy token entity dài hạn; chưa có khóa refresh đồng thời. Cache key chưa chứa app key dù purpose protector có | Thiết kế durable store tenant/user/app/Creator; rotation/concurrency và key lifecycle |
| Hồ sơ Creator / signing | [TikTokCreatorOAuthClient.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Integrations/TikTokCreatorOAuthClient.cs:66) | GET Profile `202508`, Creator header, kiểm region VN; HMAC cho path/query cố định | Signer chỉ phù hợp GET hiện tại, chưa tái sử dụng cho POST body/query động | Tách signer chuẩn ký đúng bytes được gửi theo [S8] |
| Live adapter | [TikTokAffiliateApiService.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application/TikTokAffiliate/TikTokAffiliateApiService.cs:16) | Profile nối client thật; `IsAvailable=false` | Product/link throw unavailable; links/orders `Array.Empty` không gọi TikTok | Implement client thật; biểu diễn unavailable khác empty-success |
| Fixture profile/product/orders | [TikTokAffiliateMockService.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application/TikTokAffiliate/TikTokAffiliateMockService.cs:14) | Hard-code `catback_demo`, Crocs product, ba orders; delay giả lập | Không phải dữ liệu lấy từ Creator/product/order API | Giữ cho demo nội bộ; review phải đi qua adapter thật |
| URL sharing giả lập | [TikTokAffiliateMockService.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application/TikTokAffiliate/TikTokAffiliateMockService.cs:90) | Tự nối URL product với `affiliate_id=demo` | Không TikTok-issued sharing link, không chứng minh attribution | Lưu nguyên link do API thật trả; không tự giả URL tracking |
| Demo state | [TikTokAffiliateDemoStore.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application/TikTokAffiliate/TikTokAffiliateDemoStore.cs:22) | Memory theo tenant/user, sliding 2 giờ/max 8 giờ | Không dùng làm token store/order source | Phân biệt state demo với dữ liệu integration |
| Trang demo consent | [TikTokCreatorDemo.cshtml.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Pages/TikTokCreatorDemo.cshtml.cs:35) | Đồng ý gọi Mock `ConnectCreator`; chỉ khả dụng IsDemo; không exchange code | Không kiểm chứng session/Creator thật; không đáp ứng yêu cầu OAuth thật | Không dùng bước consent cục bộ trong video chứng minh live |
| Dashboard orchestration | [TikTokAffiliate.cshtml.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Pages/TikTokAffiliate.cshtml.cs:35) | Card live và workflow service tách nhau; Mock vẫn dùng profile mock dù LiveCreator có; Api dùng LiveCreator đã tải | `IsAvailable=false` vẫn áp dụng toàn module dù đã có OAuth/profile thật | Tách capability/profile/products/links/orders để thể hiện đúng phần đã triển khai |
| Workflow frontend | [tiktok-affiliate.js](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/wwwroot/tiktok-affiliate.js:26) | Product/generate được mở chỉ khi `isDemo` | Dù backend được thêm API, frontend hiện vẫn khóa mode Api | Bật theo connection + required granted scope + capability |
| Tiếng Việt | [TikTokAffiliate.cshtml](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Pages/TikTokAffiliate.cshtml:60), [tiktok-affiliate.js](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/wwwroot/tiktok-affiliate.js:65) | Card live/consent có tiếng Việt; main tabs, lookup/link/order, errors/status còn tiếng Anh | Chưa đáp ứng yêu cầu reviewer về workflow chính VN | Localize đủ labels/placeholders/empty/loading/error/status/time/currency |
| Policy truy cập | [TikTokAffiliateAuthorizationHandler.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application/TikTokAffiliate/TikTokAffiliateAuthorizationHandler.cs:28) | User active + extra property `IsTiktokDemo`; không bypass admin | Live trial cũng bị policy gate | Cấp reviewer account hợp lệ qua admin hiện có; không mở anonymous |
| Bảo vệ log/HTTP | [TikTokShopOAuthLogFilter.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/Integrations/TikTokShopOAuthLogFilter.cs:9), [WebModule.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Web/WebHoanTienWebModule.cs:159) | Filter callback logs; HttpClient bỏ loggers, không auto redirect; chỉ log stage/status/code/request_id | Chưa audit được reverse proxy/APM/production logs | Redact end-to-end, giữ metadata request an toàn |
| Sandbox configuration | Source TikTok nêu ở các dòng trên | Không tìm thấy selector/dataset config Sandbox riêng trong module đã rà | Không biết cách TikTok route Creator Beta dataset | Chờ hướng dẫn chính thức; không tự đổi host hoặc thêm tham số |
| Contract/order model | [TikTokAffiliateContracts.cs](E:/HungNT/WebHoanTien/src/WebHoanTien.Application.Contracts/TikTokAffiliate/TikTokAffiliateContracts.cs:32) | DTO demo gọn: product/order total/commission/status | Chưa mô hình raw API SKU/partial errors/attribution/cursor | Tách transport DTO đã kiểm Reference và UI/domain DTO |

### Các câu hỏi audit đặc biệt

- **Hard-code Creator/product/order và URL giả:** có, trong Mock service; đường dẫn cụ thể ở bảng.
- **Fallback mock khi API lỗi:** không tìm thấy trong client/live adapter. Không được gọi stub orders rỗng là fallback lỗi; đó là chức năng chưa triển khai nhưng cần sửa cách biểu diễn.
- **Seller token dùng nhầm cho Creator:** không tìm thấy trong client hiện tại; token exchange từ chối `user_type != 1`, scope profile được kiểm. Chưa audit production token thực tế.
- **Thiếu kiểm tra state:** không; source có kiểm tra chặt. Điểm cần củng cố là consume một lần dưới concurrency, không phải thêm state từ đầu.
- **English trong luồng VN:** có; chính các mục product/link/order bị reviewer nêu vẫn chưa hoàn chỉnh tiếng Việt.
- **Demo nối với OAuth thật:** hiện có link chuyển thủ công giữa tab TikTok và consent cục bộ. Đăng nhập TikTok rồi đồng ý demo không tạo Creator token; cần video end-to-end official callback thật.

Tài liệu repo có nội dung lịch sử không còn đúng với code mới: `docs/tiktok-shop-creator-api.md` đoạn cuối còn mô tả callback placeholder/503, trong khi source hiện đã exchange/profile. Báo cáo này ưu tiên source hiện tại; chưa sửa các tài liệu cũ trong giai đoạn điều tra.

## Part 5 — App Review Resolution Plan

### Đánh giá ba phương án

| Tiêu chí | A — Creator Beta | B — Development Shop | C — Beta Creator + Sandbox Seller |
|---|---|---|---|
| Tiên quyết | TikTok enable app VN và cấp/allowlist Creator phù hợp | CatBackTest setup; UI/Seller permissions hợp lệ | A + dataset/product/campaign tương thích được TikTok xác nhận |
| Có thể chứng minh API nào | Creator profile/product/link/orders theo scope/data được cấp | Seller product/order/collaboration thực sự chạy; không thay identity Creator | Creator endpoints thật trên fixture được cấp nếu được hỗ trợ |
| API chưa được bảo đảm | Link/order nếu thiếu eligible material/conversion | Toàn bộ Creator workflow bằng riêng Seller account | Cross-dataset General Link và affiliate conversions |
| Giải quyết Creator OAuth? | Hướng phù hợp; kết quả CatBack chưa xác minh | Không có bằng chứng Development Shop tự mở Creator rollout | Vẫn cần Creator beta enablement |
| Link affiliate thật? | Có khả năng sau eligible material và API success; chưa chứng minh | Seller link không thay thế Creator link | Chưa xác minh tính tương thích |
| Creator orders? | Cần fixture/order attributed hoặc reviewer đồng ý empty-result | Test Seller order chưa đủ | Cần quy trình conversion do TikTok cấp |
| Đáp ứng review? | Ưu tiên cao nhất, cần hoàn thành implementation và evidence | Chỉ bổ trợ; chưa đủ bốn yêu cầu | Hữu ích nếu Support xác nhận; chưa dùng làm giả định nền |
| Support phải giúp | Enablement/account, material/campaign, order fixture, review evidence | VN Core affiliate capability và loại ID | Account/shop pairing, dataset routing, conversions, settlement/test semantics |

**Đề xuất:** theo A trước để xác định được một Creator token hợp lệ; điều tra B song song ở mức UI/read-only. Chọn C sau khi TikTok xác nhận cặp tài khoản và dataset, không mặc định chuyển sang Full Function hoặc thêm Seller scopes.

### Lộ trình với điểm dừng rõ

1. **Gate quyền truy cập:** Support/Manager xác nhận CatBack VN được Creator Authorization; nhận account/allowlist và product/campaign dùng được. Không đổi app category, service ID hay scope theo suy đoán.
2. **Gate OAuth:** official authorize → callback đúng state → token Creator → profile VN; bằng chứng là stage/request_id/response metadata thật đã redact. Thành công ở local consent không qua gate này.
3. **Gate data:** implement và gọi Creator product endpoint hợp lệ; hiển thị product đúng response. Không dùng Crocs fixture để lấp API trống/lỗi.
4. **Gate link:** thực thi General Link bằng token đúng scope và material đủ điều kiện; lưu TikTok-issued URL, batch success/failure và request_id.
5. **Gate orders:** gọi Creator Orders thật, pagination/thời gian chính xác; fixture có attribution hoặc xác nhận reviewer chấp nhận response rỗng thật. Không quay mảng stub rỗng làm API success.
6. **Gate VN UX:** Việt hóa toàn bộ đường đi từ kết nối tới sản phẩm, link, orders, loading/error/empty/reauthorize; status API được dịch ở UI nhưng giữ raw code/ID phục vụ audit.
7. **Gate review:** ghi video bằng tài khoản được TikTok cấp quyền, nối hành vi UI với request_id phía backend. Đối chiếu hai lý do rejection trước khi chủ dự án gửi lại.

Đây là kế hoạch do audit đề xuất, không phải bảo đảm TikTok phê duyệt. Quy định chấp nhận orders rỗng vẫn chưa xác minh. Tài liệu Test your App yêu cầu lưu evidence/request IDs; API Testing Tool platform key không chứng minh quyền của app CatBack. [S4], [S12]

## Part 6 — Step-by-Step Testing Guide

Các bước dưới đây dành cho **chủ dự án thực hiện sau khi có quyền hợp lệ**. Lần điều tra này chưa thực hiện các request hay thao tác tạo dữ liệu.

### 6.1 Lập hồ sơ và kiểm tra console

1. Đăng nhập Partner Center của CatBack; vào App & Service → app → Manage API.
2. Ghi nhận market/service status, bốn scope exact key, redirect callback và review feedback; che secrets/token/account identifiers không cần thiết.
3. Đối chiếu Local Service ID Vietnam với app đang mở; yêu cầu Support xác nhận mapping rollout, không thử lấy Global Service ID ghép vào Creator URL.
4. Gửi yêu cầu Part 7 qua Manager/Partner Assistant. Không xin thêm Creator scope nếu bốn scope hiện tại đã đủ endpoint cần dùng.

### 6.2 Xác minh manh mối Open Collaboration trên CatBackTest

1. Development Kits → Development Shops/Sandbox → Seller test accounts → chọn **CatBackTest/Core Function**, kiểm tra market VN.
2. Seller View → sản phẩm: xác nhận có product **Active**; ghi Product ID và shop/account type.
3. Marketing → Liên kết → Open Collaboration: xem trang thật có tải được hay chỉ dẫn sang trang ngoài/không khả dụng.
4. Nếu UI cho phép và chủ dự án quyết định tạo dữ liệu test: thêm product, cấu hình commission rồi lưu; ghi kết quả, error text, ID **và loại ID**. Thành công UI là evidence UI; chưa phải API Creator success.
5. Nếu màn hình là Partner Campaign, ghi rõ loại, owner, campaign publication status và Product ID membership. Nếu chỉ là Open Collaboration, không điền ID đó vào General Link `campaign_id`.
6. Nếu API Testing Tool có Seller read endpoint với quyền được cấp sẵn, chủ dự án có thể đối chiếu dữ liệu UI. Bốn Creator scopes hiện tại không cấp quyền gọi Seller Create Open Collaboration; ưu tiên UI thay vì mở rộng scope không cần thiết.

Quy tắc Seller product Active thuộc [S2]; các bước affiliate cụ thể tại VN ở trên là **thủ tục xác minh đề xuất**, chưa chứng minh menu hiện tại hoạt động.

### 6.3 Test Creator OAuth thật

1. Nhận Creator account/enrollment cụ thể từ TikTok; xác nhận selection region VN và app/Creator beta eligibility.
2. Chủ dự án cấu hình secrets ở môi trường trial hợp lệ; callback HTTPS khớp domain bắt đầu OAuth và cấu hình Partner Center.
3. Đăng nhập CatBack bằng account có `IsTiktokDemo=true`, dùng **Kết nối TikTok Creator thật**, không bắt đầu từ URL có state cố định.
4. Đăng nhập và cấp quyền trên TikTok chính thức. Nếu region error, dừng, lưu screenshot đã che thông tin nhạy cảm và thời điểm/múi giờ; gửi Support app/service/account eligibility context. Không có callback thì không cần gọi token API.
5. Khi callback thật có code: để server kiểm state và exchange. Chỉ coi gate thành công sau profile thật trả VN; không sao chép code/token vào báo cáo/video.
6. Ghi `user_type`, danh sách scope names, response code/request_id/stage ở dạng metadata; không ghi access/refresh token, secret hoặc URL callback có code.

### 6.4 Test API trong Partner Center

Entry: Development Kits → API Testing Tool, chọn **own app key CatBack**, version đúng Reference. Token được nhập tại công cụ chính thức bởi chủ dự án, không đưa vào chat. Nguồn phương thức kiểm tra: [S12].

| Thứ tự | Request đề xuất | Điều kiện / evidence cần lưu |
|---|---|---|
| 1 | Get Creator Profile `202508` | Creator token; profile scope; code/request_id và region đúng |
| 2 | Creator Product by IDs `202509` | Product ID TikTok xác nhận đủ điều kiện; scope collaboration. Reference đang hiển thị `product_ids` ở Query: kiểm generated cURL/serialization, không tự chuyển sang body. [S9] |
| 3 | Creator General Link `202505` | Đây là API tạo link; chỉ chạy khi chủ dự án cho phép và material/campaign đã xác minh. Kiểm cả success và failed materials |
| 4 | Creator Orders `202410` | Query page size hợp lệ, thời gian test nằm trong filter; lưu cursor/count và attribution IDs đã redact |

Tách hồ sơ evidence: `API/endpoint/version`, app key, môi trường/account type, thời điểm, scope names, request_id, platform response code, ID thực thể dùng trong thử nghiệm và kết quả UI. Không copy generated cURL nguyên văn nếu chứa token hoặc sign/credential nhạy cảm.

### 6.5 Test conversion và quay review

1. Yêu cầu TikTok xác định buyer/Creator/shop/campaign cùng môi trường và cách tạo conversion được hỗ trợ.
2. Nếu chỉ có Core Buyer View tạo generic order, dùng nó để kiểm Seller workflow; không tự kết luận order có Creator commission.
3. Nếu cần Full Function checkout, để chủ dự án xử lý onboarding/KYC và thanh toán theo hướng dẫn được TikTok xác nhận; không chuyển account production hiện tại. Không dùng VPN hoặc thay region để vượt lỗi authorization.
4. Sau conversion hợp lệ, đối chiếu Creator Orders với order/product/collaboration/campaign, thời gian và commission thực tế; xác minh latency với Support.
5. Nếu không có fixture, hỏi reviewer có chấp nhận request Orders thành công nhưng rỗng hay không. Ghi câu trả lời trước khi chọn video chỉ có empty state.
6. Quay UI tiếng Việt, official OAuth và bốn chức năng thật; metadata backend đủ nối evidence, che credentials.

### Giới hạn truy cập trong lần điều tra

Web reader chỉ nhận HTML shell trên nhiều trang React; đã dùng trình duyệt đọc nội dung rendered và mở rộng schema. API Testing Tool công khai tải được form, nhưng có nút **Log in** và không có session/app/token CatBack đã chọn. Không gửi Submit Request. Vì vậy trạng thái app/scopes/shop/Creator runtime đều cần kiểm tra thủ công trong tài khoản chủ dự án.

## Part 7 — TikTok Support Requirements

### Những điểm CatBack không thể tự xác nhận/kích hoạt bằng sửa code

| Yêu cầu | Câu trả lời cần TikTok cung cấp | Evidence nên gửi |
|---|---|---|
| Creator Authorization VN | App Key đã allowlist beta chưa? Vietnam Local Service nào được enable? REJECTED có ảnh hưởng eligibility không? | App/type/category/service IDs/status và region-error screenshot |
| Creator account | Cấp VN Creator testing account hay allowlist Creator hiện tại? Điều kiện và quy trình cụ thể? | Region/affiliate eligibility; cung cấp account reference qua kênh Support khi cần |
| Sandbox pairing | CatBackTest Core hỗ trợ Affiliate UI/API nào? Creator Beta đọc được dataset này không? | Shop type/market; UI thực tế, Product/Open Collaboration IDs |
| General Link | Non-campaign product được hỗ trợ không? Campaign ID loại nào? `CAMPAIGN` có thật được hỗ trợ? | Reference `202505`, phần schema/mô tả chưa nhất quán |
| Material/campaign | Cung cấp product/campaign fixture hợp lệ cho VN và token Creator được cấp | Chỉ IDs và loại môi trường; không gửi token |
| Order evidence | Có fixture/test conversion? Request rỗng thật có đáp ứng review không? | Reviewer feedback, API/version, response code/request_id nếu có |
| Scope 1873988 | Reference endpoint/schema/version/market của Generate Affiliate Sharing Link | Exact scope key/name/ID; không tự gọi General Link dưới scope này |

**Quy trình hiện hành đã xác minh:** Help → Ask Partner Assistant hoặc floating assistant; mô tả vấn đề; nếu chưa giải quyết, chọn Submit a Ticket, điền category/title/details và attachment đã redact. Sau đó trao đổi trực tiếp App Store/Partner Manager cho việc Creator testing account/authorization listing. Chưa gửi bất cứ nội dung nào. [S13], [S3]

### Nội dung ticket đề xuất — chưa gửi

```text
Subject: CatBack Vietnam Creator beta authorization blocked; request testing account and confirmed Affiliate review path

App: CatBack
App key: 6ldqvd4gsbe2m
Type/category: Custom / Creator collaborations
Target market: Vietnam
Global service ID: 7690151491252094727 (reported LIVE)
Vietnam local service ID: 7690151491252111111 (reported BETA_TESTING)
Function review: REJECTED

Creator Authorization displays “Not available in your region”. Our Vietnam
Creator can already promote other shops' products as an affiliate but cannot
authorize CatBack. Please verify the app's VN Creator rollout/allowlist,
local-service mapping and the Creator's eligibility.

Active scopes reported in Partner Center:
creator.affiliate.info
creator.affiliate.link.write
creator.affiliate_collaboration.read
creator.affiliate.share_link.read

Please confirm:
1. Can you enable VN Creator beta authorization and provide a Creator testing
   account, or enroll/allowlist our current Creator while review is rejected?
2. Does our VN Core Function development shop CatBackTest support Open
   Collaboration, commission settings and Creator-visible test products?
3. Can the provided Beta Creator access that sandbox dataset? Please provide
   the supported account/shop pairing and any required environment settings.
4. For Creator Generate General Link 202505, which campaign type/ID and
   material membership are required? Are non-campaign products supported?
   The overview requests campaign membership; campaign_id is conditional,
   and promotion_campaign_schema mentions CAMPAIGN while type says PRODUCT.
5. Please provide an eligible VN product/campaign fixture and an approved
   affiliate conversion/order testing procedure for Creator Orders 202410.
6. If no order fixture is available, will successful live Orders requests
   returning an empty list satisfy this review? Please confirm in writing.
7. Please provide the current API Reference for Generate Affiliate Sharing
   Link mapped to scope ID 1873988 / creator.affiliate.link.write.

Reviewer requires real Creator OAuth, API data, link generation, orders and
Vietnamese UI. We will not represent mock data or Seller tokens as Creator
API evidence. Attachments will contain screenshots and request metadata
only; no app secret, authorization code, access token or refresh token.
```

## Part 8 — Implementation Plan

Đây là kế hoạch để xem xét sau báo cáo, **chưa triển khai**. P0 = chặn bằng chứng review; P1 = độ đúng/bảo mật và vận hành; P2 = hoàn thiện.

| Ưu tiên | Thay đổi đề xuất | Hoàn thành khi | Cách kiểm thử sau khi được yêu cầu |
|---|---|---|---|
| P0 | Xin enablement/account/fixture qua Support | Có thông tin xác nhận app VN + Creator + material/order path | Thực hiện gate OAuth thật |
| P0 | Việt hóa toàn workflow | Creator/product/link/orders và error/loading/empty/status đều tiếng Việt | Checklist thủ công desktop/mobile; kiểm locale/currency/time |
| P0 | General signing client cho query/path/body | Chữ ký sử dụng bytes thực gửi; header token đúng | Vector signing + so request với official tool; không log secret [S8] |
| P0 | Creator product client | Reference/schema hiện hành; scope check; kết quả đúng ID | Read API thật với fixture đủ quyền; empty/error không thành fixture |
| P0 | General Link client | Scope đúng; material/campaign đủ điều kiện; lưu nguyên response link | Test batch partial failure và request thành công thật dưới quyền được cấp |
| P0 | Creator Orders client | HTTP request thật; cursors/filter/order-SKU/commission đúng | Fixture conversion hoặc empty-state plan được reviewer xác nhận |
| P0 | Capability-based frontend | Api mode mở đúng chức năng đã triển khai và granted scopes | Creator đủ/thiếu scope; unavailable khác empty success |
| P1 | Token store và refresh concurrency | Durable encrypted store theo tenant/user/app/Creator; rotation/expiry | Refresh đồng thời, restart/multi-instance, revoke/invalid token |
| P1 | Atomic OAuth state consumption | Mỗi authorization session được consume một lần | State sai/thiếu/expired, duplicate callback, concurrency, cross-user/tenant |
| P1 | Error/metadata semantics | UI giữ nguyên trạng thái lỗi API; log có stage/code/request_id an toàn | Token expired/scope denied/timeout/malformed JSON/partial errors |
| P1 | Link attribution/domain mapping | Có hướng dẫn TikTok về field/click/publisher/order correlation | Conversion đúng link, tránh gán order của Creator cho sai user CatBack |
| P2 | Lịch sử link live | Lưu link và kết quả tạo theo đúng owner; không trộn fixture demo | Kiểm tra isolation và dữ liệu tồn tại sau khi khởi động lại |
| P2 | Cập nhật docs và reviewer guide | Tài liệu phản ánh source mới, rõ evidence live và giới hạn | Rà soát hồ sơ trước submit |

**Thứ tự triển khai đề xuất:** Support enablement → xác minh OAuth/profile → product → General Link → orders → video/evidence. Việt hóa và thiết kế signer có thể chuẩn bị trong lúc chờ quyền, sau khi chủ dự án yêu cầu sửa code. Không chặn các API đọc đã xác minh chỉ vì scope 1873988 chưa có mapping endpoint.

### Danh mục nguồn và cách thu thập

Tất cả nguồn chính thức dưới đây được đối chiếu ngày 09/10/2026. “Rendered” nghĩa là đã đọc nội dung trang sau tải JavaScript, không chỉ dựa trên snippet/search index.

| Nguồn | Nội dung dùng | Đã đọc / giới hạn |
|---|---|---|
| [S1] Creator authorization guide | Beta prerequisites, testing account, Creator OAuth | Rendered; chưa truy cập account Creator |
| [S2] Seller Center development shops | Core/Full, VN market, Seller products/order và account types | Rendered; chưa test shop của chủ dự án |
| [S3] Affiliate integration | Partner listing, Sandbox US example, Open Collaboration/affiliate workflow | Rendered + search; guide có ví dụ legacy, không dùng làm schema hiện hành |
| [S4] Test your App | Seller testing, data isolation, evidence, conversion account limitations | Rendered; không tự áp Seller testing rule để phủ định Creator Beta |
| [S5] Get Creator Profile 202508 | Method/path/scope/Creator token | Rendered API Reference |
| [S6] Creator Generate General Link 202505 | Request/response và scope, market-category selector | Rendered; mở rộng material/data/sharing_links; lọc **registration VN/target VN**, không xác nhận registration thực của CatBack |
| [S7] Search Creator Affiliate Orders 202410 | Request và response orders/SKUs | Rendered; mở rộng nested fields; chưa gọi API |
| [S8] Sign your API request | HMAC/query/path/exact body bytes/Open API host | Rendered; không chạy sample code |
| [S9] Product List By Product Ids 202509 | Path/scope/token và query product_ids | Rendered; serialization cần so generated cURL |
| [S10] Trace Orders deprecation | Retired 15/08/2026, replacement `202410` | Rendered changelog |
| [S11] Affiliate legacy sunset | 12/10/2026 UTC+8, Seller collaboration version, legacy promotion link | Rendered changelog |
| [S12] API Testing Tool guide | Platform/own key distinction, authorization và testing steps | Rendered; public tool chỉ có form/Log in, không session CatBack |
| [S13] Submit ticket through Partner Assistant | Quy trình ticket hiện hành | Rendered; chưa gửi ticket |
| [S14] Common parameters | Header/query/timestamp; identifiers theo từng endpoint | Rendered; Creator identity ưu tiên Reference riêng |
| [S16] Create Open Collaboration 202412 | Seller scope/token/product/commission/shop_cipher | Rendered; tới Reference qua link changelog |

Không sử dụng ví dụ old Profile `202405`, cURL có Seller path trong phần Creator của guide hoặc thông tin tax minh họa cũ để viết client hiện hành. Không dùng credential mẫu công khai trong tài liệu để authorize CatBack. Partner Campaign/API scope 1873988 vẫn cần Reference/xác nhận riêng.

[S1]: https://partner.tiktokshop.com/docv2/page/creator-authorization-guide
[S2]: https://partner.tiktokshop.com/docv2/page/seller-center-development-shops
[S3]: https://partner.tiktokshop.com/docv2/page/affiliate-integration
[S4]: https://partner.tiktokshop.com/docv2/page/test-your-app
[S5]: https://partner.tiktokshop.com/docv2/page/get-creator-profile-202508
[S6]: https://partner.tiktokshop.com/docv2/page/creator-generate-general-link-202505
[S7]: https://partner.tiktokshop.com/docv2/page/search-creator-affiliate-orders-202410
[S8]: https://partner.tiktokshop.com/docv2/page/sign-your-api-request
[S9]: https://partner.tiktokshop.com/docv2/page/get-open-collaboration-product-list-by-product-ids-202509
[S10]: https://partner.tiktokshop.com/docv2/page/si5r1bv3
[S11]: https://partner.tiktokshop.com/docv2/page/1wqfoc2s
[S12]: https://partner.tiktokshop.com/docv2/page/api-testing-tool
[S13]: https://partner.tiktokshop.com/docv2/page/kwfskwpj
[S14]: https://partner.tiktokshop.com/docv2/page/common-parameters
[S16]: https://partner.tiktokshop.com/docv2/page/create-open-collaboration-202412

### Trả lời câu hỏi quyết định

Với **Vietnam Core Function Development Shop CatBackTest hiện có**, theo bằng chứng được cung cấp và thu thập trong lần điều tra này:

| Yêu cầu Reviewer | Kết luận | Lý do |
|---|---|---|
| 1. Real Creator OAuth | **UNKNOWN** | Chưa có Creator callback/token thật của CatBack; shop Seller không chứng minh app/Creator beta được enable |
| 2. Real Creator/Product API Data Access | **UNKNOWN** | Có code profile nhưng chưa response thành công; product Creator client chưa triển khai, Seller product không thay thế |
| 3. Real Creator Affiliate Link Generation | **UNKNOWN** | Endpoint/scope có Reference; chưa response TikTok-issued link với Product/Campaign Sandbox VN hợp lệ |
| 4. Real Creator Affiliate Order Functions | **UNKNOWN** | Chưa Creator Orders request/conversion thật; Core test Seller order chưa chứng minh attribution |

**Số phần có bằng chứng hoạt động: 0/4 YES; 4/4 UNKNOWN.** Đây là số lượng evidence đạt được, không phải kết luận “TikTok không hỗ trợ 4/4”. Những điều đã có bằng chứng **NO** là dùng Seller token thay Creator token và dùng Core cho một số buyer/finance flows; không mở rộng các NO đó thành lệnh cấm toàn bộ Creator Sandbox.

**Bước tiếp theo có giá trị cao nhất:** gửi yêu cầu Part 7 tới App Store/Partner Manager hoặc Partner Assistant để xác nhận và mở **Creator Authorization cho CatBack tại Vietnam**, nhận/allowlist Creator testing account và material/order fixture tương thích. Sau đó kiểm chứng luồng OAuth/profile thật đang có trước khi đầu tư vào pairing Sandbox chưa được xác nhận. Việt hóa workflow chính là phần chuẩn bị độc lập có thể triển khai khi chủ dự án yêu cầu tiếp tục sửa code.

## Phụ lục — Kiểm tra trực tiếp tài khoản CatBackTest VN

Chủ dự án đã đăng nhập và yêu cầu kiểm tra chức năng thực tế. Chỉ điều hướng/xem màn hình chính thức; không tạo sản phẩm, link, cộng tác, đơn hàng, gửi lời mời hoặc chấp thuận quyền OAuth. Không thực hiện request thủ công bằng API Testing Tool và không chạy test source/Playwright.

| Chức năng | Bằng chứng trực tiếp | Kết luận giới hạn |
|---|---|---|
| Account/environment | Manage accounts: CatBackTest, Core function, Local VN, Active; Seller/Shop ID `7494962079678367513`; tên shop `SANDBOX_VN7694054806386689844` | Đã xác nhận đúng Seller Development Shop VN |
| Authorize app | Manage accounts hiển thị nguyên văn “Authorize an app with this test Seller Center account”; có Manage Authorization và nút Authorize app | Đây là **Seller authorization**, không chứng minh cấp Creator token; không bấm xác nhận cấp quyền |
| Creator Authorization CatBack | Mở URL Creator chính thức với app key CatBack trong cùng môi trường trình duyệt; trang trả “Not available in your region / This app or service is not available in your region.” | Đã tái hiện chặn tại trang authorization. Chưa đăng nhập/xác minh một Creator riêng; chưa xác định nguyên nhân region/rollout/account |
| Seller products | Quản lý sản phẩm: Trên kệ 0, Đang xem xét 0, Cần chú ý 0; “Chưa có sản phẩm” | Chưa có product trên kệ để kiểm chứng tạo link; không kết luận không có draft/archived products |
| Open Collaboration | Trang thật tải được cho Shop ID trên, Added products 0 / Not added 0, Add product và Bulk edit | **Có UI Affiliate/Cộng tác mở tại VN Core**, mạnh hơn bằng chứng chỉ thấy menu. Chưa chứng minh lưu thành công hoặc Creator nhìn thấy material |
| Thiết lập commission | Bulk edit tải được; hướng dẫn chỉnh tỷ lệ hoa hồng qua template. Target invitation/create tải được, có chọn sản phẩm/Creator và preview tỷ lệ hoa hồng | Có UI cấu hình commission phía Seller; chưa lưu hay kiểm chứng commission thực tế. Giá trị 0,00% trong preview trống không phải commission của sản phẩm thật |
| Affiliate orders/commission | Đơn hàng liên kết tải được; có Creator, loại nội dung, tỷ lệ hoa hồng tiêu chuẩn/Shop Ads, cơ sở hoa hồng ước tính/thực tế, thanh toán hoa hồng ước tính/thực tế, thời gian thanh toán | **Có màn hình đọc commission phía Seller**; danh sách trống với khoảng thời gian mặc định đang hiển thị, chưa có dòng dữ liệu hay Creator API response |
| Creator management | Trang tải được, có quản lý/mời/gắn thẻ Creator và cột hoa hồng ước tính | Quản lý cộng tác phía Seller, không phải Creator OAuth |
| Tạo link affiliate Creator | Các màn hình đã xem là Seller collaboration/invitation; không thấy chức năng tạo Creator General Link. Không có product Affiliate để thử | **Chưa xác minh**, không kết luận toàn bộ Sandbox không hỗ trợ. Invitation/preview không được coi là link bán hàng attributed cho Creator |

Các trang đã kiểm tra trực tiếp:

- [Manage accounts của VN Development Shops](https://partner.tiktokshop.com/v2_sandbox/config?activeTab=manage_account&baseRegion=VN&region=VN).
- [Seller products](https://seller-vn.tiktok.com/product/manage?shop_region=VN).
- [Open Collaboration của CatBackTest](https://affiliate.tiktok.com/affiliate/collaboration/open-collaboration?shop_region=VN&shop_id=7494962079678367513).
- [Bulk edit commission](https://affiliate.tiktok.com/affiliate/collaboration/open-collaboration/bulk-edit?shop_region=VN&shop_id=7494962079678367513).
- [Affiliate orders](https://affiliate.tiktok.com/product/order?shop_region=VN&shop_id=7494962079678367513).
- [Creator management](https://affiliate.tiktok.com/affiliate/assets/creator-management?shop_region=VN&shop_id=7494962079678367513).
- [Target invitation form](https://affiliate.tiktok.com/affiliate/collaboration/target-invitation/create?enter_from=target_invitation_list&shop_region=VN&shop_id=7494962079678367513).
- [Creator Authorization của CatBack](https://shop.tiktok.com/alliance/creator/auth?app_key=6ldqvd4gsbe2m&state=catback_test_123456). State cố định chỉ dùng để xem trang trong kiểm tra này; không dùng làm phiên OAuth hợp lệ của CatBack.

**Kết luận cập nhật:** CatBackTest VN Core có Affiliate Center thực tế và các màn hình commission/orders phía Seller. Nút Authorize app được mô tả rõ là dùng Seller account. Creator Authorization của CatBack vẫn bị region error, và tạo link/đọc commission qua **Creator API** vẫn chưa có bằng chứng. Bước tiếp theo để kiểm chứng end-to-end cần product test hợp lệ và Creator được TikTok cho phép authorize; chỉ thêm Seller product sẽ không tự giải quyết chặn Creator.
