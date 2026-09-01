if (!globalThis.__catsbackShopeeContentReady) {
  globalThis.__catsbackShopeeContentReady = true;
  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message?.type === "PING_CATSBACK_CONTENT") {
      sendResponse({ ok: true, status: "CONTENT_READY", href: location.href });
      return false;
    }

    routeMessage(message)
      .then(sendResponse)
      .catch(error => sendResponse({
        ok: false,
        status: "ERROR",
        message: error?.message || String(error)
      }));
    return true;
  });
}

const REPORT_FILE_RE = /AffiliateCommissionReport_\d{12}\.csv/ig;
const REPORT_FILE_EXACT_RE = /^AffiliateCommissionReport_\d{12}\.csv$/i;

async function routeMessage(message) {
  if (message?.type === "GET_EXPORT_ITEMS" || message?.type === "GET_READY_EXPORTS") {
    if (isLoginPage()) return { ok: false, status: "LOGIN_REQUIRED", exports: [] };
    await waitForDocumentStable();
    if (!location.pathname.includes("/export_management")) {
      return { ok: false, status: "WRONG_PAGE", exports: [], message: `Đang ở ${location.pathname}.` };
    }
    return { ok: true, status: "OK", exports: getExportItemsFromDom() };
  }

  if (message?.type === "TRIGGER_CONVERSION_EXPORT") {
    return triggerConversionExport(message.config || {});
  }

  if (message?.type === "GET_DOWNLOAD_LINK" || message?.type === "DOWNLOAD_EXPORT") {
    if (isLoginPage()) return { ok: false, status: "LOGIN_REQUIRED" };
    await waitForDocumentStable();
    return getDownloadLink(message.taskId, message.fileName);
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

  const lookup = await waitForSemanticExportControl(config, 10000);
  if (!lookup.element) {
    return {
      ok: false,
      status: lookup.ambiguous ? "EXPORT_BUTTON_AMBIGUOUS" : "EXPORT_BUTTON_NOT_FOUND",
      message: lookup.ambiguous
        ? `Tìm thấy nhiều phần tử có cùng độ tin cậy cho nút Xuất dữ liệu. Tool không click để tránh nhầm. ${lookup.debug || ""}`.trim()
        : `Không tìm thấy control có ngữ nghĩa 'Xuất dữ liệu'. ${lookup.debug || ""}`.trim()
    };
  }

  const control = lookup.element;
  if (isDisabled(control)) {
    return {
      ok: false,
      status: "EXPORT_BUTTON_DISABLED",
      message: "Nút 'Xuất dữ liệu' đang bị disable."
    };
  }

  const activation = findSemanticActivationTarget(control, config);
  const target = activation.element || control;

  target.scrollIntoView({ block: "center", inline: "center", behavior: "auto" });
  await sleep(150);
  try { target.focus?.({ preventScroll: true }); } catch (_) {}

  // Quan trọng: HTML Shopee hiện tại là <button><a>Xuất dữ liệu</a></button>.
  // Handler có thể nằm ở phần tử con. Click wrapper <button> không đảm bảo
  // listener trên <a> được gọi vì event không bubble xuống descendant.
  // Vì vậy click đúng target sâu nhất mang exact business label.
  dispatchUserLikeClick(target);

  return {
    ok: true,
    status: "EXPORT_BUTTON_CLICKED",
    message: "Đã kích hoạt 'Xuất dữ liệu' bằng semantic activation target.",
    debug: `control=${describeElement(control)} score=${lookup.score}; target=${describeElement(target)} strategy=${activation.strategy}`
  };
}


function findSemanticActivationTarget(control, config) {
  if (!control) return { element: null, strategy: "none" };
  const labels = new Set(
    (Array.isArray(config?.exportButtonTexts) && config.exportButtonTexts.length
      ? config.exportButtonTexts
      : ["Xuất dữ liệu", "Xuất báo cáo", "Export"])
      .map(normalizeText)
      .filter(Boolean)
  );

  // Ưu tiên descendant có exact business label. HTML Shopee hiện tại dùng <a>
  // không có href bên trong button, nên semantic locator wrapper không được bỏ qua nó.
  const descendants = Array.from(control.querySelectorAll?.("a, span, strong, em, div") || [])
    .filter(el => isVisible(el) && labels.has(normalizeText(el.textContent || el.innerText || "")))
    .map(el => {
      const tag = String(el.tagName || "").toLowerCase();
      const childCount = el.children?.length || 0;
      let score = 0;
      if (tag === "a") score += 100;
      else if (tag === "span") score += 70;
      else score += 40;
      if (childCount === 0) score += 30;
      const rect = el.getBoundingClientRect();
      score += Math.max(0, 20 - Math.min(20, Math.round((rect.width * rect.height) / 10000)));
      return { el, score };
    })
    .sort((a, b) => b.score - a.score);

  if (descendants[0]) {
    return { element: descendants[0].el, strategy: "exact-label-descendant" };
  }

  // Fallback mô phỏng điểm người dùng click ở giữa control.
  try {
    const rect = control.getBoundingClientRect();
    const x = rect.left + rect.width / 2;
    const y = rect.top + rect.height / 2;
    const hit = document.elementFromPoint(x, y);
    if (hit && control.contains(hit) && isVisible(hit)) {
      return { element: hit, strategy: "center-hit-test" };
    }
  } catch (_) {}

  return { element: control, strategy: "semantic-control" };
}

function dispatchUserLikeClick(target) {
  if (!target) throw new Error("Missing click target");
  const init = { bubbles: true, cancelable: true, composed: true, view: window };
  try { target.dispatchEvent(new PointerEvent("pointerdown", { ...init, pointerId: 1, pointerType: "mouse", isPrimary: true, button: 0, buttons: 1 })); } catch (_) {}
  try { target.dispatchEvent(new MouseEvent("mousedown", { ...init, button: 0, buttons: 1 })); } catch (_) {}
  try { target.dispatchEvent(new PointerEvent("pointerup", { ...init, pointerId: 1, pointerType: "mouse", isPrimary: true, button: 0, buttons: 0 })); } catch (_) {}
  try { target.dispatchEvent(new MouseEvent("mouseup", { ...init, button: 0, buttons: 0 })); } catch (_) {}
  try { HTMLElement.prototype.click.call(target); } catch (_) { target.click(); }
}

async function waitForSemanticExportControl(config, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  let last = null;
  while (Date.now() < deadline) {
    last = findSemanticExportControl(config);
    if (last.element || last.ambiguous) return last;
    await sleep(350);
  }
  return last || { element: null, ambiguous: false, debug: "Không có candidate." };
}

function findSemanticExportControl(config) {
  const labels = new Set(
    (Array.isArray(config.exportButtonTexts) && config.exportButtonTexts.length
      ? config.exportButtonTexts
      : ["Xuất dữ liệu", "Xuất báo cáo", "Export"])
      .map(normalizeText)
      .filter(Boolean)
  );

  const candidates = new Map();

  // 1) Native/ARIA interactive controls.
  for (const raw of document.querySelectorAll('button, a[href], [role="button"], input[type="button"], input[type="submit"]')) {
    addSemanticCandidate(candidates, raw, labels, false);
  }

  // 2) Text có thể nằm trong span/div/a con. Leo lên interactive ancestor thay vì bám class.
  for (const raw of document.querySelectorAll("span, div, strong, p, a")) {
    if (!labels.has(normalizeText(raw.textContent || ""))) continue;
    const root = findInteractiveAncestor(raw);
    if (root) addSemanticCandidate(candidates, root, labels, true);
  }

  // 3) Selector tay chỉ là emergency override. Không dùng trong default logic.
  const explicit = String(config.exportButtonSelector || "").trim();
  if (explicit) {
    try {
      for (const raw of document.querySelectorAll(explicit)) {
        const root = findInteractiveAncestor(raw) || raw;
        addSemanticCandidate(candidates, root, labels, true, 15);
      }
    } catch (_) {}
  }

  const ranked = Array.from(candidates.values())
    .filter(x => x.score >= 150)
    .sort((a, b) => b.score - a.score);

  if (!ranked.length) {
    return {
      element: null,
      ambiguous: false,
      score: 0,
      debug: summarizeCandidates(Array.from(candidates.values()).sort((a, b) => b.score - a.score).slice(0, 4))
    };
  }

  const top = ranked[0];
  const nearTop = ranked.filter(x => top.score - x.score <= 5);
  if (nearTop.length > 1) {
    return {
      element: null,
      ambiguous: true,
      score: top.score,
      debug: summarizeCandidates(nearTop.slice(0, 4))
    };
  }

  return {
    element: top.element,
    ambiguous: false,
    score: top.score,
    debug: summarizeCandidates(ranked.slice(0, 3))
  };
}

function addSemanticCandidate(map, raw, labels, foundFromExactChild, bonus = 0) {
  const element = findInteractiveAncestor(raw) || raw;
  if (!element || !isVisible(element) || isDisabled(element)) return;

  const key = element;
  const tag = String(element.tagName || "").toLowerCase();
  const text = normalizeText(element.innerText || element.textContent || element.value || "");
  const aria = normalizeText(element.getAttribute?.("aria-label") || "");
  const title = normalizeText(element.getAttribute?.("title") || "");

  let score = bonus;
  if (labels.has(text)) score += 120;
  else if (Array.from(labels).some(label => text === label || text.startsWith(`${label} `) || text.endsWith(` ${label}`))) score += 80;
  if (labels.has(aria)) score += 95;
  if (labels.has(title)) score += 90;
  if (foundFromExactChild) score += 35;
  if (tag === "button") score += 35;
  else if (element.getAttribute?.("role") === "button") score += 30;
  else if (tag === "a") score += 20;
  else if (tag === "input") score += 15;
  if (isVisible(element)) score += 20;
  if (!isDisabled(element)) score += 20;
  if (location.pathname.includes("/report/conversion_report")) score += 25;
  if (tag === "button" && (element.getAttribute("type") || "button").toLowerCase() === "button") score += 5;

  const existing = map.get(key);
  if (!existing || score > existing.score) {
    map.set(key, { element, score, text, aria, title });
  }
}

function findInteractiveAncestor(el) {
  if (!el || el.nodeType !== Node.ELEMENT_NODE) return null;
  if (isInteractiveElement(el)) return el;
  return el.closest?.('button, a[href], [role="button"], input[type="button"], input[type="submit"]') || null;
}

function isInteractiveElement(el) {
  if (!el || el.nodeType !== Node.ELEMENT_NODE) return false;
  const tag = String(el.tagName || "").toLowerCase();
  return tag === "button" || (tag === "a" && Boolean(el.getAttribute?.("href"))) || tag === "input" || el.getAttribute?.("role") === "button";
}

function summarizeCandidates(items) {
  if (!items?.length) return "Không có candidate semantic phù hợp.";
  return items.map(x => `[${x.score}] ${describeElement(x.element)}`).join(" | ");
}

function getExportItemsFromDom() {
  const result = [];
  const byFile = new Map();
  const byTask = new Map();

  // Baseline phải chứa cả filename chưa có download link để tránh lấy nhầm
  // một task cũ vừa chuyển từ processing -> ready sau khi lần sync mới bắt đầu.
  for (const fileName of collectReportFileNames(document.body?.innerText || document.body?.textContent || "")) {
    byFile.set(fileName, {
      taskId: "",
      fileName,
      href: "",
      ready: false,
      statusText: "",
      source: "document-text"
    });
  }

  // Readiness dựa trên business contract: có link export/download + task_id.
  // Không cần class .export-item, .ant-progress-status-success hay icon class.
  for (const anchor of document.querySelectorAll("a[href]")) {
    const parsed = parseExportDownloadAnchor(anchor);
    if (!parsed) continue;

    const existing = byTask.get(parsed.taskId) || {
      taskId: parsed.taskId,
      fileName: "",
      href: parsed.href,
      ready: true,
      statusText: "ready-by-download-link",
      source: "download-link"
    };

    existing.href = parsed.href;
    existing.ready = true;
    const inferred = inferReportFileNameForAnchor(anchor, parsed.taskId);
    if (inferred) existing.fileName = inferred;
    byTask.set(parsed.taskId, existing);
  }

  // Merge task items first. Nếu link icon không có text, anchor khác cùng task_id
  // hoặc ancestor text sẽ cung cấp filename.
  for (const item of byTask.values()) {
    if (!item.fileName) {
      item.fileName = inferFileNameByTaskId(item.taskId) || "";
    }
    if (item.fileName && REPORT_FILE_EXACT_RE.test(item.fileName)) {
      byFile.set(item.fileName, { ...(byFile.get(item.fileName) || {}), ...item });
    } else if (item.taskId) {
      result.push(item);
    }
  }

  for (const item of byFile.values()) result.push(item);

  const seen = new Set();
  const deduped = result.filter(item => {
    const key = item.taskId ? `task:${item.taskId}` : `file:${item.fileName}`;
    if (!key || seen.has(key)) return false;
    seen.add(key);
    return true;
  });

  deduped.sort((a, b) => {
    const at = parseFileTime(a.fileName);
    const bt = parseFileTime(b.fileName);
    if (bt !== at) return bt - at;
    return Number(b.taskId || 0) - Number(a.taskId || 0);
  });
  return deduped;
}

function getDownloadLink(taskId, expectedFileName) {
  const wanted = String(taskId || "");
  if (!wanted) {
    return { ok: false, status: "INVALID_TASK_ID", message: "Thiếu taskId cần tải." };
  }

  const matching = [];
  for (const anchor of document.querySelectorAll("a[href]")) {
    const parsed = parseExportDownloadAnchor(anchor);
    if (!parsed || String(parsed.taskId) !== wanted) continue;
    const fileName = inferReportFileNameForAnchor(anchor, wanted) || cleanText(expectedFileName || "");
    let score = 0;
    if (REPORT_FILE_EXACT_RE.test(fileName)) score += 100;
    if (expectedFileName && fileName === expectedFileName) score += 60;
    if (cleanText(anchor.textContent || "") === fileName) score += 30;
    if (anchor.hasAttribute("download")) score += 10;
    matching.push({ anchor, href: parsed.href, fileName, score });
  }

  if (!matching.length) {
    return {
      ok: false,
      status: "DOWNLOAD_LINK_NOT_FOUND",
      message: `Không tìm thấy link export/download có task_id=${wanted}.`
    };
  }

  matching.sort((a, b) => b.score - a.score);
  const best = matching[0];
  return {
    ok: true,
    status: "DOWNLOAD_LINK_READY",
    taskId: wanted,
    fileName: best.fileName || expectedFileName || "",
    href: best.href,
    debug: describeElement(best.anchor)
  };
}

function parseExportDownloadAnchor(anchor) {
  const href = anchor?.getAttribute?.("href") || anchor?.href || "";
  if (!href) return null;
  try {
    const url = new URL(href, location.origin);
    const taskId = url.searchParams.get("task_id") || url.searchParams.get("taskId") || "";
    if (!taskId) return null;
    const path = url.pathname.toLowerCase();
    if (!(path.includes("/export/download") || (path.includes("/download") && path.includes("export")))) return null;
    return { taskId, href: url.href };
  } catch (_) {
    const taskMatch = String(href).match(/[?&](?:task_id|taskId)=([^&#]+)/i);
    if (!taskMatch || !/export[^?]*\/download|\/download[^?]*export/i.test(String(href))) return null;
    return { taskId: decodeURIComponent(taskMatch[1]), href: new URL(href, location.origin).href };
  }
}

function inferReportFileNameForAnchor(anchor, taskId) {
  const sources = [
    anchor?.textContent,
    anchor?.getAttribute?.("download"),
    anchor?.getAttribute?.("title"),
    anchor?.getAttribute?.("aria-label")
  ];

  for (const source of sources) {
    const file = firstReportFileName(source);
    if (file) return file;
  }

  let node = anchor;
  for (let depth = 0; node && depth < 6; depth++, node = node.parentElement) {
    const file = firstReportFileName(node.textContent || "");
    if (file) return file;
  }

  // HTML hiện tại có cả filename link và icon link cùng task_id.
  if (taskId) {
    for (const other of document.querySelectorAll("a[href]")) {
      const parsed = parseExportDownloadAnchor(other);
      if (!parsed || String(parsed.taskId) !== String(taskId)) continue;
      const file = firstReportFileName(other.textContent || other.getAttribute?.("download") || "");
      if (file) return file;
    }
  }

  return "";
}

function inferFileNameByTaskId(taskId) {
  for (const anchor of document.querySelectorAll("a[href]")) {
    const parsed = parseExportDownloadAnchor(anchor);
    if (!parsed || String(parsed.taskId) !== String(taskId)) continue;
    const file = inferReportFileNameForAnchor(anchor, taskId);
    if (file) return file;
  }
  return "";
}

function collectReportFileNames(text) {
  const matches = String(text || "").match(REPORT_FILE_RE) || [];
  return Array.from(new Set(matches.map(cleanText)));
}

function firstReportFileName(text) {
  const match = String(text || "").match(/AffiliateCommissionReport_\d{12}\.csv/i);
  return match ? match[0] : "";
}

function parseFileTime(fileName) {
  const m = String(fileName || "").match(/^AffiliateCommissionReport_(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})\.csv$/i);
  if (!m) return 0;
  const dt = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]), Number(m[4]), Number(m[5]), 0, 0);
  const ts = dt.getTime();
  return Number.isFinite(ts) ? ts : 0;
}

function isLoginPage() {
  const url = location.href.toLowerCase();
  if (url.includes("/auth") || url.includes("login") || url.includes("signin")) return true;
  const password = document.querySelector("input[type='password']");
  if (password && isVisible(password)) return true;
  return false;
}

function describeElement(el) {
  if (!el) return "unknown";
  const tag = String(el.tagName || "").toLowerCase();
  const text = cleanText(el.innerText || el.textContent || el.value || "").slice(0, 100);
  const role = el.getAttribute?.("role") || "";
  const href = el.getAttribute?.("href") || "";
  const cls = cleanText(typeof el.className === "string" ? el.className : "").slice(0, 100);
  // class chỉ để debug, không dùng làm identity/locator.
  return `${tag}${role ? `[role=${role}]` : ""}${href ? ` href=\"${href}\"` : ""} text=\"${text}\"${cls ? ` class(debug)=\"${cls}\"` : ""}`;
}

function isDisabled(el) {
  return Boolean(
    el?.disabled ||
    el?.getAttribute?.("aria-disabled") === "true" ||
    el?.getAttribute?.("disabled") !== null
  );
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
