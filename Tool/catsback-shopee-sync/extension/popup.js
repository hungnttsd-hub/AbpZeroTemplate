const statusEl = document.getElementById("status");
const messageEl = document.getElementById("message");
const lastRunEl = document.getElementById("lastRun");
const lastFileEl = document.getElementById("lastFile");
const helperEl = document.getElementById("helper");
const syncNowBtn = document.getElementById("syncNow");
const clearLockBtn = document.getElementById("clearLock");
const exportSettlementsBtn = document.getElementById("exportSettlements");
const importSettlementsBtn = document.getElementById("importSettlements");
const settlementResultEl = document.getElementById("settlementResult");

refresh();

exportSettlementsBtn.addEventListener("click", () => runSettlementAction("export"));
importSettlementsBtn.addEventListener("click", () => runSettlementAction("import"));

syncNowBtn.addEventListener("click", async () => {
  syncNowBtn.disabled = true;
  syncNowBtn.textContent = "Đang chạy...";
  const result = await chrome.runtime.sendMessage({ type: "RUN_SYNC_NOW" });
  await refresh();
  syncNowBtn.disabled = false;
  syncNowBtn.textContent = "Đồng bộ ngay";
  if (!result?.ok) {
    messageEl.textContent = result?.error || result?.status || "Sync chưa hoàn tất.";
  }
});

clearLockBtn.addEventListener("click", async () => {
  clearLockBtn.disabled = true;
  const result = await chrome.runtime.sendMessage({ type: "CLEAR_STALE_LOCK" });
  messageEl.textContent = result?.message || "Đã xử lý khóa đồng bộ.";
  clearLockBtn.disabled = false;
  await refresh();
});

document.getElementById("openReport").addEventListener("click", async () => {
  const { reportUrl } = await chrome.storage.local.get({
    reportUrl: "https://affiliate.shopee.vn/report/conversion_report"
  });
  chrome.tabs.create({ url: reportUrl, active: true });
});

document.getElementById("openExports").addEventListener("click", async () => {
  const { exportManagementUrl } = await chrome.storage.local.get({
    exportManagementUrl: "https://affiliate.shopee.vn/export_management"
  });
  chrome.tabs.create({ url: exportManagementUrl, active: true });
});

document.getElementById("options").addEventListener("click", () => chrome.runtime.openOptionsPage());

async function refresh() {
  const data = await chrome.runtime.sendMessage({ type: "GET_STATUS" });
  statusEl.textContent = data?.lastStatus || "Chưa chạy";
  messageEl.textContent = data?.lastMessage || "";
  lastRunEl.textContent = data?.lastRunAt
    ? `Lần chạy gần nhất: ${new Date(data.lastRunAt).toLocaleString("vi-VN")}`
    : "";
  lastFileEl.textContent = data?.lastDownloadedFile
    ? `File gần nhất: ${String(data.lastDownloadedFile).replace(/\\/g, "/").split("/").pop()}`
    : "";
  helperEl.textContent = data?.helperOnline
    ? `Local Helper: đang chạy${data?.apiConfigured ? " - API đã cấu hình" : " - chưa cấu hình Client ID/Secret"}${data?.helperWatchDir ? ` - ${data.helperWatchDir}` : ""}`
    : "Local Helper: chưa chạy";

  syncNowBtn.disabled = Boolean(data?.isRunning);
  syncNowBtn.textContent = data?.isRunning ? "Đang chạy..." : "Đồng bộ ngay";
  const hasPersistedLock = Boolean(data?.syncLockAt);
  clearLockBtn.style.display = hasPersistedLock && !data?.isRunning ? "block" : "none";
}

async function runSettlementAction(action) {
  const isImport = action === "import";
  exportSettlementsBtn.disabled = true;
  importSettlementsBtn.disabled = true;
  settlementResultEl.textContent = isImport
    ? "Đang lấy toàn bộ bảng kê trong danh sách và gửi về CatsBack..."
    : "Đang lấy toàn bộ bảng kê trong danh sách và tổng hợp CSV...";

  try {
    const result = await chrome.runtime.sendMessage({
      type: isImport ? "IMPORT_SHOPEE_SETTLEMENTS" : "EXPORT_SHOPEE_SETTLEMENTS"
    });
    if (!result?.ok) throw new Error(result?.error || "Không thể tổng hợp đối soát.");

    const parts = [
      `${result.validationCount || 0} bảng kê`,
      `${result.rowCount || 0} đơn`,
      result.filePath ? `File: ${result.filePath}` : ""
    ].filter(Boolean);
    if (isImport && result.importSummary) parts.push(result.importSummary);
    settlementResultEl.textContent = parts.join(" · ");
  } catch (error) {
    settlementResultEl.textContent = error?.message || String(error);
  } finally {
    exportSettlementsBtn.disabled = false;
    importSettlementsBtn.disabled = false;
  }
}
