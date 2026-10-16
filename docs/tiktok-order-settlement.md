# Đối soát và ghi nhận hoàn tiền đơn TikTok

Triển khai source ngày 15/10/2026, theo quyết định của chủ dự án:

- Admin được duyệt thủ công ở **mọi trạng thái** TikTok; không bắt buộc sàn đã trả `SETTLED` hoặc xác nhận CatBack đã nhận tiền.
- Dùng `actual_commission` của TikTok: không trừ thuế lần nữa, thuế bổ sung **0%**.
- Nhập hoa hồng **trước thuế**: mặc định thuế **10%**, admin được sửa 0–100%.
- Không thêm phí dịch vụ mặc định cho TikTok. Không thay đổi đối soát hoặc tính tiền Shopee.

## Thao tác

1. Admin có quyền `Affiliate.Admin.Orders` vào `/Admin/TikTokOrders`. Dữ liệu được job Creator API đồng bộ mỗi giờ (mặc định đọc lại 90 ngày); có thể lấy lại qua nút đồng bộ nếu có quyền Sync. Cả hai dùng chung service/khóa và không tự duyệt/cộng ví. Xem [cấu hình job](tiktok-shop-creator-api.md#tự-động-đồng-bộ-đơn-hàng-mỗi-giờ--16102026).
2. Bấm **Đối soát** ở đơn cần duyệt, mở `/Admin/TikTokSettlements/{orderId}`. Trang hiển thị Creator, trạng thái TikTok, thời điểm đồng bộ và các khoản dự kiến/thực nhận từ database.
3. Chọn nguồn số tiền. Chỉ dùng actual_commission khi có snapshot SETTLED hợp lệ và đủ số tiền VND theo SKU. Với trạng thái khác hoặc muốn duyệt số tiền khác, chọn **Admin nhập hoa hồng trước thuế**, nhập số tiền và tỷ lệ thuế. Không tự điền khoản net/estimated từ API thành số tiền trước thuế.
4. Nhập lý do duyệt 10–1.000 ký tự, bấm **Xem trước số tiền cộng ví**. Kiểm tra hoa hồng, thuế, số tiền sau thuế, từng người nhận và các cảnh báo.
5. Đánh dấu xác nhận, bấm **Duyệt đối soát và cộng ví**. Đơn chuyển `AffiliateOrderStatus.Settled`, chốt số tiền trên từng attribution; ví đọc trực tiếp sổ đơn đã chốt. Thông báo ghi nhận hoàn tiền được lưu cùng transaction.
6. Duyệt lại đơn đã chốt chỉ trả thông báo đã duyệt, không ghi nhận khoản thứ hai. Có thể mở **Xem đối soát** để xem tiền, thời gian, admin và lý do đã chốt.

Ví dụ: admin nhập trước thuế 100.000đ, thuế 10% → còn 90.000đ. Nếu tỷ lệ đã lưu của người nhận là 80%, cộng ví 72.000đ. Với actual_commission 90.000đ, không trừ thêm 10%, tiền người nhận vẫn tính trên 90.000đ.

## Kiểm tra và phân bổ

- Luồng chỉ xử lý `AffiliatePlatform.TikTok`, đối chiếu Creator của đơn với Creator hoàn tiền `IsPrimary` do admin đăng ký. Không chọn Creator theo user hiện tại, không dùng token/bảng Developer. Registry chỉ đọc ID đã đăng ký, không yêu cầu token đang hoạt động và không gọi TikTok khi xem trước/duyệt.
- Không đoán người nhận theo tên/sản phẩm/thời gian mua. Phải có attribution Matched với user, tracking và đúng Creator/source TikTok của CatBack. User đã xóa, attribution conflict, dữ liệu thiếu hoặc đã chốt dở dang sẽ chặn duyệt.
- Có thể duyệt khi một phần attribution chưa ghép, giống luồng Shopee: phần chưa ghép được giữ lại và có cảnh báo, không cộng cho user. Sau khi chốt không tự ghép hoặc tự phân phối phần này về sau. Nếu chưa ghép được bất kỳ người nhận nào, phải xử lý ghép hợp lệ trong Quản trị hoàn tiền trước.
- Với actual_commission, đọc lại snapshot đã lưu của từng SKU, kiểm tra đủ SKU, VND và tổng khớp khoản actual đã lưu. Không xem estimated commission là tiền thực nhận. Không trừ `pit`, chia sẻ agency hoặc thuế thêm lần nữa.
- Với hoa hồng nhập tay, chia số tiền sau thuế theo trọng số hoa hồng của attribution đã lưu; nếu chỉ một dòng thì nhận toàn bộ trọng số. Khi nhiều dòng đều có trọng số 0 và số tiền cần chia lớn hơn 0, chặn để tránh tự chia tùy ý.
- Dùng tỷ lệ chia đã lưu tại từng attribution, không nhập lại hoặc thay bằng tỷ lệ hiện tại trong bước duyệt. Tiền hoàn mỗi user được làm tròn đồng; không vượt phần net của chính user. Khoản thuế/net giữ tối đa 4 số thập phân, cùng độ chính xác sổ đơn. Hoa hồng nhập tay tối đa 1.000 tỷ đồng.
- Trạng thái REFUNDED/FROZEN/chưa thanh toán vẫn có thể duyệt nhập tay nếu đủ dữ liệu người nhận; trang cảnh báo để admin kiểm tra và lưu lý do quyết định. Không đổi trạng thái nguồn TikTok để giả thành SETTLED.

## Chống chốt trùng và lưu vết

- Xem trước và duyệt là hai POST có antiforgery, yêu cầu role admin và quyền Orders, chỉ hỗ trợ host. Service không mở thành API application từ xa (`RemoteService` tắt).
- Bản xác nhận được Data Protection ký/mã hóa theo admin, chứa order ID, input và hash phương án, hết hạn 10 phút. Chỉ form xác nhận này được chốt; sửa field ở form nhập phải bấm xem trước lại.
- Khi duyệt, cùng advisory lock PostgreSQL với import TikTok chặn sync/duyệt song song giữa local/production. Transaction ngắn khóa dòng Order, OrderItem và OrderItemAttribution; lập lại phương án và so hash. Dữ liệu/owner/rate/số tiền thay đổi thì yêu cầu xem trước lại.
- Chốt Order và attribution cùng một transaction, sử dụng `SettledAt`/trạng thái Settled để chống ghi nhận lại. Ví dùng `WalletBalanceCalculator` hiện có, không gọi thêm một bước tăng balance độc lập. Các cập nhật đồng thời khác chịu row lock và concurrency stamp của aggregate.
- Lưu trong ExtraProperties: admin duyệt, lý do, nguồn số tiền, hoa hồng gốc, thuế/tỷ lệ, trạng thái nguồn và trạng thái nội bộ tại lúc duyệt, Creator, hash, chi tiết attribution/user/tracking/tỷ lệ/net/hoàn tiền. Thời gian và mã đối soát lưu ở `SettledAt`/`SettlementReference`.
- Đồng bộ sau khi đã duyệt vẫn giữ nguyên khoản đã cộng. Khi TikTok thay đổi dữ liệu, ghi chú cần kiểm tra; chưa có luồng tự thu hồi/điều chỉnh âm hoàn tiền. Không tự đảo khoản đã chốt.
- Lịch sử ví TikTok sau khi duyệt dùng nhãn tiền đã ghi nhận và thời điểm admin duyệt. Nhánh hiển thị/tính tiền Shopee giữ nguyên.

## Triển khai và phạm vi kiểm tra

Không cần migration mới: dùng sổ Order/OrderItemAttribution và ExtraProperties đã có. Cần deploy source mới để thấy nút và màn hình đối soát. Không áp dụng thay đổi database, gọi API Creator thật, duyệt đơn hoặc cộng ví thực tế trong phiên lập trình này. Chưa chạy test/kiểm thử trình duyệt theo hướng dẫn dự án; build Web và kiểm tra diff dùng để kiểm tra biên dịch/thay đổi source.

Nguồn đã đọc lại ngày 15/10/2026: [Search Creator Affiliate Orders 202410](https://partner.tiktokshop.com/docv2/page/6736985bd99b7d02fd715482), metadata API Reference chính thức `api/v1/document/api_meta?src_document_id=6736985bd99b7d02fd715482`. Tài liệu mô tả actual_commission đã trừ thuế/chia sẻ agency; chính sách cho admin duyệt mọi trạng thái và thuế mặc định trên hoa hồng nhập tay do chủ dự án xác nhận trong chat.
