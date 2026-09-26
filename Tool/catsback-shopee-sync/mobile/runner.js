const host = document.createElement("div");
host.id = "catsback-mobile-settlements";
host.style.cssText = "position:fixed;z-index:2147483647;bottom:12px;left:12px;width:calc(100% - 24px);max-width:420px;";
const root = host.attachShadow({ mode: "closed" });
// Build the panel without innerHTML, which can require TrustedHTML on the host page.
const create = (tag, text = "", attributes = {}, children = []) => {
  const element = document.createElement(tag);
  if (text) element.textContent = text;
  for (const [name, value] of Object.entries(attributes)) element.setAttribute(name, value);
  element.append(...children);
  return element;
};
const style = create("style");
style.textContent = `
    :host { color-scheme:light; } * { box-sizing:border-box; }
    section { padding:16px;background:#fff;color:#18232f;border:2px solid #186959;border-radius:16px;font:15px/1.5 system-ui,sans-serif;box-shadow:0 6px 30px #0004;max-height:65vh;overflow:auto; }
    header { display:flex;align-items:center;justify-content:space-between;gap:8px; }
    button,a { font:inherit;min-height:44px;padding:10px 14px;border-radius:9px; }
    button { border:1px solid #baccc7;background:#eff6f4;color:#143d34;cursor:pointer; }
    button:disabled { opacity:.5;cursor:default; } p { margin:10px 0; }
    a { display:block;text-align:center;background:#186959;color:#fff;text-decoration:none;margin-top:10px; }
    [hidden] { display:none!important; } .actions { display:flex;gap:8px;flex-wrap:wrap; }
    small { display:block;color:#4b5c65; } #status { white-space:pre-wrap;overflow-wrap:anywhere; }
`;
root.append(style, create("section", "", { "aria-label": "CatsBack tổng hợp JSON" }, [
  create("header", "", {}, [
    create("strong", "CatsBack · JSON v2"),
    create("button", "Thu gọn", { id: "toggle", "aria-expanded": "true" })
  ]),
  create("div", "", { id: "body" }, [
    create("p", "Đổi bộ lọc/kỳ hoặc chuyển trang danh sách Billing để Shopee tải lại dữ liệu. Không tải lại toàn bộ tab.",
      { id: "status", role: "status", "aria-live": "polite" }),
    create("small", "Đang chờ danh sách bảng kê…", { id: "capture" }),
    create("p", "", {}, [create("small", "Chỉ tổng hợp danh sách Shopee vừa tải, không tự quét các trang khác. Giữ tab này mở và màn hình sáng khi chạy.")]),
    create("div", "", { class: "actions" }, [
      create("button", "Tổng hợp JSON", { id: "start", disabled: "" }),
      create("button", "Dừng", { id: "cancel", hidden: "" }),
      create("button", "Đóng", { id: "close" })
    ]),
    create("a", "Tải file JSON", { id: "download", hidden: "" })
  ])
]));
const get = id => root.getElementById(id);
let running = false;
let controller;
let objectUrl;
let wakeLock;
let latestCapture;
const updateCapture = () => {
  const capture = window.__catsBackBillingListNetworkCaptureV1?.latest;
  if (capture !== latestCapture) {
    latestCapture = capture;
    get("capture").textContent = capture
      ? `Đã nhận danh sách lúc ${new Date(capture.capturedAt).toLocaleTimeString("vi-VN")}.`
      : "Đang chờ danh sách bảng kê…";
  }
  get("start").disabled = running || !capture;
};
updateCapture();
const captureTimer = setInterval(updateCapture, 500);
get("toggle").onclick = () => {
  get("body").hidden = !get("body").hidden;
  get("toggle").textContent = get("body").hidden ? "Mở" : "Thu gọn";
  get("toggle").setAttribute("aria-expanded", String(!get("body").hidden));
};
host.addEventListener("catsback:show", () => {
  get("body").hidden = false;
  get("toggle").textContent = "Thu gọn";
  get("toggle").setAttribute("aria-expanded", "true");
});
get("cancel").onclick = () => controller?.abort(new Error("Đã dừng. Chưa tạo file JSON."));
get("close").onclick = () => {
  if (running) return;
  clearInterval(captureTimer);
  if (objectUrl) URL.revokeObjectURL(objectUrl);
  host.remove();
};
document.documentElement.append(host);
get("start").onclick = async () => {
  if (running) return;
  running = true;
  controller = new AbortController();
  get("start").disabled = true;
  get("close").disabled = true;
  get("cancel").hidden = false;
  get("download").hidden = true;
  if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
  get("status").textContent = "Đang tổng hợp. Giữ nguyên bộ lọc và tab Shopee…";
  try {
    try { wakeLock = await navigator.wakeLock?.request("screen"); } catch (_) { /* Optional on mobile. */ }
    const report = await collectShopeeSettlementRowsInPage({
      signal: controller.signal,
      onProgress: message => { get("status").textContent = message; }
    });
    controller.signal.throwIfAborted();
    if (!report?.ok) throw new Error(report?.error || "Không tổng hợp được dữ liệu.");
    if (!report.rows?.length) throw new Error("Danh sách vừa tải không có đơn hàng để xuất. Chọn kỳ khác rồi thử lại.");
    const validationCount = new Set(report.rows.map(row => `${row.source_affiliate_id}/${row.validation_id}`)).size;
    const exportedAt = new Date().toISOString();
    const data = {
      schemaVersion: report.schemaVersion,
      exportedAt,
      validationCount,
      rows: report.rows
    };
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json;charset=utf-8" });
    if (blob.size > 5 * 1024 * 1024) throw new Error("JSON vượt 5 MB. Chọn ít kỳ hơn rồi tổng hợp lại để import vào CatsBack.");
    objectUrl = URL.createObjectURL(blob);
    get("download").href = objectUrl;
    get("download").download = `catsback-settlements-${exportedAt.replace(/[:.]/g, "-")}.json`;
    get("download").hidden = false;
    get("status").textContent = `Đã tổng hợp ${validationCount} bảng kê / ${report.rows.length} dòng đơn hàng. Bấm “Tải file JSON”, sau đó chọn file trong mục Import đối soát của CatsBack.`;
  } catch (error) {
    get("status").textContent = error?.message || String(error);
  } finally {
    running = false;
    get("close").disabled = false;
    get("cancel").hidden = true;
    updateCapture();
    try { await wakeLock?.release(); } catch (_) { /* Already released. */ }
    wakeLock = null;
  }
};
