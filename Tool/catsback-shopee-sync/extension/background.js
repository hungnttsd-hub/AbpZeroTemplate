const DEFAULTS = {
  reportUrl: "https://affiliate.shopee.vn/report/conversion_report",
  exportManagementUrl: "https://affiliate.shopee.vn/export_management",
  intervalMinutes: 60,
  exportButtonSelector: "",
  exportButtonTexts: ["Xuất dữ liệu"],
  pageReadyDelayMs: 2500,
  exportPageOpenTimeoutMs: 7000,
  exportActionConfirmTimeoutMs: 12000,
  exportTimeoutMs: 180000,
  exportPollMs: 3000,
  refreshExportPageEveryMs: 10000,
  downloadSubfolder: "CatsBack",
  openShopeeTabsActive: false,
  notifyOnSuccess: false,
  closeAutoOpenedExportTab: true
};

const ALARM_NAME = "catsback-shopee-hourly-sync";
let activeRunToken = null;
const LEGACY_LOCK_TTL_MS = 7 * 60 * 1000;

chrome.runtime.onInstalled.addListener(async () => {
  const current = await chrome.storage.local.get(DEFAULTS);
  await chrome.storage.local.set(current);
  await chrome.storage.local.remove([
    "autoSetReportDateRange",
    "dateRangeInputSelector",
    "searchButtonTexts",
    "lastReportRange",
    "keepShopeeTabsOpen",
    "syncLockAt",
    "syncLockSource",
    "syncLockToken"
  ]);
  await ensureAlarm();
});

chrome.runtime.onStartup.addListener(async () => {
  await clearPersistedSyncLock();
  await ensureAlarm();
});

chrome.storage.onChanged.addListener(async (changes, areaName) => {
  if (areaName === "local" && changes.intervalMinutes) {
    await ensureAlarm();
  }
});

chrome.alarms.onAlarm.addListener(async alarm => {
  if (alarm.name === ALARM_NAME) await runSync("alarm");
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
  if (message?.type === "CLEAR_STALE_LOCK") {
    clearStaleLockFromUi().then(sendResponse);
    return true;
  }
  if (message?.type === "EXPORT_SHOPEE_SETTLEMENTS") {
    runSettlementCollection("export").then(sendResponse);
    return true;
  }
  if (message?.type === "IMPORT_SHOPEE_SETTLEMENTS") {
    runSettlementCollection("import").then(sendResponse);
    return true;
  }
});

async function getStatusWithHelper() {
  const data = await chrome.storage.local.get([
    "lastRunAt",
    "lastStatus",
    "lastMessage",
    "lastDownloadedFile",
    "lastTaskId",
    "syncLockAt",
    "syncLockSource"
  ]);

  data.isRunning = Boolean(activeRunToken);
  data.lockAgeMs = data.syncLockAt ? Math.max(0, Date.now() - Number(data.syncLockAt)) : 0;

  try {
    const response = await fetch("http://127.0.0.1:32145/health", { cache: "no-store" });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const health = await response.json();
    data.helperOnline = Boolean(health.ok);
    data.apiConfigured = Boolean(health.apiConfigured);
    data.helperVersion = health.version || "";
    data.helperWatchDir = health.watchDir || "";
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
  chrome.alarms.create(ALARM_NAME, { periodInMinutes: minutes, delayInMinutes: 1 });
}

async function runSync(source) {
  const startedAt = Date.now();

  if (activeRunToken) {
    return {
      ok: false,
      reason: "locked",
      status: "SYNC_ALREADY_RUNNING",
      error: "Một lần đồng bộ khác đang chạy. Hãy chờ lần đó hoàn tất."
    };
  }

  const runToken = crypto.randomUUID();
  activeRunToken = runToken;

  const previousLock = await chrome.storage.local.get(["syncLockAt", "syncLockSource"]);
  if (previousLock.syncLockAt) {
    const age = startedAt - Number(previousLock.syncLockAt);
    if (age >= 0 && age < LEGACY_LOCK_TTL_MS) {
      console.warn("Recovering stale persisted sync lock", {
        ageMs: age,
        source: previousLock.syncLockSource || "unknown"
      });
    }
  }

  await chrome.storage.local.set({
    syncLockAt: startedAt,
    syncLockSource: source,
    syncLockToken: runToken,
    lastRunAt: startedAt
  });
  await updateStatus("STARTING", `Bắt đầu đồng bộ (${source}).`);

  let baselineTab = null;
  let baselineTabCreated = false;
  let reportTab = null;
  let reportTabCreated = false;
  let autoOpenedExportTab = null;

  try {
    const config = await chrome.storage.local.get(DEFAULTS);

    // 1) Chụp danh sách file/task hiện có trước khi bấm Xuất dữ liệu.
    await updateStatus("READING_EXPORT_BASELINE", "Đang đọc danh sách báo cáo hiện có để tránh tải nhầm file cũ.");
    const existingExportTabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/export_management*" });
    baselineTab = existingExportTabs[0] || null;
    if (!baselineTab) {
      baselineTab = await chrome.tabs.create({
        url: config.exportManagementUrl,
        active: false
      });
      baselineTabCreated = true;
    }

    await waitForTabReady(baselineTab.id, Number(config.pageReadyDelayMs) || 2500);
    const baselineResponse = await sendWithRetry(baselineTab.id, { type: "GET_EXPORT_ITEMS" }, 8, 800);
    if (baselineResponse?.status === "LOGIN_REQUIRED") {
      throw new UserFacingError("LOGIN_REQUIRED", "Shopee Affiliate đang yêu cầu đăng nhập lại.");
    }

    const baselineTaskIds = new Set((baselineResponse?.exports || []).map(x => String(x.taskId || "")).filter(Boolean));
    const baselineFileNames = new Set((baselineResponse?.exports || []).map(x => String(x.fileName || "")).filter(Boolean));

    // Ghi nhận tất cả tab export_management đã tồn tại TRƯỚC khi click.
    const tabsBeforeClick = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/export_management*" });
    const exportTabIdsBeforeClick = new Set(tabsBeforeClick.map(t => t.id));

    // 2) Reload Conversion Report để Shopee tự áp dụng khoảng thời gian mặc định.
    await updateStatus("LOADING_REPORT", "Đang mở Conversion Report và dùng nguyên khoảng thời gian mặc định của Shopee.");
    const reportTabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/report/conversion_report*" });
    reportTab = reportTabs[0] || null;

    if (!reportTab) {
      reportTab = await chrome.tabs.create({
        url: config.reportUrl,
        active: Boolean(config.openShopeeTabsActive)
      });
      reportTabCreated = true;
      await waitForTabReady(reportTab.id, Number(config.pageReadyDelayMs) || 2500);
    } else {
      if (config.openShopeeTabsActive) {
        await chrome.tabs.update(reportTab.id, { active: true }).catch(() => {});
      }
      await chrome.tabs.reload(reportTab.id);
      await waitForTabReady(reportTab.id, Number(config.pageReadyDelayMs) || 2500);
    }

    // 3) Kích hoạt "Xuất dữ liệu". Khi user bấm Đồng bộ ngay, đưa tab report
    // ra foreground để có thể quan sát trực tiếp. Alarm hàng giờ vẫn chạy background.
    if (source === "manual") {
      await chrome.tabs.update(reportTab.id, { active: true }).catch(() => {});
      if (reportTab.windowId) await chrome.windows.update(reportTab.windowId, { focused: true }).catch(() => {});
      await sleep(250);
    }

    await updateStatus("CLICKING_EXPORT", "Đang kích hoạt đúng 'Xuất dữ liệu' trên Conversion Report.");

    // Ưu tiên click ở MAIN world để handler React/Vue của chính trang nhận event
    // trong cùng execution world. Target là descendant mang exact text, ví dụ HTML
    // hiện tại: <button><a>Xuất dữ liệu</a></button>. Không phụ thuộc CSS class.
    let trigger = await triggerExportInMainWorld(reportTab.id, config).catch(() => null);
    if (!trigger?.ok) {
      // Fallback content script nếu MAIN world bị browser/version chặn.
      trigger = await sendWithRetry(reportTab.id, {
        type: "TRIGGER_CONVERSION_EXPORT",
        config: {
          exportButtonSelector: config.exportButtonSelector,
          exportButtonTexts: config.exportButtonTexts
        }
      }, 8, 800);
    }

    if (trigger?.status === "LOGIN_REQUIRED") {
      throw new UserFacingError("LOGIN_REQUIRED", "Shopee Affiliate đang yêu cầu đăng nhập lại.");
    }
    if (!trigger?.ok) {
      throw new UserFacingError(trigger?.status || "EXPORT_TRIGGER_FAILED", trigger?.message || "Không thể bấm nút Xuất dữ liệu.");
    }

    await updateStatus(
      "EXPORT_CLICKED",
      `Đã click ${trigger?.debug || "nút Xuất dữ liệu"}.`
    );

    // Shopee thường tự mở tab /export_management. Synthetic click của extension có thể
    // bị Chrome chặn popup, nên chờ ngắn; nếu tab mới không xuất hiện thì dùng chính
    // baseline Export Management đã mở sẵn và reload để xem task mới.
    await updateStatus("WAITING_EXPORT_PAGE", "Đang chờ trang Quản lý xuất dữ liệu của Shopee...");
    autoOpenedExportTab = await waitForNewExportManagementTab(
      exportTabIdsBeforeClick,
      Math.max(2000, Number(config.exportPageOpenTimeoutMs) || 5000),
      true
    );

    let exportWorkTab = autoOpenedExportTab;
    if (exportWorkTab) {
      await waitForTabReady(exportWorkTab.id, Number(config.pageReadyDelayMs) || 2500);
      await updateStatus("EXPORT_PAGE_OPENED", "Shopee đã mở/điều hướng tới Quản lý xuất dữ liệu. Đã xác nhận thao tác Export có hiệu lực.");
    } else {
      // Không được coi baseline page là bằng chứng click thành công. v0.6.2 từng
      // fallback ngay và gây WAITING_EXPORT_FILE giả dù Export chưa kích hoạt.
      await updateStatus("VERIFYING_EXPORT_ACTION", "Chưa thấy Shopee mở Quản lý xuất dữ liệu; đang kiểm tra xem task mới có thực sự được tạo hay không...");
      const quickNew = await waitForNewExportTaskQuick(
        baselineTab.id,
        baselineTaskIds,
        baselineFileNames,
        Math.max(5000, Number(config.exportActionConfirmTimeoutMs) || 12000)
      );
      if (!quickNew) {
        throw new UserFacingError(
          "EXPORT_CLICK_NOT_CONFIRMED",
          `Đã dispatch click nhưng Shopee không mở /export_management và không xuất hiện task mới. ${trigger?.debug || ""}`.trim()
        );
      }
      exportWorkTab = baselineTab;
      await updateStatus(
        "EXPORT_ACTION_CONFIRMED",
        `Đã xác nhận Shopee tạo task mới ${quickNew.taskId || quickNew.fileName || ""}; tiếp tục chờ file sẵn sàng.`
      );
    }

    // 4) Chờ report mới theo business identity:
    // filename AffiliateCommissionReport_YYYYMMDDHHmm.csv + download URL có task_id.
    // Không phụ thuộc class của item/progress/icon.
    const readyExport = await waitForNewReadyExport(
      exportWorkTab.id,
      baselineTaskIds,
      baselineFileNames,
      config
    );

    await updateStatus(
      "EXPORT_FILE_READY",
      `Shopee đã tạo ${readyExport.fileName} (task ${readyExport.taskId}).`
    );

    // 5) Lấy href theo task_id từ business download URL và dùng Chrome Downloads
    // để ép file vào Downloads/CatsBack. Không cần nhận diện icon.
    const linkResult = await sendWithRetry(exportWorkTab.id, {
      type: "GET_DOWNLOAD_LINK",
      taskId: readyExport.taskId,
      fileName: readyExport.fileName
    }, 5, 800);

    if (!linkResult?.ok || !linkResult?.href) {
      throw new UserFacingError(
        linkResult?.status || "DOWNLOAD_LINK_NOT_FOUND",
        linkResult?.message || "Không tìm thấy link tải file của task mới."
      );
    }

    const relativeDownloadPath = buildDownloadRelativePath(config.downloadSubfolder, readyExport.fileName);
    await updateStatus("DOWNLOADING", `Đang tải ${relativeDownloadPath}`);

    let downloadId;
    try {
      downloadId = await chrome.downloads.download({
        url: linkResult.href,
        filename: relativeDownloadPath,
        conflictAction: "uniquify",
        saveAs: false
      });
    } catch (error) {
      throw new UserFacingError(
        "DOWNLOAD_START_FAILED",
        `Chrome không thể bắt đầu tải file vào ${relativeDownloadPath}: ${error?.message || error}`
      );
    }

    const downloaded = await waitForDownloadIdComplete(
      downloadId,
      Math.max(60000, Number(config.exportTimeoutMs) || 180000)
    );

    await chrome.storage.local.set({
      lastDownloadedFile: downloaded.filename,
      lastTaskId: String(readyExport.taskId)
    });

    await updateStatus(
      "DOWNLOAD_COMPLETED",
      `Đã tải ${basename(downloaded.filename)}. Local Helper sẽ tự import sang CatsBack.`
    );

    if (config.notifyOnSuccess) {
      notify("CatsBack Shopee Sync", `Đã tải ${basename(downloaded.filename)}.`);
    }

    // Không để mỗi giờ tích thêm một tab export_management do Shopee tự mở.
    if (config.closeAutoOpenedExportTab && autoOpenedExportTab?.id) {
      await chrome.tabs.remove(autoOpenedExportTab.id).catch(() => {});
      autoOpenedExportTab = null;
    }

    if (baselineTabCreated && baselineTab?.id) {
      await chrome.tabs.remove(baselineTab.id).catch(() => {});
      baselineTab = null;
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
    // Chỉ đóng tab baseline do tool tự tạo. Tab report của user được giữ nguyên.
    if (baselineTabCreated && baselineTab?.id) {
      await chrome.tabs.remove(baselineTab.id).catch(() => {});
    }

    if (activeRunToken === runToken) activeRunToken = null;
    const stored = await chrome.storage.local.get(["syncLockToken"]);
    if (!stored.syncLockToken || stored.syncLockToken === runToken) {
      await clearPersistedSyncLock();
    }
  }
}

async function triggerExportInMainWorld(tabId, config) {
  const labels = (Array.isArray(config?.exportButtonTexts) && config.exportButtonTexts.length
    ? config.exportButtonTexts
    : ["Xuất dữ liệu"]).map(String);

  const results = await chrome.scripting.executeScript({
    target: { tabId },
    world: "MAIN",
    args: [labels],
    func: labelsArg => {
      const normalize = value => String(value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .trim()
        .toLowerCase()
        .replace(/\s+/g, " ");
      const expected = new Set((labelsArg || []).map(normalize).filter(Boolean));
      const visible = el => {
        if (!el) return false;
        const style = getComputedStyle(el);
        const rect = el.getBoundingClientRect();
        return style.display !== "none" && style.visibility !== "hidden" && Number(style.opacity) !== 0 && rect.width > 0 && rect.height > 0;
      };
      const disabled = el => Boolean(el?.disabled || el?.getAttribute?.("aria-disabled") === "true" || el?.hasAttribute?.("disabled"));
      const describe = el => {
        if (!el) return "unknown";
        const tag = String(el.tagName || "").toLowerCase();
        const text = String(el.innerText || el.textContent || el.value || "").replace(/\s+/g, " ").trim().slice(0, 100);
        return `${tag} text="${text}"`;
      };

      const candidates = [];
      for (const el of document.querySelectorAll('button, [role="button"], a, input[type="button"], input[type="submit"]')) {
        if (!visible(el) || disabled(el)) continue;
        const text = normalize(el.innerText || el.textContent || el.value || "");
        const aria = normalize(el.getAttribute?.("aria-label") || "");
        if (!expected.has(text) && !expected.has(aria)) continue;
        let score = 0;
        if (expected.has(text)) score += 120;
        if (expected.has(aria)) score += 90;
        const tag = String(el.tagName || "").toLowerCase();
        if (tag === "button") score += 35;
        else if (el.getAttribute?.("role") === "button") score += 30;
        else if (tag === "a") score += 20;
        score += 40;
        candidates.push({ el, score });
      }
      candidates.sort((a, b) => b.score - a.score);
      if (!candidates.length) return { ok: false, status: "EXPORT_BUTTON_NOT_FOUND", message: "MAIN world không tìm thấy Xuất dữ liệu." };
      if (candidates.length > 1 && candidates[0].score - candidates[1].score <= 5) {
        return { ok: false, status: "EXPORT_BUTTON_AMBIGUOUS", message: "MAIN world thấy nhiều candidate ngang điểm." };
      }

      const control = candidates[0].el;
      // Với DOM hiện tại <button><a>Xuất dữ liệu</a></button>, click <a> mới
      // giống vị trí người dùng thực sự bấm và đảm bảo listener ở descendant nhận event.
      const descendants = Array.from(control.querySelectorAll?.('a, span, strong, em, div') || [])
        .filter(el => visible(el) && expected.has(normalize(el.textContent || el.innerText || "")))
        .map(el => ({ el, score: (String(el.tagName).toLowerCase() === "a" ? 100 : 50) + ((el.children?.length || 0) === 0 ? 30 : 0) }))
        .sort((a, b) => b.score - a.score);
      let target = descendants[0]?.el || null;
      let strategy = target ? "exact-label-descendant" : "semantic-control";
      if (!target) {
        const rect = control.getBoundingClientRect();
        const hit = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2);
        if (hit && control.contains(hit) && visible(hit)) {
          target = hit;
          strategy = "center-hit-test";
        }
      }
      target ||= control;
      target.scrollIntoView({ block: "center", inline: "center", behavior: "auto" });
      try { target.focus?.({ preventScroll: true }); } catch (_) {}
      const init = { bubbles: true, cancelable: true, composed: true, view: window };
      try { target.dispatchEvent(new PointerEvent("pointerdown", { ...init, pointerId: 1, pointerType: "mouse", isPrimary: true, button: 0, buttons: 1 })); } catch (_) {}
      try { target.dispatchEvent(new MouseEvent("mousedown", { ...init, button: 0, buttons: 1 })); } catch (_) {}
      try { target.dispatchEvent(new PointerEvent("pointerup", { ...init, pointerId: 1, pointerType: "mouse", isPrimary: true, button: 0, buttons: 0 })); } catch (_) {}
      try { target.dispatchEvent(new MouseEvent("mouseup", { ...init, button: 0, buttons: 0 })); } catch (_) {}
      try { target.click(); } catch (error) {
        return { ok: false, status: "EXPORT_CLICK_EXCEPTION", message: error?.message || String(error) };
      }
      return {
        ok: true,
        status: "EXPORT_BUTTON_CLICKED",
        message: "MAIN world đã kích hoạt Xuất dữ liệu.",
        debug: `control=${describe(control)}; target=${describe(target)}; strategy=${strategy}`
      };
    }
  });
  return results?.[0]?.result || { ok: false, status: "MAIN_WORLD_NO_RESULT" };
}

async function waitForNewExportTaskQuick(tabId, baselineTaskIds, baselineFileNames, timeoutMs) {
  const baselineTasks = new Set(Array.from(baselineTaskIds || []).map(String).filter(Boolean));
  const baselineFiles = new Set(Array.from(baselineFileNames || []).map(String).filter(Boolean));
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    await chrome.tabs.reload(tabId).catch(() => {});
    await waitForTabReady(tabId, 700).catch(() => {});
    try {
      const response = await sendWithRetry(tabId, { type: "GET_EXPORT_ITEMS" }, 3, 400);
      const items = (response?.exports || []).filter(x => x && x.fileName && /^AffiliateCommissionReport_\d{12}\.csv$/i.test(x.fileName));
      const fresh = items.filter(x => {
        const taskId = String(x.taskId || "");
        const fileName = String(x.fileName || "");
        return taskId ? !baselineTasks.has(taskId) : (fileName && !baselineFiles.has(fileName));
      });
      if (fresh.length) {
        fresh.sort(compareExportsNewestFirst);
        return fresh[0];
      }
    } catch (_) {}
    await sleep(1200);
  }
  return null;
}

async function waitForNewExportManagementTab(existingIds, timeoutMs, returnNullOnTimeout = false) {
  const before = new Set(existingIds || []);
  const deadline = Date.now() + timeoutMs;
  let settled = false;
  let resolvePromise;
  let rejectPromise;

  const promise = new Promise((resolve, reject) => {
    resolvePromise = resolve;
    rejectPromise = reject;
  });

  const isExportUrl = url => String(url || "").startsWith("https://affiliate.shopee.vn/export_management");

  const cleanup = () => {
    try { chrome.tabs.onCreated.removeListener(onCreated); } catch (_) {}
    try { chrome.tabs.onUpdated.removeListener(onUpdated); } catch (_) {}
  };

  const resolveOnce = tab => {
    if (settled || !tab?.id || before.has(tab.id)) return;
    if (!isExportUrl(tab.url) && !isExportUrl(tab.pendingUrl)) return;
    settled = true;
    cleanup();
    resolvePromise(tab);
  };

  const onCreated = tab => resolveOnce(tab);
  const onUpdated = async (tabId, changeInfo, tab) => {
    if (before.has(tabId)) return;
    if (isExportUrl(changeInfo.url) || isExportUrl(tab?.url) || isExportUrl(tab?.pendingUrl)) {
      resolveOnce(tab || await chrome.tabs.get(tabId).catch(() => null));
    }
  };

  chrome.tabs.onCreated.addListener(onCreated);
  chrome.tabs.onUpdated.addListener(onUpdated);

  // Poll song song để tránh bỏ lỡ event nếu tab được mở đúng lúc listener khởi tạo.
  (async () => {
    while (!settled && Date.now() < deadline) {
      const tabs = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/export_management*" }).catch(() => []);
      const candidate = tabs.find(t => !before.has(t.id));
      if (candidate) {
        resolveOnce(candidate);
        return;
      }
      await sleep(250);
    }

    if (!settled) {
      settled = true;
      cleanup();
      if (returnNullOnTimeout) {
        resolvePromise(null);
      } else {
        rejectPromise(new UserFacingError(
          "EXPORT_PAGE_NOT_OPENED",
          "Đã click nút 'Xuất dữ liệu' nhưng Shopee không mở tab Quản lý xuất dữ liệu trong thời gian cho phép."
        ));
      }
    }
  })();

  return promise;
}

async function waitForNewReadyExport(tabId, baselineTaskIds, baselineFileNames, config) {
  const baselineTasks = new Set(Array.from(baselineTaskIds || []).map(String).filter(Boolean));
  const baselineFiles = new Set(Array.from(baselineFileNames || []).map(String).filter(Boolean));
  const timeoutMs = Math.max(30000, Number(config.exportTimeoutMs) || 180000);
  const pollMs = Math.max(1000, Number(config.exportPollMs) || 3000);
  const reloadEveryMs = Math.max(0, Number(config.refreshExportPageEveryMs) || 10000);
  const deadline = Date.now() + timeoutMs;
  let lastReloadAt = Date.now();
  let lastSeenNewFile = "";
  let lastDiagnosticAt = 0;
  let lastDiagnosticSignature = "";

  await updateStatus(
    "WAITING_EXPORT_FILE",
    `Đang chờ report mới. Baseline: ${baselineTasks.size} task / ${baselineFiles.size} file. Nhận diện task_id là khóa chính; không lọc theo timestamp filename.`
  );

  while (Date.now() < deadline) {
    try {
      const response = await sendWithRetry(tabId, { type: "GET_EXPORT_ITEMS" }, 3, 500);
      if (response?.status === "LOGIN_REQUIRED") {
        throw new UserFacingError("LOGIN_REQUIRED", "Shopee Affiliate đang yêu cầu đăng nhập lại.");
      }

      const all = (response?.exports || [])
        .filter(x => x && x.fileName)
        .filter(x => /^AffiliateCommissionReport_\d{12}\.csv$/i.test(x.fileName));

      // task_id là business identity mạnh nhất. Nếu item có task_id, chỉ so với
      // baseline task_id. Không loại item chỉ vì filename trùng: Shopee filename
      // chỉ có độ chính xác tới phút nên 2 task khác nhau có thể cùng filename.
      // Nếu item chưa có task_id thì mới fallback sang filename.
      const newItems = all.filter(x => {
        const taskId = String(x.taskId || "");
        const fileName = String(x.fileName || "");
        if (taskId) return !baselineTasks.has(taskId);
        return fileName && !baselineFiles.has(fileName);
      });

      if (newItems.length) {
        newItems.sort(compareExportsNewestFirst);
        const newest = newItems[0];
        if (newest.fileName !== lastSeenNewFile || newest.ready) {
          lastSeenNewFile = newest.fileName;
          await updateStatus(
            newest.ready ? "EXPORT_FILE_READY" : "WAITING_EXPORT_FILE",
            newest.ready
              ? `Đã thấy report mới ${newest.fileName} (task ${newest.taskId || "chưa có"}) có download link.`
              : `Đã thấy report mới ${newest.fileName} (task ${newest.taskId || "chưa có"}); Shopee vẫn đang tạo file...`
          );
        }
        if (newest.ready && newest.taskId && newest.href) return newest;
      }

      // Diagnostic định kỳ để tránh trạng thái treo mà không biết DOM đang thấy gì.
      // Chỉ update khi snapshot thay đổi hoặc sau 15 giây.
      const latestVisible = all.slice().sort(compareExportsNewestFirst)[0] || null;
      const readyCount = all.filter(x => x.ready && x.taskId && x.href).length;
      const signature = [
        all.length,
        newItems.length,
        readyCount,
        latestVisible?.taskId || "",
        latestVisible?.fileName || "",
        latestVisible?.ready ? "1" : "0"
      ].join("|");
      if (signature !== lastDiagnosticSignature || Date.now() - lastDiagnosticAt >= 15000) {
        lastDiagnosticSignature = signature;
        lastDiagnosticAt = Date.now();
        const latestText = latestVisible
          ? `${latestVisible.fileName} / task ${latestVisible.taskId || "?"} / ${latestVisible.ready ? "ready" : "processing"}`
          : "không có AffiliateCommissionReport trong DOM";
        await updateStatus(
          "WAITING_EXPORT_FILE",
          `Đang chờ report mới. DOM: ${all.length} report, ${readyCount} ready, ${newItems.length} item mới. Mới nhất: ${latestText}. Baseline: ${baselineTasks.size} task / ${baselineFiles.size} file.`
        );
      }
    } catch (error) {
      if (error instanceof UserFacingError) throw error;
      // Nếu tab vừa reload/content script chưa sẵn sàng, vòng sau sẽ thử lại.
    }

    if (reloadEveryMs > 0 && Date.now() - lastReloadAt >= reloadEveryMs) {
      lastReloadAt = Date.now();
      await chrome.tabs.reload(tabId).catch(() => {});
      await waitForTabReady(tabId, 800).catch(() => {});
    }

    await sleep(pollMs);
  }

  throw new UserFacingError(
    "EXPORT_TIMEOUT",
    lastSeenNewFile
      ? `Quá thời gian chờ ${lastSeenNewFile} xuất hiện download link.`
      : `Quá thời gian nhưng không thấy task AffiliateCommissionReport mới. Baseline: ${baselineTasks.size} task / ${baselineFiles.size} file.`
  );
}

function compareExportsNewestFirst(a, b) {
  const at = parseReportFileTime(a.fileName) || 0;
  const bt = parseReportFileTime(b.fileName) || 0;
  if (bt !== at) return bt - at;
  return numericTaskId(b.taskId) - numericTaskId(a.taskId);
}

function parseReportFileTime(fileName) {
  const m = String(fileName || "").match(/^AffiliateCommissionReport_(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})\.csv$/i);
  if (!m) return 0;
  const [, y, mo, d, h, mi] = m;
  const dt = new Date(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), 0, 0);
  const ts = dt.getTime();
  return Number.isFinite(ts) ? ts : 0;
}

function buildDownloadRelativePath(subfolder, fileName) {
  const safeFileName = basename(fileName).replace(/[<>:"|?*]/g, "_");
  const parts = String(subfolder || "")
    .replace(/\\/g, "/")
    .split("/")
    .map(x => x.trim())
    .filter(x => x && x !== "." && x !== "..")
    .map(x => x.replace(/[<>:"|?*]/g, "_"));
  return parts.length ? `${parts.join("/")}/${safeFileName}` : safeFileName;
}

async function waitForDownloadIdComplete(downloadId, timeoutMs) {
  if (!Number.isInteger(downloadId)) {
    throw new UserFacingError("DOWNLOAD_START_FAILED", "Chrome không trả về downloadId hợp lệ.");
  }
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const items = await chrome.downloads.search({ id: downloadId });
    const item = items[0];
    if (item?.state === "complete") return item;
    if (item?.state === "interrupted") {
      throw new UserFacingError("DOWNLOAD_INTERRUPTED", `Chrome báo tải file bị gián đoạn: ${item.error || "unknown"}`);
    }
    await sleep(1000);
  }
  throw new UserFacingError("DOWNLOAD_TIMEOUT", "Chrome đã bắt đầu tải nhưng file chưa hoàn tất trong thời gian cho phép.");
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
  throw new UserFacingError("PAGE_LOAD_TIMEOUT", "Trang Shopee tải quá lâu.");
}

async function sendWithRetry(tabId, message, attempts = 6, delayMs = 800) {
  let lastError;
  let injected = false;

  for (let i = 0; i < attempts; i++) {
    try {
      const response = await chrome.tabs.sendMessage(tabId, message);
      if (response) return response;
    } catch (error) {
      lastError = error;
      const text = String(error?.message || error || "");
      if (!injected && /Receiving end does not exist|Could not establish connection/i.test(text)) {
        try {
          const tab = await chrome.tabs.get(tabId);
          if (tab?.url?.startsWith("https://affiliate.shopee.vn/")) {
            await chrome.scripting.executeScript({ target: { tabId }, files: ["content.js"] });
            injected = true;
            await sleep(250);
            continue;
          }
        } catch (injectError) {
          lastError = injectError;
        }
      }
    }
    await sleep(delayMs);
  }

  const messageText = String(lastError?.message || lastError || "");
  if (/Receiving end does not exist|Could not establish connection/i.test(messageText)) {
    throw new UserFacingError(
      "CONTENT_SCRIPT_NOT_READY",
      "Không kết nối được với tab Shopee. Hãy refresh tab affiliate.shopee.vn một lần rồi bấm Đồng bộ ngay."
    );
  }
  throw lastError || new Error("Không thể giao tiếp với trang Shopee.");
}

async function clearPersistedSyncLock() {
  await chrome.storage.local.remove(["syncLockAt", "syncLockSource", "syncLockToken"]);
}

async function clearStaleLockFromUi() {
  if (activeRunToken) {
    return { ok: false, reason: "active", message: "Đang có một lần đồng bộ thực sự chạy. Không mở khóa cưỡng bức." };
  }
  await clearPersistedSyncLock();
  return { ok: true, message: "Đã xóa khóa đồng bộ cũ." };
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

const SHOPEE_BILLING_URL = "https://affiliate.shopee.vn/payment/billing";
const SETTLEMENT_HELPER_BASE_URL = "http://127.0.0.1:32145";

async function runSettlementCollection(mode) {
  const action = mode === "import" ? "import" : "export";
  try {
    const healthResponse = await fetch(`${SETTLEMENT_HELPER_BASE_URL}/health`, { cache: "no-store" });
    if (!healthResponse.ok) throw new Error(`Local Helper trả về HTTP ${healthResponse.status}.`);
    const health = await healthResponse.json();
    if (!health?.ok) throw new Error("Local Helper chưa sẵn sàng.");
    if (action === "import" && !health.apiConfigured) {
      throw new Error("Local Helper chưa được cấu hình Client ID/Client Secret CatsBack.");
    }

    const tab = await ensureShopeeBillingTab();
    const execution = await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      world: "MAIN",
      func: collectShopeeSettlementRowsInPage
    });
    const report = execution?.[0]?.result;
    if (!report?.ok) throw new Error(report?.error || "Shopee không trả về dữ liệu đối soát hợp lệ.");
    if (!Array.isArray(report.rows) || report.rows.length === 0) {
      const requestInfo = report.billingListRequestUrl ? ` Request: ${report.billingListRequestUrl}` : "";
      throw new Error(
        `Không tìm thấy bảng kê nào có dữ liệu đơn hàng trong response Billing của Shopee ` +
        `(list=${report.billingListCount || 0}, candidates=${report.candidateCount || 0}).${requestInfo}`
      );
    }

    const helperResponse = await fetch(`${SETTLEMENT_HELPER_BASE_URL}/api/settlements/${action}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        schemaVersion: report.schemaVersion,
        validationCount: report.validationCount,
        rows: report.rows
      })
    });
    const helperPayload = await helperResponse.json().catch(() => ({}));
    if (!helperResponse.ok || !helperPayload?.ok) {
      throw new Error(helperPayload?.error || `Local Helper trả về HTTP ${helperResponse.status}.`);
    }

    const result = {
      ok: true,
      validationCount: report.validationCount,
      rowCount: report.rows.length,
      filePath: helperPayload.filePath || "",
      importSummary: helperPayload.importSummary || ""
    };
    await chrome.storage.local.set({
      lastSettlementRunAt: new Date().toISOString(),
      lastSettlementStatus: action === "import" ? "IMPORTED" : "EXPORTED",
      lastSettlementMessage: `${result.validationCount} validation / ${result.rowCount} orders`,
      lastSettlementFile: result.filePath
    });
    return result;
  } catch (error) {
    const message = error?.message || String(error);
    await chrome.storage.local.set({
      lastSettlementRunAt: new Date().toISOString(),
      lastSettlementStatus: "FAILED",
      lastSettlementMessage: message
    });
    return { ok: false, error: message };
  }
}

async function ensureShopeeBillingTab() {
  const candidates = await chrome.tabs.query({ url: "https://affiliate.shopee.vn/payment/billing*" });
  let tab = candidates.find(candidate => {
    try {
      const url = new URL(candidate.url || "");
      return url.pathname.replace(/\/$/, "") === "/payment/billing";
    } catch (_) {
      return false;
    }
  });

  if (!tab) {
    tab = await chrome.tabs.create({ url: SHOPEE_BILLING_URL, active: false });
  } else if (tab.url !== SHOPEE_BILLING_URL) {
    tab = await chrome.tabs.update(tab.id, { url: SHOPEE_BILLING_URL });
  } else {
    await chrome.tabs.reload(tab.id);
    await sleep(250);
  }

  await waitForTabReady(tab.id, 1200);
  const refreshed = await chrome.tabs.get(tab.id);
  if (!String(refreshed.url || "").startsWith("https://affiliate.shopee.vn/")) {
    throw new Error("Không mở được trang danh sách đối soát Shopee.");
  }
  return refreshed;
}

// Hàm này được inject vào MAIN world của trang billing. Danh sách bảng kê được lấy
// từ response billing_list do chính trang Shopee đã gọi; chỉ các API chi tiết mới
// được gọi bổ sung bằng cookie/CSRF/session của tab hiện tại.
async function collectShopeeSettlementRowsInPage() {
  const SCHEMA_VERSION = "catsback-settlement-v2";
  const MONEY_SCALE = 100000;
  const OUTPUT_SCALE = 10000;
  const MAX_VALIDATIONS = 2000;
  const MAX_CHECKOUTS_PER_BILL = 10000;
  const PAGE_SIZE = 100;

  try {
    if (location.hostname !== "affiliate.shopee.vn") {
      throw new Error("Tab hiện tại không thuộc affiliate.shopee.vn.");
    }

    const capture = await waitForBillingListNetworkCapture(15000);
    const payload = capture.payload;
    if (Number(payload?.code ?? -1) !== 0) {
      throw new Error(`Shopee API billing_list lỗi: ${String(payload?.msg || payload?.message || payload?.code || "unknown")}`);
    }

    const list = Array.isArray(payload?.data?.list) ? payload.data.list : [];
    const candidates = flattenBillingList(list);
    const validations = new Map();

    for (const item of candidates) {
      const validationId = String(item?.validation_id ?? "").trim();
      if (!/^\d+$/.test(validationId)) continue;
      const existing = validations.get(validationId);
      if (!existing || summaryTimestamp(item) >= summaryTimestamp(existing)) {
        validations.set(validationId, item);
      }
    }

    if (validations.size > MAX_VALIDATIONS) {
      throw new Error(`Có ${validations.size} bảng kê, vượt giới hạn an toàn ${MAX_VALIDATIONS}.`);
    }

    const validationSummaries = [...validations.values()].sort((left, right) =>
      summaryTimestamp(left) - summaryTimestamp(right));
    const rows = [];

    for (const summary of validationSummaries) {
      const validationId = String(summary.validation_id);
      const payload = await apiGet("/api/v3/payment/billing_detail", { validation_id: validationId });
      const bill = payload?.data;
      if (!bill || String(bill.validation_id ?? "") !== validationId) {
        throw new Error(`Bảng kê ${validationId}: Shopee trả về sai validation_id.`);
      }
      const payoutId = String(summary.payout_id ?? bill.payout_id ?? "").trim();
      const sourceAffiliateId = String(bill.affiliate_id ?? "").trim();
      if (!sourceAffiliateId) throw new Error(`Bảng kê ${validationId}: thiếu affiliate_id.`);
      const paymentStatus = rawNumber(summary.payment_status ?? bill.payment_status ?? -1, "payment_status");
      const validationPayoutStatus = rawNumber(
        summary.validation_payout_status ?? bill.validation_payout_status ?? -1,
        "validation_payout_status"
      );
      const overallValidationStatus = optionalSafeInteger(
        summary.overall_validation_status ?? bill.overall_validation_status
      );
      const billValidationStatus = optionalSafeInteger(summary.validation_status ?? bill.validation_status);
      const settlementCycle = optionalSafeInteger(summary.settlement_cycle ?? bill.settlement_cycle);

      const paymentValidation = bill.payment_validation || {};
      const commissionSettlements = Array.isArray(paymentValidation.commission_settlement_list)
        ? paymentValidation.commission_settlement_list
        : [];
      const hasAdjustment = Boolean(bill.is_adjusted) || nonZero(bill.payment_shopee_adjustment) ||
        (Array.isArray(bill.payment_shopee_adjustment_list) && bill.payment_shopee_adjustment_list.length > 0) ||
        commissionSettlements.some(settlement => nonZero(settlement?.adjustment));
      const hasClawback = nonZero(bill.clawback_shopee_commission) || nonZero(bill.clawback_seller_commission) ||
        (Array.isArray(paymentValidation.clawback_settlement_list) && paymentValidation.clawback_settlement_list.length > 0);
      const hasBonus = Array.isArray(paymentValidation.bonus_settlement_list) &&
        paymentValidation.bonus_settlement_list.length > 0;
      const hasPpp = nonZero(bill.bill_ppp_amount) ||
        (Array.isArray(paymentValidation.ppp_settlement_list) && paymentValidation.ppp_settlement_list.length > 0);
      const cumulativeStatus = rawNumber(bill?.payment_confirmation?.cumulative_status ?? 0, "cumulative_status");
      const isCumulative = cumulativeStatus !== 0;
      const completedFrom = positiveTimestamp(bill.order_completed_period_start_time,
        `Bảng kê ${validationId}: thiếu ngày bắt đầu hoàn thành đơn.`);
      const completedTo = positiveTimestamp(bill.order_completed_period_end_time,
        `Bảng kê ${validationId}: thiếu ngày kết thúc hoàn thành đơn.`);
      const paidAt = optionalPositiveTimestamp(summary.payment_completed_time ?? bill.payment_completed_time);
      if (completedTo < completedFrom) throw new Error(`Bảng kê ${validationId}: khoảng ngày hoàn thành đơn không hợp lệ.`);

      const eligibleRaw = moneyRaw(bill.eligible_total_commission_amount, "eligible_total_commission_amount");
      const afterServiceRaw = moneyRaw(bill.bill_commission_amount, "bill_commission_amount");
      const providerPaidRaw = moneyRaw(bill.payable_total_commission_amount, "payable_total_commission_amount");
      const providerPaymentCompleted = paymentStatus === 4 && validationPayoutStatus === 2 && paidAt !== null;
      // Shopee trả payable_total_commission_amount = 0 khi bill còn Pending. Số 0 đó chưa phải
      // tiền sau thuế, vì vậy không được biến toàn bộ bill_commission_amount thành thuế.
      const taxRaw = providerPaymentCompleted ? afterServiceRaw - providerPaidRaw : 0;
      const settlementRaw = afterServiceRaw - taxRaw;
      if (eligibleRaw < afterServiceRaw || taxRaw < 0 || settlementRaw < 0) {
        throw new Error(`Bảng kê ${validationId}: tổng tiền sau phí hoặc sau thuế không hợp lệ.`);
      }

      const checkouts = await getValidationCheckouts(completedFrom, completedTo, validationId);
      if (checkouts.length > MAX_CHECKOUTS_PER_BILL) {
        throw new Error(`Bảng kê ${validationId}: vượt giới hạn ${MAX_CHECKOUTS_PER_BILL} checkout.`);
      }
      const orderWeights = buildOrderWeights(checkouts, sourceAffiliateId, validationId);
      const sourceEligibleRaw = orderWeights.reduce((sum, order) => sum + order.weightRaw, 0);
      const rawTolerance = Math.max(MONEY_SCALE, Math.abs(eligibleRaw) * 0.0001);
      if (Math.abs(sourceEligibleRaw - eligibleRaw) > rawTolerance) {
        throw new Error(`Bảng kê ${validationId}: tổng hoa hồng đơn không khớp tổng hợp lệ của Shopee.`);
      }
      if (eligibleRaw > 0 && sourceEligibleRaw <= 0) {
        throw new Error(`Bảng kê ${validationId}: không tìm thấy đơn có hoa hồng để phân bổ.`);
      }

      const eligibleUnits = rawToOutputUnits(eligibleRaw);
      const afterServiceUnits = rawToOutputUnits(afterServiceRaw);
      const paidUnits = rawToOutputUnits(settlementRaw);
      const orderEligibleUnits = allocateUnits(eligibleUnits, orderWeights.map(order => order.weightRaw));
      const feeUnits = allocateUnits(eligibleUnits - afterServiceUnits, orderEligibleUnits, orderEligibleUnits);
      const afterFeeCapacities = orderEligibleUnits.map((value, index) => value - feeUnits[index]);
      const taxUnits = allocateUnits(afterServiceUnits - paidUnits, afterFeeCapacities, afterFeeCapacities);

      for (let index = 0; index < orderWeights.length; index++) {
        const orderEligible = orderEligibleUnits[index];
        const serviceFee = feeUnits[index];
        const tax = taxUnits[index];
        const actualPaid = orderEligible - serviceFee - tax;
        if (actualPaid < 0) throw new Error(`Bảng kê ${validationId}: phân bổ âm cho đơn ${orderWeights[index].orderId}.`);
        rows.push({
          schema_version: SCHEMA_VERSION,
          source_affiliate_id: sourceAffiliateId,
          validation_id: validationId,
          payout_id: payoutId,
          payment_completed_at_utc: paidAt ? timestampIso(paidAt) : "",
          order_completed_from_utc: timestampIso(completedFrom),
          order_completed_to_utc: timestampIso(completedTo),
          payment_status: String(paymentStatus),
          validation_payout_status: String(validationPayoutStatus),
          overall_validation_status: overallValidationStatus === null ? "" : String(overallValidationStatus),
          bill_validation_status: billValidationStatus === null ? "" : String(billValidationStatus),
          settlement_cycle: settlementCycle === null ? "" : String(settlementCycle),
          has_adjustment: String(hasAdjustment),
          has_clawback: String(hasClawback),
          is_cumulative: String(isCumulative),
          has_bonus: String(hasBonus),
          has_ppp: String(hasPpp),
          bill_eligible_commission: formatUnits(eligibleUnits),
          bill_after_service_fee: formatUnits(afterServiceUnits),
          bill_paid_commission: formatUnits(paidUnits),
          order_id: orderWeights[index].orderId,
          order_eligible_commission: formatUnits(orderEligible),
          allocated_service_fee: formatUnits(serviceFee),
          allocated_tax: formatUnits(tax),
          actual_paid_commission: formatUnits(actualPaid)
        });
      }
    }

    return {
      ok: true,
      schemaVersion: SCHEMA_VERSION,
      validationCount: validationSummaries.length,
      billingListRequestUrl: capture.url,
      billingListCount: list.length,
      candidateCount: candidates.length,
      rows
    };
  } catch (error) {
    return { ok: false, error: error?.message || String(error) };
  }

  async function waitForBillingListNetworkCapture(timeoutMs) {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      const capture = window.__catsBackBillingListNetworkCaptureV1?.latest;
      if (capture) {
        if (capture.status < 200 || capture.status >= 300) {
          throw new Error(`Request billing_list của trang Shopee trả về HTTP ${capture.status}.`);
        }
        if (!capture.payload) {
          throw new Error(`Không đọc được response billing_list của trang Shopee${capture.error ? `: ${capture.error}` : "."}`);
        }
        return capture;
      }
      await new Promise(resolve => setTimeout(resolve, 250));
    }
    throw new Error(
      "Không bắt được request /api/v3/payment/billing_list do trang Shopee phát ra. " +
      "Hãy reload extension rồi đăng nhập lại Shopee nếu lỗi tiếp tục xuất hiện."
    );
  }

  function flattenBillingList(list) {
    const result = [];
    for (const item of list) {
      const bills = Array.isArray(item?.bills) ? item.bills : [];
      if (bills.length === 0) {
        result.push(item);
        continue;
      }
      for (const bill of bills) {
        result.push({
          ...item,
          ...bill,
          validation_id: bill?.validation_id ?? item?.validation_id
        });
      }
    }
    return result;
  }

  function summaryTimestamp(value) {
    return optionalSafeInteger(value?.payment_completed_time) ??
      optionalSafeInteger(value?.payment_time) ??
      optionalSafeInteger(value?.payout_created_time) ??
      0;
  }

  function optionalSafeInteger(value) {
    if (value === null || value === undefined || value === "") return null;
    const number = Number(value);
    return Number.isFinite(number) && Number.isSafeInteger(number) ? number : null;
  }

  async function apiGet(path, query) {
    const url = new URL(path, location.origin);
    for (const [key, value] of Object.entries(query || {})) url.searchParams.set(key, value);
    const response = await fetch(url.toString(), {
      method: "GET",
      credentials: "include",
      headers: { Accept: "application/json, text/plain, */*" },
      cache: "no-store"
    });
    if (!response.ok) throw new Error(`Shopee API ${path} trả về HTTP ${response.status}.`);
    const contentType = response.headers.get("content-type") || "";
    if (!contentType.toLowerCase().includes("json")) {
      throw new Error("Phiên đăng nhập Shopee đã hết hạn. Hãy đăng nhập lại rồi chạy lại tool.");
    }
    const payload = await response.json();
    if (Number(payload?.code ?? -1) !== 0) {
      throw new Error(`Shopee API ${path} lỗi: ${String(payload?.msg || payload?.message || payload?.code || "unknown")}`);
    }
    return payload;
  }

  async function getValidationCheckouts(startTime, endTime, validationId) {
    const checkouts = [];
    let pageNum = 1;
    let totalCount = null;
    const seenCheckoutIds = new Set();

    while (true) {
      const payload = await apiGet("/api/v3/report/validation_detail/v2", {
        page_size: String(PAGE_SIZE),
        page_num: String(pageNum),
        start_time: String(startTime),
        end_time: String(endTime)
      });
      const data = payload?.data || {};
      const page = Array.isArray(data.list) ? data.list : [];
      const reportedTotal = rawNumber(data.total_count ?? 0, "total_count");
      if (!Number.isInteger(reportedTotal) || reportedTotal < 0) {
        throw new Error(`Bảng kê ${validationId}: total_count không hợp lệ.`);
      }
      if (reportedTotal > MAX_CHECKOUTS_PER_BILL) {
        throw new Error(`Bảng kê ${validationId}: có ${reportedTotal} checkout, vượt giới hạn ${MAX_CHECKOUTS_PER_BILL}.`);
      }
      if (totalCount === null) totalCount = reportedTotal;
      else if (reportedTotal !== totalCount) throw new Error(`Bảng kê ${validationId}: total_count thay đổi trong lúc tải.`);

      if (page.length === 0) {
        if (checkouts.length < totalCount) throw new Error(`Bảng kê ${validationId}: Shopee trả thiếu trang dữ liệu.`);
        break;
      }
      for (const checkout of page) {
        const checkoutId = String(checkout?.checkout_id ?? "").trim();
        if (!checkoutId) throw new Error(`Bảng kê ${validationId}: có checkout thiếu checkout_id.`);
        if (seenCheckoutIds.has(checkoutId)) {
          throw new Error(`Bảng kê ${validationId}: checkout_id ${checkoutId} bị trùng giữa các trang.`);
        }
        seenCheckoutIds.add(checkoutId);
      }
      checkouts.push(...page);
      if (checkouts.length >= totalCount) break;
      pageNum += 1;
      if (pageNum > 1001) throw new Error(`Bảng kê ${validationId}: không thể tải đủ dữ liệu trong 1000 trang.`);
    }

    if (checkouts.length !== totalCount) {
      throw new Error(`Bảng kê ${validationId}: nhận ${checkouts.length}/${totalCount} checkout.`);
    }
    return checkouts;
  }

  function buildOrderWeights(checkouts, sourceAffiliateId, validationId) {
    const byOrderId = new Map();
    for (const checkout of checkouts) {
      const checkoutAffiliateId = String(checkout?.affiliate_id ?? "").trim();
      if (checkoutAffiliateId && checkoutAffiliateId !== sourceAffiliateId) continue;
      const checkoutNetRaw = moneyRaw(checkout?.affiliate_net_commission ?? 0, "affiliate_net_commission");
      const orders = Array.isArray(checkout?.orders) ? checkout.orders : [];
      if (checkoutNetRaw > 0 && orders.length === 0) {
        throw new Error(`Bảng kê ${validationId}: checkout có hoa hồng nhưng không có đơn.`);
      }
      if (orders.length === 0 || checkoutNetRaw === 0) continue;

      const orderParts = orders.map((order, index) => {
        const orderId = String(order?.order_sn ?? "").trim();
        if (!orderId) throw new Error(`Bảng kê ${validationId}: có đơn thiếu order_sn.`);
        const items = Array.isArray(order?.items) ? order.items : [];
        const itemWeight = items.reduce((sum, item) => sum +
          moneyRaw(item?.item_commission ?? 0, "item_commission") +
          moneyRaw(item?.capped_brand_commission ?? 0, "capped_brand_commission"), 0);
        return { orderId, itemWeight, index };
      });
      const weights = orderParts.map(part => part.itemWeight);
      if (weights.every(value => value <= 0)) weights.fill(1);
      const allocations = allocateRaw(checkoutNetRaw, weights);
      for (let index = 0; index < orderParts.length; index++) {
        if (allocations[index] <= 0) continue;
        const id = orderParts[index].orderId;
        byOrderId.set(id, (byOrderId.get(id) || 0) + allocations[index]);
      }
    }

    return [...byOrderId.entries()]
      .map(([orderId, weightRaw]) => ({ orderId, weightRaw }))
      .sort((left, right) => left.orderId.localeCompare(right.orderId, "en"));
  }

  function allocateRaw(total, weights) {
    if (total === 0) return weights.map(() => 0);
    const sumWeights = weights.reduce((sum, value) => sum + Math.max(0, value), 0);
    if (sumWeights <= 0) throw new Error("Không có trọng số hợp lệ để phân bổ hoa hồng checkout.");
    return weights.map(value => total * Math.max(0, value) / sumWeights);
  }

  function allocateUnits(totalUnits, weights, capacities) {
    if (!Number.isSafeInteger(totalUnits) || totalUnits < 0) throw new Error("Tổng tiền phân bổ không hợp lệ.");
    if (totalUnits === 0) return weights.map(() => 0);
    const safeWeights = weights.map(value => Number.isFinite(value) && value > 0 ? value : 0);
    const sumWeights = safeWeights.reduce((sum, value) => sum + value, 0);
    if (sumWeights <= 0) throw new Error("Không có trọng số hợp lệ để phân bổ.");
    const caps = capacities || weights.map(() => Number.MAX_SAFE_INTEGER);
    if (caps.reduce((sum, value) => sum + value, 0) < totalUnits) throw new Error("Tổng phân bổ vượt số tiền khả dụng.");

    const exact = safeWeights.map(value => totalUnits * value / sumWeights);
    const result = exact.map((value, index) => Math.min(Math.floor(value), caps[index]));
    let remaining = totalUnits - result.reduce((sum, value) => sum + value, 0);
    const order = exact.map((value, index) => ({ index, fraction: value - Math.floor(value) }))
      .sort((left, right) => right.fraction - left.fraction || left.index - right.index);
    while (remaining > 0) {
      let changed = false;
      for (const candidate of order) {
        if (remaining === 0) break;
        if (result[candidate.index] >= caps[candidate.index]) continue;
        result[candidate.index] += 1;
        remaining -= 1;
        changed = true;
      }
      if (!changed) throw new Error("Không thể phân bổ hết tổng tiền.");
    }
    return result;
  }

  function moneyRaw(value, field) {
    const number = rawNumber(value ?? 0, field);
    if (number < 0) throw new Error(`${field} không được âm.`);
    return number;
  }

  function rawNumber(value, field) {
    const number = Number(value);
    if (!Number.isFinite(number) || !Number.isSafeInteger(number)) {
      throw new Error(`${field} không phải số nguyên an toàn.`);
    }
    return number;
  }

  function nonZero(value) {
    return rawNumber(value ?? 0, "adjustment") !== 0;
  }

  function positiveTimestamp(value, message) {
    const number = rawNumber(value ?? 0, "timestamp");
    if (number <= 0) throw new Error(message);
    return number;
  }

  function optionalPositiveTimestamp(value) {
    const number = rawNumber(value ?? 0, "timestamp");
    return number > 0 ? number : null;
  }

  function rawToOutputUnits(raw) {
    const units = Math.round(raw * OUTPUT_SCALE / MONEY_SCALE);
    if (!Number.isSafeInteger(units) || units < 0) throw new Error("Giá trị tiền vượt giới hạn an toàn.");
    return units;
  }

  function formatUnits(units) {
    const whole = Math.floor(units / OUTPUT_SCALE);
    const fraction = String(units % OUTPUT_SCALE).padStart(4, "0");
    return `${whole}.${fraction}`;
  }

  function timestampIso(seconds) {
    const value = new Date(seconds * 1000);
    if (!Number.isFinite(value.getTime())) throw new Error("Timestamp Shopee không hợp lệ.");
    return value.toISOString();
  }
}

class UserFacingError extends Error {
  constructor(code, message) {
    super(message);
    this.name = "UserFacingError";
    this.code = code;
  }
}
