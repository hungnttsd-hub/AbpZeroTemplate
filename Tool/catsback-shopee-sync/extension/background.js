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

const ALARM_NAME = "catsback-shopee-hourly-sync";
const LOCK_TTL_MS = 12 * 60 * 1000;

chrome.runtime.onInstalled.addListener(async () => {
  const current = await chrome.storage.local.get(DEFAULTS);
  await chrome.storage.local.set(current);
  await ensureAlarm();
});

chrome.runtime.onStartup.addListener(async () => {
  await ensureAlarm();
});

chrome.storage.onChanged.addListener(async (changes, areaName) => {
  if (areaName === "local" && changes.intervalMinutes) {
    await ensureAlarm();
  }
});

chrome.alarms.onAlarm.addListener(async alarm => {
  if (alarm.name === ALARM_NAME) {
    await runSync("alarm");
  }
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message?.type === "RUN_SYNC_NOW") {
    runSync("manual").then(sendResponse);
    return true;
  }

  if (message?.type === "GET_STATUS") {
    getStatusWithHelper().then(sendResponse);
    return true;
  }
});

async function getStatusWithHelper() {
  const data = await chrome.storage.local.get([
    "lastRunAt",
    "lastStatus",
    "lastMessage",
    "lastDownloadedFile",
    "lastTaskId"
  ]);

  try {
    const response = await fetch("http://127.0.0.1:32145/health", { cache: "no-store" });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const health = await response.json();
    data.helperOnline = Boolean(health.ok);
    data.apiConfigured = Boolean(health.apiConfigured);
    data.helperVersion = health.version || "";
  } catch (_) {
    data.helperOnline = false;
    data.apiConfigured = false;
  }

  return data;
}

async function ensureAlarm() {
  const config = await chrome.storage.local.get(DEFAULTS);
  const minutes = Math.max(60, Number(config.intervalMinutes) || 60);
  await chrome.alarms.clear(ALARM_NAME);
  chrome.alarms.create(ALARM_NAME, {
    periodInMinutes: minutes,
    delayInMinutes: 1
  });
}

async function runSync(source) {
  const startedAt = Date.now();
  const lock = await chrome.storage.local.get(["syncLockAt"]);

  if (lock.syncLockAt && startedAt - lock.syncLockAt < LOCK_TTL_MS) {
    await updateStatus("SKIPPED_LOCKED", "Bỏ qua vì một lần đồng bộ khác đang chạy.");
    return { ok: false, reason: "locked" };
  }

  await chrome.storage.local.set({ syncLockAt: startedAt, lastRunAt: startedAt });
  await updateStatus("STARTING", `Bắt đầu đồng bộ (${source}).`);

  let reportTab = null;
  let exportTab = null;
  let createdReportTab = false;
  let createdExportTab = false;
  const preExistingExportTabIds = new Set();

  try {
    const config = await chrome.storage.local.get(DEFAULTS);

    // 1) Mở/reuse trang quản lý export trước để chụp baseline task hiện có.
    const existingExportTabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/export_management*" });
    existingExportTabs.forEach(t => preExistingExportTabIds.add(t.id));
    exportTab = existingExportTabs[0];
    if (!exportTab) {
      exportTab = await chrome.tabs.create({
        url: config.exportManagementUrl,
        active: Boolean(config.openShopeeTabsActive)
      });
      createdExportTab = true;
    }

    await waitForTabReady(exportTab.id, Number(config.pageReadyDelayMs) || 2500);
    const baseline = await sendWithRetry(exportTab.id, { type: "GET_READY_EXPORTS" }, 8, 1000);

    if (baseline?.status === "LOGIN_REQUIRED") {
      throw new UserFacingError("LOGIN_REQUIRED", "Shopee Affiliate đang yêu cầu đăng nhập lại.");
    }

    const baselineTaskIds = Array.isArray(baseline?.exports)
      ? baseline.exports.map(x => String(x.taskId)).filter(Boolean)
      : [];

    // 2) Mở/reuse Conversion Report. Nếu tab đã tồn tại thì KHÔNG reload để giữ nguyên filter ngày hiện tại.
    const reportTabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/report/conversion_report*" });
    reportTab = reportTabs[0];
    if (!reportTab) {
      reportTab = await chrome.tabs.create({
        url: config.reportUrl,
        active: Boolean(config.openShopeeTabsActive)
      });
      createdReportTab = true;
      await waitForTabReady(reportTab.id, Number(config.pageReadyDelayMs) || 2500);
    } else {
      await waitForTabReady(reportTab.id, 500);
    }

    // 3) Click đúng nút "Xuất dữ liệu" trên UI Shopee. Không tự gọi private API.
    await updateStatus("TRIGGERING_EXPORT", "Đang yêu cầu Shopee tạo báo cáo chuyển đổi.");
    const trigger = await sendWithRetry(reportTab.id, {
      type: "TRIGGER_CONVERSION_EXPORT",
      config: {
        exportButtonSelector: config.exportButtonSelector,
        exportButtonTexts: config.exportButtonTexts
      }
    }, 8, 1000);

    if (trigger?.status === "LOGIN_REQUIRED") {
      throw new UserFacingError("LOGIN_REQUIRED", "Shopee Affiliate đang yêu cầu đăng nhập lại.");
    }
    if (!trigger?.ok) {
      throw new UserFacingError(trigger?.status || "EXPORT_TRIGGER_FAILED", trigger?.message || "Không thể bấm nút Xuất dữ liệu.");
    }

    // 4) Theo dõi tab export_management đã mở từ trước. Trang Shopee tự poll task list.
    await updateStatus("WAITING_EXPORT", "Shopee đang tạo file CSV...");
    const readyExport = await waitForNewReadyExport(exportTab.id, baselineTaskIds, startedAt, config);

    // 5) Click anchor download thật của đúng task mới.
    const beforeDownloads = await getRecentDownloadIds();
    const downloadTriggerAt = Date.now();
    const clickResult = await sendWithRetry(exportTab.id, {
      type: "DOWNLOAD_EXPORT",
      taskId: readyExport.taskId,
      fileName: readyExport.fileName
    }, 5, 800);

    if (!clickResult?.ok) {
      throw new UserFacingError(clickResult?.status || "DOWNLOAD_LINK_NOT_FOUND", clickResult?.message || "Không tìm thấy link tải file của task mới.");
    }

    await updateStatus("DOWNLOADING", `Đang tải ${readyExport.fileName}`);
    const downloaded = await waitForDownloadComplete(
      beforeDownloads,
      downloadTriggerAt,
      readyExport.fileName,
      Math.max(60000, Number(config.exportTimeoutMs) || 180000)
    );

    await chrome.storage.local.set({
      lastDownloadedFile: downloaded.filename,
      lastTaskId: String(readyExport.taskId)
    });

    await updateStatus(
      "DOWNLOAD_COMPLETED",
      `Đã tải ${basename(downloaded.filename)}. Local helper sẽ import sang CatsBack.`
    );

    if (config.notifyOnSuccess) {
      notify("CatsBack Shopee Sync", `Đã tải ${basename(downloaded.filename)}.`);
    }

    // Shopee tự mở thêm export_management khi click Export. Chỉ đóng tab mới do lần chạy này tạo,
    // không đụng tới các tab export_management đã có trước.
    if (!config.keepShopeeTabsOpen) {
      await closeCreatedShopeeTabs({
        reportTabId: reportTab?.id,
        exportTabId: exportTab?.id,
        createdReportTab,
        createdExportTab,
        preExistingExportIds: preExistingExportTabIds
      });
    } else {
      await closeDuplicateExportTabs(exportTab.id, preExistingExportTabIds);
    }

    return {
      ok: true,
      status: "DOWNLOAD_COMPLETED",
      taskId: readyExport.taskId,
      fileName: downloaded.filename
    };
  } catch (error) {
    const status = error?.code || "ERROR";
    const message = error?.message || String(error);
    await updateStatus(status, message);

    if (status === "LOGIN_REQUIRED") {
      notify("Shopee cần đăng nhập", "Phiên Shopee Affiliate đã hết hạn. Hãy đăng nhập lại trong Chrome rồi chạy Đồng bộ ngay.");
    } else {
      notify("CatsBack Shopee Sync lỗi", message);
    }

    return { ok: false, status, error: message };
  } finally {
    await chrome.storage.local.remove("syncLockAt");
  }
}

async function waitForNewReadyExport(tabId, baselineTaskIds, startedAt, config) {
  const baseline = new Set((baselineTaskIds || []).map(String));
  const timeoutMs = Math.max(30000, Number(config.exportTimeoutMs) || 180000);
  const pollMs = Math.max(1000, Number(config.exportPollMs) || 3000);
  const reloadEveryMs = Math.max(0, Number(config.refreshExportPageEveryMs) || 15000);
  const deadline = Date.now() + timeoutMs;
  let lastReloadAt = Date.now();

  while (Date.now() < deadline) {
    try {
      const response = await sendWithRetry(tabId, { type: "GET_READY_EXPORTS" }, 3, 500);
      if (response?.status === "LOGIN_REQUIRED") {
        throw new UserFacingError("LOGIN_REQUIRED", "Shopee Affiliate đang yêu cầu đăng nhập lại.");
      }

      const candidates = (response?.exports || [])
        .filter(x => x && x.taskId && x.fileName)
        .filter(x => !baseline.has(String(x.taskId)))
        .filter(x => /^AffiliateCommissionReport_.*\.csv$/i.test(x.fileName));

      if (candidates.length) {
        candidates.sort((a, b) => numericTaskId(b.taskId) - numericTaskId(a.taskId));
        return candidates[0];
      }
    } catch (error) {
      if (error instanceof UserFacingError) throw error;
      // Trang có thể vừa reload; vòng lặp tiếp theo sẽ thử lại.
    }

    if (reloadEveryMs > 0 && Date.now() - lastReloadAt >= reloadEveryMs) {
      lastReloadAt = Date.now();
      await chrome.tabs.reload(tabId).catch(() => {});
      await waitForTabReady(tabId, 1000).catch(() => {});
    }

    await sleep(pollMs);
  }

  throw new UserFacingError(
    "EXPORT_TIMEOUT",
    "Quá thời gian chờ Shopee tạo file AffiliateCommissionReport."
  );
}

async function waitForDownloadComplete(existingIds, startedAt, expectedFileName, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  const oldIds = new Set(existingIds || []);
  const expectedStem = String(expectedFileName || "").replace(/\.csv$/i, "");
  let candidateId = null;

  while (Date.now() < deadline) {
    const items = await chrome.downloads.search({ orderBy: ["-startTime"], limit: 30 });
    const candidate = items.find(item => {
      if (oldIds.has(item.id)) return false;
      const start = item.startTime ? Date.parse(item.startTime) : 0;
      if (start && start < startedAt - 5000) return false;
      const name = basename(item.filename || "");
      if (!/^AffiliateCommissionReport_.*\.csv$/i.test(name)) return false;
      if (expectedStem && !name.toLowerCase().includes(expectedStem.toLowerCase())) return false;
      return true;
    }) || items.find(item => {
      if (oldIds.has(item.id)) return false;
      const start = item.startTime ? Date.parse(item.startTime) : 0;
      if (start && start < startedAt - 5000) return false;
      return /^AffiliateCommissionReport_.*\.csv$/i.test(basename(item.filename || ""));
    });

    if (candidate) {
      candidateId = candidate.id;
      if (candidate.state === "complete") return candidate;
      if (candidate.state === "interrupted") {
        throw new UserFacingError("DOWNLOAD_INTERRUPTED", `Chrome báo tải file bị gián đoạn: ${candidate.error || "unknown"}`);
      }
    }

    await sleep(1000);
  }

  throw new UserFacingError(
    "DOWNLOAD_TIMEOUT",
    candidateId
      ? "Chrome đã bắt đầu tải nhưng file chưa hoàn tất trong thời gian cho phép."
      : "Không phát hiện file AffiliateCommissionReport mới trong Chrome Downloads."
  );
}

async function getRecentDownloadIds() {
  const items = await chrome.downloads.search({ orderBy: ["-startTime"], limit: 100 });
  return items.map(x => x.id);
}

async function waitForTabReady(tabId, extraDelayMs = 0) {
  const deadline = Date.now() + 45000;
  while (Date.now() < deadline) {
    const tab = await chrome.tabs.get(tabId);
    if (tab.status === "complete") {
      if (extraDelayMs > 0) await sleep(extraDelayMs);
      return tab;
    }
    await sleep(500);
  }
  throw new Error("Trang Shopee tải quá lâu.");
}

async function sendWithRetry(tabId, message, attempts = 6, delayMs = 800) {
  let lastError;
  for (let i = 0; i < attempts; i++) {
    try {
      const response = await chrome.tabs.sendMessage(tabId, message);
      if (response) return response;
    } catch (error) {
      lastError = error;
    }
    await sleep(delayMs);
  }
  throw lastError || new Error("Không thể giao tiếp với trang Shopee.");
}

async function closeDuplicateExportTabs(primaryTabId, preExistingIds) {
  const tabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/export_management*" });
  const ids = tabs
    .filter(t => t.id !== primaryTabId && !preExistingIds.has(t.id))
    .map(t => t.id);
  if (ids.length) await chrome.tabs.remove(ids).catch(() => {});
}

async function closeCreatedShopeeTabs({ reportTabId, exportTabId, createdReportTab, createdExportTab, preExistingExportIds }) {
  const ids = [];
  if (createdReportTab && reportTabId) ids.push(reportTabId);
  if (createdExportTab && exportTabId) ids.push(exportTabId);

  // Shopee có thể tự mở thêm tab export_management sau khi click Export.
  const exportTabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/export_management*" });
  for (const tab of exportTabs) {
    if (!preExistingExportIds.has(tab.id) && tab.id !== exportTabId) ids.push(tab.id);
  }

  const unique = [...new Set(ids)].filter(Boolean);
  if (unique.length) await chrome.tabs.remove(unique).catch(() => {});
}

async function updateStatus(status, message) {
  await chrome.storage.local.set({
    lastStatus: status,
    lastMessage: message,
    lastStatusAt: Date.now()
  });
}

function notify(title, message) {
  chrome.notifications.create({
    type: "basic",
    iconUrl: "icon.png",
    title,
    message
  }).catch(() => {});
}

function numericTaskId(value) {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function basename(value) {
  return String(value || "").replace(/\\/g, "/").split("/").pop();
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

class UserFacingError extends Error {
  constructor(code, message) {
    super(message);
    this.name = "UserFacingError";
    this.code = code;
  }
}
