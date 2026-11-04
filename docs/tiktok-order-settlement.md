# Đối soát và ghi nhận hoàn tiền đơn TikTok

Triển khai source ngày 15/10/2026, cập nhật duyệt hàng loạt ngày 18/10/2026 và đối soát tự động/thuế thiếu thông tin ngày 03/11/2026 theo quyết định của chủ dự án:

- Admin được duyệt thủ công ở **mọi trạng thái** TikTok; không bắt buộc sàn đã trả `SETTLED` hoặc xác nhận CatBack đã nhận tiền.
- Dùng `actual_commission` của TikTok: đọc `pit` VND theo từng SKU. Nếu **thiếu thông tin thuế** hoặc `pit = 0đ`, CatBack trừ bổ sung **10%** trên actual của SKU đó; nếu `pit > 0đ`, không trừ thêm. Thiếu nghĩa là không có `pit`, `pit = null`, hoặc object thuế không có amount/amount null/chuỗi trắng và không có currency mâu thuẫn. Thuế có dữ liệu nhưng sai định dạng, âm hoặc không phải VND vẫn chặn duyệt; không tự coi dữ liệu lỗi là không có thuế. Đây là chính sách đối soát của chủ dự án, không phải khẳng định TikTok luôn thu 10%.
- Nhập hoa hồng **trước thuế**: mặc định thuế **10%**, admin được sửa trong khoảng 0–100%; nhập **0% tự chuyển thành 10%** khi xem trước và duyệt.
- Không thêm phí dịch vụ mặc định cho TikTok. Không thay đổi đối soát hoặc tính tiền Shopee.

## Tự động đối soát trong job đồng bộ — 03/11/2026

- Job `tiktok-creator-orders-hourly` giữ nguyên lịch mỗi giờ và cấu hình `TikTokShop:OrderSync`.
  Sau khi đồng bộ xong, job tự đối soát những đơn có trạng thái nguồn **SETTLED** vừa được
  lưu thành công trong lượt đó. Không lấy các đơn import lỗi hoặc chỉ có snapshot cũ ngoài
  danh sách kết quả; không dùng estimated commission và không áp dụng điều kiện 7 ngày của Shopee.
- Đồng bộ thủ công qua nút lấy đơn vẫn chỉ lưu dữ liệu. Các đơn chưa tự đối soát được vẫn có
  thể xem trước và duyệt bằng các nút đối soát hiện có.
- `TikTokSettlementService` dùng chung cách tính/phân bổ/chốt tiền cho job và admin.
  App service thủ công vẫn giữ quyền role admin, `Affiliate.Admin.Orders`, host và hash xác nhận;
  job gọi service nội bộ, không giả mạo phiên đăng nhập hoặc mở API duyệt không cần quyền.
- Mỗi đơn được kiểm tra lại trong transaction riêng, với advisory lock và row lock như duyệt tay.
  Phải đúng Creator chính, đủ SKU và actual VND, tất cả attribution ghép đúng user/tracking,
  user còn hoạt động, không có cảnh báo dữ liệu/gian lận hoặc chốt dở dang. Khác với duyệt tay,
  job bỏ qua cả đơn có một phần attribution chưa ghép để admin kiểm tra.
- Thiếu thuế/bằng 0: áp 10% trên actual của **từng SKU** trước khi chia hoàn tiền cho user;
  SKU đã khấu trừ thuế giữ nguyên actual. Làm tròn khoản thuế đến 4 số thập phân.
  Tổng thuế TikTok trả được để `null` khi có SKU thiếu thuế, không hiển thị nhầm là đã biết bằng 0.
  Lưu số SKU thiếu thuế và policy `MissingOrZeroWithholdingTenPercentV2` cùng số tiền chốt.
- Nguồn tự động lưu `TikTokSettlementSource = OrderSyncJob`, lý do và thời điểm đối soát;
  không gán một admin giả vào `TikTokSettlementApprovedBy`. Chi tiết đơn hiển thị tác vụ đã duyệt.
  Nguồn thủ công lưu `Manual` và ID admin. Đơn đã chốt trước đó giữ nguyên số tiền và thuế đã lưu.
- Đơn không đủ điều kiện được bỏ qua và ghi lý do trong log. Lỗi kỹ thuật được ghi riêng;
  không làm mất các đơn đã đối soát thành công. Nếu có lỗi import/đối soát, job kết thúc Failed
  và lịch giờ tiếp theo đọc lại. Log Hangfire và `AffiliateSyncRun.ErrorSummary` lưu số duyệt,
  số đã chốt trước, số bỏ qua/lỗi và tổng cộng ví. Ví tiếp tục đọc ledger, không cộng thêm lần hai.

Không đổi endpoint/version/request Creator API, không thêm scope và không gọi API đối soát mới.
Build Web thành công; chưa chạy test, trình duyệt, job hoặc đối soát/cộng ví thật trong lần sửa này.

## Hoàn tiền dự kiến sau thuế dự phòng — 03/11/2026

- Link sản phẩm: hoa hồng sản phẩm gốc được trừ thuế dự phòng 10% (làm tròn 4 số thập phân),
  rồi chia theo chính sách 80% cho hai đơn hợp lệ đầu trên TikTok, hoặc tỷ lệ rule hiện hành
  từ đơn thứ ba (hiện tại 60%). Áp dụng cho cả đầu min/max của khoảng tiền hoàn, làm tròn
  tiền user nhận theo đồng và không vượt phần net. Khoản hoa hồng Creator hiển thị vẫn là số gốc.
- Ví dụ: 112.800đ hoa hồng → dự phòng 11.280đ thuế → net 101.520đ → user 80% nhận dự kiến
  81.216đ; tỷ lệ 60% nhận dự kiến 60.912đ.
- `TikTokCashbackEstimateCalculator` tính dự kiến đơn đang chờ từ `AllocatedNetCommission`
  gốc và tỷ lệ đã lưu của từng attribution. Snapshot có `pit > 0` VND hợp lệ thì không trừ thêm;
  thiếu/0/chưa đọc được thuế thì dự phòng 10%. Dữ liệu thuế lỗi vẫn bị dịch vụ đối soát chặn
  khi chốt tiền. Dự kiến không phải xác nhận đơn đủ điều kiện duyệt.
- Danh sách đơn, chi tiết SKU/người nhận, tổng hoàn tiền dự kiến, phần chờ hoàn và lịch sử
  chờ hoàn trong ví cùng dùng projection này. Đơn cũ chưa có snapshot cũng được dự phòng 10%
  ngay khi đọc; không cần đồng bộ đơn hoặc chạy backfill trước. Không lấy số dự kiến đã làm tròn
  để trừ thuế lần nữa; mỗi lần đọc đều tính từ tiền gốc. Các snapshot nghiệp vụ gốc được giữ để
  phục vụ đối soát và truy vết, không ghi đè lịch sử thông báo.
- Link đã lưu được tính lại từ min/max hoa hồng gốc khi tạo/mở lại/xem danh sách link,
  kể cả khi metadata còn trong cache 15 phút. Không cần tạo lại link.
- Dùng chung `TikTokCashbackTaxPolicy` với đối soát actual, nhưng không ghi chốt ledger,
  không sửa đơn đã đối soát hoặc số dư có thể rút. Quyền xem theo user hiện có giữ nguyên.
  Chính sách Shopee không thay đổi. Không cần migration hoặc gọi thêm API TikTok.

## Thao tác

1. Admin có quyền `Affiliate.Admin.Orders` vào `/Admin/TikTokOrders`. Job Creator API đồng bộ mỗi giờ (mặc định đọc lại 90 ngày), sau đó tự đối soát các đơn SETTLED đủ điều kiện như trên. Có thể lấy lại đơn qua nút đồng bộ nếu có quyền Sync; nút này chỉ lưu dữ liệu. Xem [cấu hình job](tiktok-shop-creator-api.md#tự-động-đồng-bộ-đơn-hàng-mỗi-giờ--16102026).
2. Bấm **Đối soát** ở đơn cần duyệt, mở `/Admin/TikTokSettlements/{orderId}`. Trang hiển thị Creator, trạng thái TikTok, thời điểm đồng bộ và các khoản dự kiến/thực nhận từ database.
3. Chọn nguồn số tiền. Chỉ dùng actual_commission khi có snapshot SETTLED hợp lệ và đủ số tiền VND theo SKU. Với trạng thái khác hoặc muốn duyệt số tiền khác, chọn **Admin nhập hoa hồng trước thuế**, nhập số tiền và tỷ lệ thuế. Không tự điền khoản net/estimated từ API thành số tiền trước thuế.
4. Nhập lý do duyệt 10–1.000 ký tự, bấm **Xem trước số tiền cộng ví**. Kiểm tra hoa hồng, thuế, số tiền sau thuế, từng người nhận và các cảnh báo.
5. Đánh dấu xác nhận, bấm **Duyệt đối soát và cộng ví**. Đơn chuyển `AffiliateOrderStatus.Settled`, chốt số tiền trên từng attribution; ví đọc trực tiếp sổ đơn đã chốt. Thông báo ghi nhận hoàn tiền được lưu cùng transaction.
6. Duyệt lại đơn đã chốt chỉ trả thông báo đã duyệt, không ghi nhận khoản thứ hai. Có thể mở **Xem đối soát** để xem tiền, thời gian, admin và lý do đã chốt.

Ví dụ: admin nhập trước thuế 100.000đ, thuế 10% → còn 90.000đ. Nếu tỷ lệ đã lưu của người nhận là 80%, cộng ví 72.000đ. Với actual_commission 12.800đ và pit 0đ → thuế bổ sung 1.280đ, còn 11.520đ trước khi chia hoàn tiền. Nếu pit đã lớn hơn 0đ thì giữ nguyên actual, không trừ pit thêm lần nữa.

## Duyệt toàn bộ đơn đã thanh toán — 18/10/2026

- Tại `/Admin/TikTokOrders`, nút **Duyệt toàn bộ đơn TikTok đã thanh toán** mở `/Admin/TikTokSettlements/Bulk`. Duyệt từng đơn tiếp tục qua nút **Duyệt đối soát** tại mỗi dòng.
- Xem trước tất cả đơn có `TikTokProviderStatus = SETTLED` chưa được CatBack chốt, không giới hạn trang hoặc bộ lọc ngày ở danh sách đơn. Chỉ dùng actual, không tự nhập hoa hồng ước tính. Đơn sai Creator, thiếu người nhận/SKU, thuế lỗi hoặc có xung đột được liệt kê với lý do bỏ qua; thiếu thông tin thuế áp 10% như quy tắc hiện hành.
- Màn hình hiển thị hoa hồng thực nhận, thuế TikTok đã trừ, thuế bổ sung, từng người nhận và tổng tiền cộng ví của các đơn đủ điều kiện. Admin xác nhận danh sách và lý do trước khi duyệt.
- Bản xem trước được mã hóa/ký bằng Data Protection theo admin và lưu cache 10 phút; form chỉ gửi mã tra cứu ngẫu nhiên. Cache mất/hết hạn thì phải xem trước lại. Chỉ các ID/hash có trong bản này được duyệt, không tự thêm đơn xuất hiện sau đó.
- Mỗi đơn duyệt trong transaction riêng, dùng khóa đồng bộ/sổ đơn hiện có và kiểm tra lại SETTLED, dữ liệu/hash/người nhận/thuế. Đơn thay đổi được bỏ qua và trả kết quả riêng; đơn đã chốt không cộng lần nữa. Sau khi hoàn tất, hiển thị kết quả từng đơn và xóa bản xem trước.
- SKU được phân bổ theo số tiền sau thuế riêng để đơn nhiều SKU có cả pit 0đ và pit lớn hơn 0đ không bị chia sai thuế. Lưu thêm tổng pit, cơ sở tính thuế bổ sung và phiên bản chính sách thuế vào ExtraProperties. Đơn đã chốt từ trước giữ nguyên số tiền và quy tắc đã lưu.

## Kiểm tra và phân bổ

- Luồng chỉ xử lý `AffiliatePlatform.TikTok`, đối chiếu Creator của đơn với Creator hoàn tiền `IsPrimary` do admin đăng ký. Không chọn Creator theo user hiện tại, không dùng token/bảng Developer. Registry chỉ đọc ID đã đăng ký, không yêu cầu token đang hoạt động và không gọi TikTok khi xem trước/duyệt.
- Không đoán người nhận theo tên/sản phẩm/thời gian mua. Phải có attribution Matched với user, tracking và đúng Creator/source TikTok của CatBack. User đã xóa, attribution conflict, dữ liệu thiếu hoặc đã chốt dở dang sẽ chặn duyệt.
- Có thể duyệt khi một phần attribution chưa ghép, giống luồng Shopee: phần chưa ghép được giữ lại và có cảnh báo, không cộng cho user. Sau khi chốt không tự ghép hoặc tự phân phối phần này về sau. Nếu chưa ghép được bất kỳ người nhận nào, phải xử lý ghép hợp lệ trong Quản trị hoàn tiền trước.
- Với actual_commission, đọc lại snapshot đã lưu của từng SKU, kiểm tra đủ SKU, VND, thông tin pit nếu có và tổng khớp khoản actual đã lưu. Không xem estimated commission là tiền thực nhận. Không trừ lại pit/agency; chỉ áp dụng khoản bổ sung 10% trên SKU thiếu thông tin thuế hoặc có pit bằng 0 theo quy tắc trên.
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

Nguồn đã đọc lại ngày 18/10/2026: [Search Creator Affiliate Orders 202410](https://partner.tiktokshop.com/docv2/page/6736985bd99b7d02fd715482), metadata API Reference chính thức `api/v1/document/api_meta?src_document_id=6736985bd99b7d02fd715482`. Tài liệu mô tả actual_commission đã trừ thuế/chia sẻ agency và pit là khoản thuế khấu trừ. Chính sách bổ sung 10% khi thuế bằng 0 và duyệt hàng loạt do chủ dự án yêu cầu trong chat.
