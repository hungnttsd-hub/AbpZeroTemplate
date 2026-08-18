# ADR 0001 — Để Product Search sang Phase 2

Status: Accepted — 2026-08-18

Shopee Open API xác nhận `productOfferV2` hỗ trợ `keyword`, `shopId`, `itemId`, category, sort và phân trang. Phase 1 chỉ dùng `itemId` để lấy metadata/commission estimate sau đăng nhập.

Không tạo catalog schema, search API, cache index hoặc UI tìm sản phẩm ở Phase 1. Việc để code/schema chết lúc này làm tăng chi phí đồng bộ và ràng buộc Shopee vào core domain. Phase 2 sẽ thiết kế search như capability riêng của provider, kèm cache/catalog policy sau khi có dữ liệu và rate-limit thực tế.
