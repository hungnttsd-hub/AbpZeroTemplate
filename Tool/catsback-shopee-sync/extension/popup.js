const statusEl = document.getElementById("status");
const messageEl = document.getElementById("message");
const lastRunEl = document.getElementById("lastRun");
const lastFileEl = document.getElementById("lastFile");
const helperEl = document.getElementById("helper");
const syncNowBtn = document.getElementById("syncNow");

refresh();

syncNowBtn.addEventListener("click", async () => {
  syncNowBtn.disabled = true;
  syncNowBtn.textContent = "Đang chạy...";
  const result = await chrome.runtime.sendMessage({ type: "RUN_SYNC_NOW" });
  await refresh();
  syncNowBtn.disabled = false;
  syncNowBtn.textContent = "Đồng bộ ngay";
  if (!result?.ok && result?.reason !== "locked") {
    messageEl.textContent = result?.error || result?.status || "Sync chưa hoàn tất.";
  }
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
    ? `Local Helper: đang chạy${data?.apiConfigured ? " - API đã cấu hình" : " - chưa cấu hình Client ID/Secret"}`
    : "Local Helper: chưa chạy";
}
