const DEFAULTS = {
  reportUrl: "https://affiliate.shopee.vn/report/conversion_report",
  exportManagementUrl: "https://affiliate.shopee.vn/export_management",
  intervalMinutes: 60,
  exportButtonSelector: "",
  exportButtonTexts: ["Xuất dữ liệu", "Xuất báo cáo", "Export"],
  pageReadyDelayMs: 2500,
  exportTimeoutMs: 180000,
  exportPollMs: 3000,
  refreshExportPageEveryMs: 15000,
  keepShopeeTabsOpen: true,
  openShopeeTabsActive: false,
  notifyOnSuccess: false
};

const HELPER_BASE = "http://127.0.0.1:32145";
let helperAvailable = false;

document.getElementById("save").addEventListener("click", save);
document.getElementById("testApi").addEventListener("click", testApiConnection);
document.getElementById("openHelperSettings").addEventListener("click", () => {
  chrome.tabs.create({ url: `${HELPER_BASE}/settings`, active: true });
});

load();

async function save() {
  const data = {
    reportUrl: document.getElementById("reportUrl").value.trim() || DEFAULTS.reportUrl,
    exportManagementUrl: document.getElementById("exportManagementUrl").value.trim() || DEFAULTS.exportManagementUrl,
    intervalMinutes: Math.max(60, Number(document.getElementById("intervalMinutes").value) || 60),
    exportButtonSelector: document.getElementById("exportButtonSelector").value.trim(),
    exportButtonTexts: document.getElementById("exportButtonTexts").value.split("\n").map(x => x.trim()).filter(Boolean),
    pageReadyDelayMs: Math.max(500, Number(document.getElementById("pageReadyDelayMs").value) || 2500),
    exportTimeoutMs: Math.max(30000, Number(document.getElementById("exportTimeoutMs").value) || 180000),
    exportPollMs: Math.max(1000, Number(document.getElementById("exportPollMs").value) || 3000),
    refreshExportPageEveryMs: Math.max(0, Number(document.getElementById("refreshExportPageEveryMs").value) || 15000),
    keepShopeeTabsOpen: document.getElementById("keepShopeeTabsOpen").checked,
    openShopeeTabsActive: document.getElementById("openShopeeTabsActive").checked,
    notifyOnSuccess: document.getElementById("notifyOnSuccess").checked
  };

  await chrome.storage.local.set(data);

  let helperMessage = "";
  try {
    await saveHelperSettings();
    helperMessage = " + CatsBack API";
  } catch (error) {
    helperMessage = " (Shopee đã lưu; CatsBack chưa lưu vì Helper chưa chạy)";
    setHelperStatus(false, `Không kết nối được Local Helper: ${error.message}`);
  }

  document.getElementById("saved").textContent = `Đã lưu${helperMessage}`;
  setTimeout(() => document.getElementById("saved").textContent = "", 3000);
}

async function load() {
  const data = await chrome.storage.local.get(DEFAULTS);
  document.getElementById("reportUrl").value = data.reportUrl;
  document.getElementById("exportManagementUrl").value = data.exportManagementUrl;
  document.getElementById("intervalMinutes").value = data.intervalMinutes;
  document.getElementById("exportButtonSelector").value = data.exportButtonSelector;
  document.getElementById("exportButtonTexts").value = (data.exportButtonTexts || []).join("\n");
  document.getElementById("pageReadyDelayMs").value = data.pageReadyDelayMs;
  document.getElementById("exportTimeoutMs").value = data.exportTimeoutMs;
  document.getElementById("exportPollMs").value = data.exportPollMs;
  document.getElementById("refreshExportPageEveryMs").value = data.refreshExportPageEveryMs;
  document.getElementById("keepShopeeTabsOpen").checked = Boolean(data.keepShopeeTabsOpen);
  document.getElementById("openShopeeTabsActive").checked = Boolean(data.openShopeeTabsActive);
  document.getElementById("notifyOnSuccess").checked = Boolean(data.notifyOnSuccess);

  await loadHelperSettings();
}

async function loadHelperSettings() {
  try {
    const response = await fetch(`${HELPER_BASE}/api/settings`, { cache: "no-store" });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const data = await response.json();

    helperAvailable = true;
    document.getElementById("apiBaseUrl").value = data.apiBaseUrl || "https://catsback.onrender.com";
    document.getElementById("clientId").value = data.clientId || "";
    document.getElementById("clientSecret").value = "";
    document.getElementById("clearClientSecret").checked = false;
    document.getElementById("tokenPath").value = data.tokenPath || "/api/public/shopee-automation/token";
    document.getElementById("importPath").value = data.importPath || "/api/public/shopee-automation/reports/import";
    document.getElementById("formFieldName").value = data.formFieldName || "report";
    document.getElementById("tokenRefreshSkewSeconds").value = data.tokenRefreshSkewSeconds ?? 60;
    document.getElementById("secretState").textContent = data.hasClientSecret
      ? "Đã có Client Secret trong Local Helper. Để trống ô Secret khi lưu nếu muốn giữ giá trị cũ."
      : "Chưa có Client Secret trong Local Helper.";

    setHelperStatus(true, data.apiConfigured
      ? "Local Helper đang chạy. Client credentials đã cấu hình; Helper sẽ tự lấy Bearer token khi cần."
      : "Local Helper đang chạy. Chỉ cần nhập Client ID và Client Secret khi bạn có.");
  } catch (error) {
    helperAvailable = false;
    setHelperStatus(false, "Local Helper chưa chạy. Hãy chạy local-helper/start-helper.cmd trước khi lưu Client ID/Secret.");
  }
}

async function saveHelperSettings() {
  await ensureHelper();

  const payload = {
    apiBaseUrl: document.getElementById("apiBaseUrl").value.trim() || "https://catsback.onrender.com",
    clientId: document.getElementById("clientId").value.trim(),
    clientSecret: document.getElementById("clientSecret").value,
    clearClientSecret: document.getElementById("clearClientSecret").checked,
    tokenPath: document.getElementById("tokenPath").value.trim() || "/api/public/shopee-automation/token",
    importPath: document.getElementById("importPath").value.trim() || "/api/public/shopee-automation/reports/import",
    formFieldName: document.getElementById("formFieldName").value.trim() || "report",
    tokenRefreshSkewSeconds: Math.max(0, Number(document.getElementById("tokenRefreshSkewSeconds").value) || 60)
  };

  const response = await fetch(`${HELPER_BASE}/api/settings`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  const result = await response.json().catch(() => ({}));
  if (!response.ok || result.ok === false) {
    throw new Error(result.error || `HTTP ${response.status}`);
  }

  document.getElementById("clientSecret").value = "";
  document.getElementById("clearClientSecret").checked = false;
  document.getElementById("secretState").textContent = result.hasClientSecret
    ? "Đã có Client Secret trong Local Helper. Để trống ô Secret khi lưu nếu muốn giữ giá trị cũ."
    : "Chưa có Client Secret trong Local Helper.";
  setHelperStatus(true, result.apiConfigured
    ? "Đã lưu. Helper sẵn sàng tự lấy Bearer token và upload CSV."
    : "Đã lưu. Chưa đủ Client ID/Client Secret nên Helper sẽ giữ file và chưa upload.");

  return result;
}

async function testApiConnection() {
  const button = document.getElementById("testApi");
  button.disabled = true;
  try {
    const saved = await saveHelperSettings();
    if (!saved.apiConfigured) throw new Error("Chưa đủ Client ID/Client Secret.");

    setHelperStatus(true, "Đang gọi token endpoint để kiểm tra...");
    const response = await fetch(`${HELPER_BASE}/api/test-connection`, { method: "POST" });
    const result = await response.json().catch(() => ({}));
    if (!response.ok || result.ok === false) throw new Error(result.error || `HTTP ${response.status}`);

    setHelperStatus(true, `Kết nối CatsBack OK. Nhận token ${result.tokenType || "Bearer"}; hết hạn lúc ${result.expiresAtUtc}.`);
  } catch (error) {
    setHelperStatus(false, `Kiểm tra thất bại: ${error.message}`);
  } finally {
    button.disabled = false;
  }
}

async function ensureHelper() {
  if (helperAvailable) return;
  const health = await fetch(`${HELPER_BASE}/health`, { cache: "no-store" });
  if (!health.ok) throw new Error(`HTTP ${health.status}`);
  helperAvailable = true;
}

function setHelperStatus(ok, message) {
  const el = document.getElementById("helperStatus");
  el.textContent = message;
  el.className = `api-status ${ok ? "ok" : "warn"}`;
}
