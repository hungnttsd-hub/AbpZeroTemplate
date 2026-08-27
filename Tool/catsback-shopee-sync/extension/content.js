chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  routeMessage(message)
    .then(sendResponse)
    .catch(error => sendResponse({
      ok: false,
      status: "ERROR",
      message: error?.message || String(error)
    }));
  return true;
});

async function routeMessage(message) {
  if (message?.type === "GET_READY_EXPORTS") {
    if (isLoginPage()) return { ok: false, status: "LOGIN_REQUIRED", exports: [] };
    await waitForDocumentStable();
    return { ok: true, status: "OK", exports: getReadyExportsFromDom() };
  }

  if (message?.type === "TRIGGER_CONVERSION_EXPORT") {
    return triggerConversionExport(message.config || {});
  }

  if (message?.type === "DOWNLOAD_EXPORT") {
    if (isLoginPage()) return { ok: false, status: "LOGIN_REQUIRED" };
    await waitForDocumentStable();
    return downloadExport(message.taskId, message.fileName);
  }

  return { ok: false, status: "UNKNOWN_MESSAGE" };
}

async function triggerConversionExport(config) {
  if (isLoginPage()) {
    return { ok: false, status: "LOGIN_REQUIRED", message: "Trang Shopee đang yêu cầu đăng nhập." };
  }

  if (!location.pathname.includes("/report/conversion_report")) {
    return {
      ok: false,
      status: "WRONG_PAGE",
      message: `Đang ở ${location.pathname}, không phải conversion_report.`
    };
  }

  await waitForDocumentStable();

  const button = findExportButton(config);
  if (!button) {
    return {
      ok: false,
      status: "EXPORT_BUTTON_NOT_FOUND",
      message: "Không tìm thấy nút 'Xuất dữ liệu' trên Báo cáo chuyển đổi."
    };
  }

  if (button.disabled || button.getAttribute("aria-disabled") === "true") {
    return {
      ok: false,
      status: "EXPORT_BUTTON_DISABLED",
      message: "Nút 'Xuất dữ liệu' đang bị disable."
    };
  }

  button.scrollIntoView({ block: "center", behavior: "auto" });
  button.click();

  return {
    ok: true,
    status: "EXPORT_TRIGGERED",
    message: "Đã click nút Xuất dữ liệu."
  };
}

function getReadyExportsFromDom() {
  const rows = Array.from(document.querySelectorAll(".export-item"));
  const result = [];
  const seen = new Set();

  for (const row of rows) {
    const fileAnchor = row.querySelector("a.export-item-file-name[download], a.export-item-file-name[href]");
    const anchors = Array.from(row.querySelectorAll("a[download][href], a[href*='/export/download?task_id='], a[href*='/export-common/download?taskId=']"));
    const downloadAnchor = anchors.find(a => parseTaskIdFromHref(a.href));
    const anchor = downloadAnchor || fileAnchor;
    if (!anchor?.href) continue;

    const taskId = parseTaskIdFromHref(anchor.href);
    if (!taskId || seen.has(String(taskId))) continue;

    const fileName = cleanText(fileAnchor?.textContent || "") || inferFileNameFromRow(row);
    if (!fileName || !/^AffiliateCommissionReport_.*\.csv$/i.test(fileName)) continue;

    seen.add(String(taskId));
    result.push({
      taskId: String(taskId),
      fileName,
      href: anchor.href
    });
  }

  // Fallback nếu Shopee đổi class nhưng vẫn giữ <a download href="...task_id=...">.
  for (const anchor of document.querySelectorAll("a[download][href]")) {
    const taskId = parseTaskIdFromHref(anchor.href);
    if (!taskId || seen.has(String(taskId))) continue;

    const row = anchor.closest(".export-item") || anchor.parentElement;
    const fileName = cleanText(
      row?.querySelector?.("a.export-item-file-name")?.textContent ||
      anchor.getAttribute("download") ||
      anchor.textContent ||
      ""
    );

    if (!/^AffiliateCommissionReport_.*\.csv$/i.test(fileName)) continue;

    seen.add(String(taskId));
    result.push({ taskId: String(taskId), fileName, href: anchor.href });
  }

  result.sort((a, b) => Number(b.taskId) - Number(a.taskId));
  return result;
}

function downloadExport(taskId, expectedFileName) {
  const wanted = String(taskId || "");
  if (!wanted) {
    return { ok: false, status: "INVALID_TASK_ID", message: "Thiếu taskId cần tải." };
  }

  const anchors = Array.from(document.querySelectorAll("a[download][href], a[href]"));
  const anchor = anchors.find(a => String(parseTaskIdFromHref(a.href)) === wanted);

  if (!anchor) {
    return {
      ok: false,
      status: "DOWNLOAD_LINK_NOT_FOUND",
      message: `Không tìm thấy link download cho task ${wanted}.`
    };
  }

  const row = anchor.closest(".export-item");
  const fileName = cleanText(
    row?.querySelector("a.export-item-file-name")?.textContent ||
    expectedFileName ||
    anchor.getAttribute("download") ||
    ""
  );

  anchor.scrollIntoView({ block: "center", behavior: "auto" });
  anchor.click();

  return {
    ok: true,
    status: "DOWNLOAD_TRIGGERED",
    taskId: wanted,
    fileName,
    href: anchor.href
  };
}

function parseTaskIdFromHref(href) {
  if (!href) return "";
  try {
    const url = new URL(href, location.origin);
    return url.searchParams.get("task_id") || url.searchParams.get("taskId") || "";
  } catch (_) {
    const match = String(href).match(/[?&](?:task_id|taskId)=([^&#]+)/i);
    return match ? decodeURIComponent(match[1]) : "";
  }
}

function inferFileNameFromRow(row) {
  const text = cleanText(row?.textContent || "");
  const match = text.match(/AffiliateCommissionReport_[0-9]{8,14}[^\s]*\.csv/i);
  return match ? match[0] : "";
}

function isLoginPage() {
  const url = location.href.toLowerCase();
  if (url.includes("/auth") || url.includes("login") || url.includes("signin")) return true;

  const password = document.querySelector("input[type='password']");
  if (password && isVisible(password)) return true;

  const loginText = findByText(["Đăng nhập", "Log in", "Login", "Sign in"], ["button", "a", "[role='button']"]);
  return Boolean(loginText && isVisible(loginText) && !location.pathname.includes("conversion_report") && !location.pathname.includes("export_management"));
}

function findExportButton(config) {
  if (config.exportButtonSelector) {
    try {
      const el = document.querySelector(config.exportButtonSelector);
      if (el && isVisible(el)) return el.closest("button, [role='button'], a") || el;
    } catch (_) {}
  }

  const texts = Array.isArray(config.exportButtonTexts) && config.exportButtonTexts.length
    ? config.exportButtonTexts
    : ["Xuất dữ liệu", "Xuất báo cáo", "Export"];

  return findByText(texts, ["button", "[role='button']", "a", "span", "div"]);
}

function findByText(texts, selectors) {
  const targets = texts.map(normalizeText);
  for (const selector of selectors) {
    for (const el of document.querySelectorAll(selector)) {
      if (!isVisible(el)) continue;
      const text = normalizeText(el.innerText || el.textContent || "");
      if (!text) continue;
      if (targets.some(target => text === target || text.includes(target))) {
        const clickable = el.closest("button, [role='button'], a") || el;
        if (isVisible(clickable)) return clickable;
      }
    }
  }
  return null;
}

function isVisible(el) {
  if (!el) return false;
  const style = getComputedStyle(el);
  if (style.display === "none" || style.visibility === "hidden" || Number(style.opacity) === 0) return false;
  const rect = el.getBoundingClientRect();
  return rect.width > 0 && rect.height > 0;
}

function cleanText(value) {
  return String(value || "").replace(/\s+/g, " ").trim();
}

function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase()
    .replace(/\s+/g, " ");
}

async function waitForDocumentStable() {
  if (document.readyState !== "complete") {
    await new Promise(resolve => window.addEventListener("load", resolve, { once: true }));
  }
  await sleep(500);
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}
