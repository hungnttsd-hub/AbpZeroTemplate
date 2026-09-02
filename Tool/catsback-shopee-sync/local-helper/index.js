const fs = require("fs");
const fsp = require("fs/promises");
const path = require("path");
const os = require("os");
const crypto = require("crypto");
const http = require("http");

const APP_VERSION = "0.7.4";
const APP_DIR = __dirname;
const CONFIG_PATH = path.join(APP_DIR, "config.json");
const EXAMPLE_CONFIG_PATH = path.join(APP_DIR, "config.example.json");
const STATE_PATH = path.join(APP_DIR, "state.json");
const LOG_DIR = path.join(APP_DIR, "logs");
const LOG_PATH = path.join(LOG_DIR, "helper.log");
const pending = new Map();

const DEFAULT_CONFIG = {
  watchDir: "%USERPROFILE%\\Downloads\\CatsBack",
  fileRegex: "^AffiliateCommissionReport_.*\\.(csv|txt)$",
  apiBaseUrl: "https://catsback.onrender.com",
  tokenPath: "/api/public/shopee-automation/token",
  importPath: "/api/public/shopee-automation/reports/import",
  settlementImportPath: "/api/public/shopee-automation/settlements/import",
  settlementOutputDir: "%USERPROFILE%\\Downloads\\CatsBackSettlements",
  clientId: "",
  clientSecret: "",
  formFieldName: "report",
  tokenRefreshSkewSeconds: 60,
  archiveOnSuccess: true,
  archiveDir: "%USERPROFILE%\\Downloads\\CatsBackArchive",
  stableChecks: 4,
  stableCheckIntervalMs: 1500,
  retryCount: 3,
  retryDelayMs: 5000,
  requestTimeoutMs: 120000,
  settingsPort: 32145
};

const SETTLEMENT_SCHEMA_VERSION = "catsback-settlement-v2";
const SETTLEMENT_HEADERS = [
  "schema_version",
  "source_affiliate_id",
  "validation_id",
  "payout_id",
  "payment_completed_at_utc",
  "order_completed_from_utc",
  "order_completed_to_utc",
  "payment_status",
  "validation_payout_status",
  "overall_validation_status",
  "bill_validation_status",
  "settlement_cycle",
  "has_adjustment",
  "has_clawback",
  "is_cumulative",
  "has_bonus",
  "has_ppp",
  "bill_eligible_commission",
  "bill_after_service_fee",
  "bill_paid_commission",
  "order_id",
  "order_eligible_commission",
  "allocated_service_fee",
  "allocated_tax",
  "actual_paid_commission"
];

let tokenCache = emptyTokenCache();

main().catch(async err => {
  await log(`FATAL ${err.stack || err.message || err}`);
  process.exit(1);
});

async function main() {
  await fsp.mkdir(LOG_DIR, { recursive: true });
  await ensureConfig();

  const config = loadConfig();
  const watchDir = expandEnv(config.watchDir);
  await fsp.mkdir(watchDir, { recursive: true });

  startSettingsServer(Number(config.settingsPort) || 32145);
  await log(`START version=${APP_VERSION} watchDir=${watchDir}`);
  await log(`API_CONFIGURED ${isApiConfigured(config)}`);

  const regex = new RegExp(config.fileRegex, "i");
  fs.watch(watchDir, { persistent: true }, (eventType, filename) => {
    if (!filename) return;
    const name = filename.toString();
    if (!regex.test(name)) return;

    const filePath = path.join(watchDir, name);
    scheduleProcess(filePath);
  });

  await log("WATCH_READY only-new-files-after-start=true");
}

async function ensureConfig() {
  if (fs.existsSync(CONFIG_PATH)) return;

  let initial = DEFAULT_CONFIG;
  if (fs.existsSync(EXAMPLE_CONFIG_PATH)) {
    try {
      initial = { ...DEFAULT_CONFIG, ...JSON.parse(fs.readFileSync(EXAMPLE_CONFIG_PATH, "utf8")) };
    } catch (_) {}
  }

  await saveConfig(initial);
  await log(`CONFIG_CREATED ${CONFIG_PATH}`);
}

function scheduleProcess(filePath) {
  const existing = pending.get(filePath);
  if (existing) clearTimeout(existing);

  const timer = setTimeout(async () => {
    pending.delete(filePath);
    try {
      if (!fs.existsSync(filePath)) return;
      await processFile(filePath);
    } catch (error) {
      await log(`ERROR file=${filePath} ${error.stack || error.message || error}`);
    }
  }, 1500);

  pending.set(filePath, timer);
}

function loadConfig() {
  const raw = fs.readFileSync(CONFIG_PATH, "utf8");
  const parsed = JSON.parse(raw);

  // v0.3 migration: keep watch/runtime options, but move authentication to
  // short-lived client-credentials flow. Old static apiToken is intentionally ignored.
  const migrated = { ...parsed };
  if (!migrated.apiBaseUrl) {
    try {
      if (migrated.importUrl) {
        const old = new URL(migrated.importUrl);
        migrated.apiBaseUrl = old.origin;
      }
    } catch (_) {}
  }

  return { ...DEFAULT_CONFIG, ...migrated };
}

async function saveConfig(config) {
  const clean = { ...DEFAULT_CONFIG, ...config };

  // Do not keep obsolete static-token settings from v0.3.
  delete clean.apiToken;
  delete clean.apiKey;
  delete clean.authHeader;
  delete clean.authPrefix;
  delete clean.uploadMode;
  delete clean.fileNameQueryParam;
  delete clean.importUrl;

  const tempPath = `${CONFIG_PATH}.tmp`;
  await fsp.writeFile(tempPath, JSON.stringify(clean, null, 2), "utf8");
  await fsp.rename(tempPath, CONFIG_PATH);
}

async function processFile(filePath) {
  const config = loadConfig();

  if (!isApiConfigured(config)) {
    await log(`WAIT_API_CONFIG file=${filePath} clientId=${Boolean(config.clientId)} clientSecret=${Boolean(config.clientSecret)}`);
    return;
  }

  await waitUntilStable(filePath, config);

  const hash = await sha256(filePath);
  const state = await loadState();
  if (state.processed?.[hash]) {
    await log(`SKIP_DUP hash=${hash} file=${filePath}`);
    return;
  }

  await log(`UPLOAD_START hash=${hash} file=${filePath}`);
  let lastError;
  const attempts = Math.max(1, Number(config.retryCount) || 3);

  for (let attempt = 1; attempt <= attempts; attempt++) {
    try {
      const result = await uploadReport(filePath, config);
      const summary = summarizeResponse(result.body);
      await log(`UPLOAD_OK attempt=${attempt} status=${result.status} file=${filePath}${summary ? ` response=${summary}` : ""}`);

      state.processed = state.processed || {};
      state.processed[hash] = {
        file: filePath,
        at: new Date().toISOString(),
        responseStatus: result.status,
        response: result.body.slice(0, 4000)
      };
      await saveState(state);

      if (config.archiveOnSuccess) {
        await archive(filePath, config.archiveDir);
      }
      return;
    } catch (error) {
      lastError = error;
      await log(`UPLOAD_FAIL attempt=${attempt}/${attempts} error=${sanitizeError(error.message)}`);
      if (attempt < attempts) await sleep(Number(config.retryDelayMs) || 5000);
    }
  }

  throw lastError || new Error("Upload failed");
}

async function uploadReport(filePath, config) {
  requireModernNode();
  if (!isApiConfigured(config)) throw new Error("CatsBack Client ID/Client Secret is not configured.");

  let token = await getAccessToken(config, false);
  let result = await postReport(filePath, config, token);

  // A token can become invalid immediately after ClientSecret rotation.
  // Refresh once on 401, then let the normal outer retry policy handle other failures.
  if (result.status === 401) {
    clearTokenCache();
    await log("IMPORT_401 refreshing_access_token=true");
    token = await getAccessToken(config, true);
    result = await postReport(filePath, config, token);
  }

  if (result.status < 200 || result.status >= 300) {
    throw new Error(`HTTP ${result.status}: ${result.body.slice(0, 1500)}`);
  }

  return result;
}

async function uploadSettlementReport(filePath, config) {
  requireModernNode();
  if (!isApiConfigured(config)) throw new Error("CatsBack Client ID/Client Secret is not configured.");

  let token = await getAccessToken(config, false);
  let result = await postReport(filePath, config, token, config.settlementImportPath);
  if (result.status === 401) {
    clearTokenCache();
    await log("SETTLEMENT_IMPORT_401 refreshing_access_token=true");
    token = await getAccessToken(config, true);
    result = await postReport(filePath, config, token, config.settlementImportPath);
  }
  if (result.status < 200 || result.status >= 300) {
    throw new Error(`HTTP ${result.status}: ${result.body.slice(0, 1500)}`);
  }
  return result;
}

async function getAccessToken(config, forceRefresh = false) {
  requireModernNode();
  const skewMs = Math.max(0, Number(config.tokenRefreshSkewSeconds) || 60) * 1000;
  const now = Date.now();

  if (!forceRefresh && tokenCache.accessToken && tokenCache.expiresAtMs - skewMs > now) {
    return { ...tokenCache };
  }

  const tokenUrl = resolveApiUrl(config.apiBaseUrl, config.tokenPath);
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), Math.max(5000, Number(config.requestTimeoutMs) || 120000));

  try {
    const response = await fetch(tokenUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json"
      },
      body: JSON.stringify({
        client_id: String(config.clientId || "").trim(),
        client_secret: String(config.clientSecret || "")
      }),
      signal: controller.signal
    });

    const text = await response.text();
    if (!response.ok) {
      throw new Error(`TOKEN_HTTP_${response.status}: ${text.slice(0, 1000)}`);
    }

    let payload;
    try {
      payload = JSON.parse(text);
    } catch {
      throw new Error("Token endpoint returned invalid JSON.");
    }

    const accessToken = String(payload.access_token || "").trim();
    if (!accessToken) throw new Error("Token response does not contain access_token.");

    const tokenType = String(payload.token_type || "Bearer").trim() || "Bearer";
    const expiresInSeconds = Math.max(1, Number(payload.expires_in) || 1800);
    const parsedExpiry = payload.expires_at_utc ? Date.parse(payload.expires_at_utc) : NaN;
    const expiresAtMs = Number.isFinite(parsedExpiry) ? parsedExpiry : now + expiresInSeconds * 1000;

    tokenCache = { accessToken, tokenType, expiresAtMs };
    await log(`TOKEN_OK type=${tokenType} expiresAt=${new Date(expiresAtMs).toISOString()}`);
    return { ...tokenCache };
  } finally {
    clearTimeout(timeout);
  }
}

async function postReport(filePath, config, token, endpointPath = config.importPath) {
  const fileBuffer = await fsp.readFile(filePath);
  const fileName = path.basename(filePath);
  const importUrl = resolveApiUrl(config.apiBaseUrl, endpointPath);
  const form = new FormData();
  form.append(config.formFieldName || "report", new Blob([fileBuffer], { type: guessContentType(fileName) }), fileName);

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), Math.max(5000, Number(config.requestTimeoutMs) || 120000));

  try {
    const response = await fetch(importUrl, {
      method: "POST",
      headers: {
        Authorization: `${token.tokenType || "Bearer"} ${token.accessToken}`,
        Accept: "application/json, text/plain, */*"
      },
      body: form,
      signal: controller.signal
    });

    const text = await response.text();
    return { status: response.status, body: text };
  } finally {
    clearTimeout(timeout);
  }
}

function resolveApiUrl(baseUrl, endpointPath) {
  const base = String(baseUrl || "").trim().replace(/\/+$/, "");
  const endpoint = String(endpointPath || "").trim();
  if (!base) throw new Error("CatsBack API Base URL is empty.");

  if (/^https?:\/\//i.test(endpoint)) return endpoint;
  return new URL(endpoint.startsWith("/") ? endpoint : `/${endpoint}`, `${base}/`).toString();
}

function guessContentType(fileName) {
  return /\.csv$/i.test(fileName) ? "text/csv" : "text/plain";
}

function isApiConfigured(config) {
  return Boolean(
    String(config.apiBaseUrl || "").trim() &&
    String(config.tokenPath || "").trim() &&
    String(config.importPath || "").trim() &&
    String(config.settlementImportPath || "").trim() &&
    String(config.clientId || "").trim() &&
    String(config.clientSecret || "")
  );
}

function emptyTokenCache() {
  return { accessToken: "", tokenType: "Bearer", expiresAtMs: 0 };
}

function clearTokenCache() {
  tokenCache = emptyTokenCache();
}

function requireModernNode() {
  if (typeof fetch !== "function" || typeof FormData !== "function" || typeof Blob !== "function") {
    throw new Error("Node.js 18+ is required because this helper uses built-in fetch/FormData/Blob.");
  }
}

function summarizeResponse(text) {
  if (!text) return "";
  try {
    const parsed = JSON.parse(text);
    const keys = [
      "importedRowCount",
      "conversionCount",
      "insertedCount",
      "updatedCount",
      "unmatchedCount",
      "matchedItemCount",
      "unmatchedItemCount",
      "multiTrackingOrderCount",
      "errorCount"
    ];
    const out = {};
    for (const key of keys) {
      if (Object.prototype.hasOwnProperty.call(parsed, key)) out[key] = parsed[key];
    }
    if (Array.isArray(parsed.errors) && parsed.errors.length) {
      out.errors = parsed.errors.slice(0, 20);
    }
    if (Object.keys(out).length) return JSON.stringify(out);
  } catch (_) {}
  return text.replace(/\s+/g, " ").slice(0, 500);
}

async function waitUntilStable(filePath, config) {
  const checks = Math.max(2, Number(config.stableChecks) || 4);
  const interval = Math.max(500, Number(config.stableCheckIntervalMs) || 1500);
  let previousSize = -1;
  let stable = 0;

  while (stable < checks) {
    const stat = await fsp.stat(filePath);
    if (stat.size > 0 && stat.size === previousSize) stable += 1;
    else stable = 0;
    previousSize = stat.size;
    await sleep(interval);
  }
}

async function archive(filePath, configuredDir) {
  const dir = expandEnv(configuredDir || path.join(path.dirname(filePath), "CatsBackArchive"));
  await fsp.mkdir(dir, { recursive: true });
  const ext = path.extname(filePath);
  const base = path.basename(filePath, ext);
  const stamp = new Date().toISOString().replace(/[:.]/g, "-");
  const target = path.join(dir, `${base}_${stamp}${ext}`);
  await fsp.rename(filePath, target);
  await log(`ARCHIVE ${filePath} -> ${target}`);
}

async function sha256(filePath) {
  return new Promise((resolve, reject) => {
    const hash = crypto.createHash("sha256");
    const stream = fs.createReadStream(filePath);
    stream.on("error", reject);
    stream.on("data", chunk => hash.update(chunk));
    stream.on("end", () => resolve(hash.digest("hex")));
  });
}

function validateCanonicalSettlementReport(input) {
  if (!input || input.schemaVersion !== SETTLEMENT_SCHEMA_VERSION) {
    throw new Error("Unsupported settlement schema version.");
  }
  if (!Array.isArray(input.rows) || input.rows.length === 0) {
    throw new Error("Settlement report does not contain any rows.");
  }
  if (input.rows.length > 100000) throw new Error("Settlement report exceeds 100000 rows.");

  const seenOrders = new Set();
  const validations = new Set();
  const rows = input.rows.map((source, index) => {
    if (!source || typeof source !== "object" || Array.isArray(source)) {
      throw new Error(`Settlement row ${index + 1} is invalid.`);
    }
    const row = {};
    for (const header of SETTLEMENT_HEADERS) row[header] = String(source[header] ?? "").trim();
    if (row.schema_version !== SETTLEMENT_SCHEMA_VERSION) {
      throw new Error(`Settlement row ${index + 1} has an invalid schema_version.`);
    }
    if (!/^\d+$/.test(row.source_affiliate_id) || !/^\d+$/.test(row.validation_id)) {
      throw new Error(`Settlement row ${index + 1} has an invalid affiliate/validation id.`);
    }
    if (!row.order_id) throw new Error(`Settlement row ${index + 1} is missing order_id.`);
    for (const field of ["payment_status", "validation_payout_status"]) {
      if (!/^\d+$/.test(row[field])) throw new Error(`Settlement row ${index + 1} has an invalid ${field}.`);
    }
    for (const field of ["overall_validation_status", "bill_validation_status", "settlement_cycle"]) {
      if (row[field] && !/^\d+$/.test(row[field])) {
        throw new Error(`Settlement row ${index + 1} has an invalid ${field}.`);
      }
    }
    for (const field of ["has_adjustment", "has_clawback", "is_cumulative", "has_bonus", "has_ppp"]) {
      if (row[field] !== "true" && row[field] !== "false") {
        throw new Error(`Settlement row ${index + 1} has an invalid ${field}.`);
      }
    }
    if (row.payment_completed_at_utc && !Number.isFinite(Date.parse(row.payment_completed_at_utc))) {
      throw new Error(`Settlement row ${index + 1} has an invalid payment_completed_at_utc.`);
    }
    for (const field of ["order_completed_from_utc", "order_completed_to_utc"]) {
      if (!Number.isFinite(Date.parse(row[field]))) throw new Error(`Settlement row ${index + 1} has an invalid ${field}.`);
    }
    for (const field of [
      "bill_eligible_commission", "bill_after_service_fee", "bill_paid_commission",
      "order_eligible_commission", "allocated_service_fee", "allocated_tax", "actual_paid_commission"
    ]) {
      if (!/^\d+(?:\.\d{1,4})?$/.test(row[field])) {
        throw new Error(`Settlement row ${index + 1} has an invalid ${field}.`);
      }
    }
    const identity = `${row.source_affiliate_id}\u0000${row.validation_id}\u0000${row.order_id}`;
    if (seenOrders.has(identity)) throw new Error(`Settlement row ${index + 1} duplicates an order.`);
    seenOrders.add(identity);
    validations.add(`${row.source_affiliate_id}\u0000${row.validation_id}`);
    return row;
  });

  return { schemaVersion: SETTLEMENT_SCHEMA_VERSION, validationCount: validations.size, rows };
}

async function saveCanonicalSettlementCsv(report, config) {
  const outputDir = expandEnv(config.settlementOutputDir || DEFAULT_CONFIG.settlementOutputDir);
  if (!outputDir) throw new Error("Settlement output directory is empty.");
  await fsp.mkdir(outputDir, { recursive: true });
  const csv = canonicalSettlementCsv(report.rows);
  const stamp = new Date().toISOString().replace(/[-:]/g, "").replace(/\.\d{3}Z$/, "Z");

  for (let attempt = 0; attempt < 100; attempt++) {
    const suffix = attempt === 0 ? "" : `_${attempt}`;
    const filePath = path.join(outputDir, `CatsBackSettlement_${stamp}${suffix}.csv`);
    try {
      await fsp.writeFile(filePath, csv, { encoding: "utf8", flag: "wx" });
      return filePath;
    } catch (error) {
      if (error?.code !== "EEXIST") throw error;
    }
  }
  throw new Error("Could not create a unique settlement CSV file.");
}

function canonicalSettlementCsv(rows) {
  const lines = [SETTLEMENT_HEADERS.map(csvCell).join(",")];
  for (const row of rows) lines.push(SETTLEMENT_HEADERS.map(header => csvCell(row[header])).join(","));
  return `\ufeff${lines.join("\r\n")}\r\n`;
}

function csvCell(value) {
  return `"${String(value ?? "").replace(/"/g, '""')}"`;
}

function parseJsonOrNull(value) {
  try { return JSON.parse(value); } catch (_) { return null; }
}

function summarizeSettlementImport(parsed, rawText) {
  if (parsed && typeof parsed === "object") {
    const parts = [];
    const labels = [
      ["validationCount", "bảng kê"],
      ["updatedValidationCount", "bảng kê cập nhật"],
      ["pendingApprovalCount", "chờ duyệt"],
      ["waitingPaymentCount", "chờ Shopee thanh toán"],
      ["alreadySettledCount", "đã đối soát"],
      ["unmatchedCount", "không khớp"],
      ["errorCount", "lỗi"]
    ];
    for (const [key, label] of labels) {
      if (Object.prototype.hasOwnProperty.call(parsed, key)) parts.push(`${label}: ${parsed[key]}`);
    }
    if (parsed.isDuplicate === true) parts.push("file đã import trước đó");
    if (parts.length) return parts.join(" · ");
  }
  return String(rawText || "").replace(/\s+/g, " ").slice(0, 500);
}

async function loadState() {
  if (!fs.existsSync(STATE_PATH)) return { processed: {} };
  try {
    return JSON.parse(await fsp.readFile(STATE_PATH, "utf8"));
  } catch {
    return { processed: {} };
  }
}

async function saveState(state) {
  await fsp.writeFile(STATE_PATH, JSON.stringify(state, null, 2), "utf8");
}

function expandEnv(value) {
  if (!value) return value;
  return value
    .replace(/%USERPROFILE%/gi, process.env.USERPROFILE || os.homedir())
    .replace(/%HOME%/gi, os.homedir());
}

function startSettingsServer(port) {
  const server = http.createServer(async (req, res) => {
    try {
      if (!allowRequestOrigin(req, res, port)) return;
      if (req.method === "OPTIONS") {
        res.writeHead(204);
        return res.end();
      }

      const url = new URL(req.url, `http://127.0.0.1:${port}`);

      if (req.method === "GET" && url.pathname === "/health") {
        const config = loadConfig();
        return sendJson(res, 200, {
          ok: true,
          version: APP_VERSION,
          apiConfigured: isApiConfigured(config),
          tokenCached: Boolean(tokenCache.accessToken && tokenCache.expiresAtMs > Date.now()),
          tokenExpiresAtUtc: tokenCache.expiresAtMs ? new Date(tokenCache.expiresAtMs).toISOString() : null,
          watchDir: expandEnv(config.watchDir)
        });
      }

      if (req.method === "GET" && url.pathname === "/api/settings") {
        const config = loadConfig();
        return sendJson(res, 200, publicSettings(config));
      }

      if (req.method === "POST" && url.pathname === "/api/settings") {
        const body = await readJsonBody(req, 32768);
        const current = loadConfig();
        const updated = applyApiSettings(current, body);
        await saveConfig(updated);
        clearTokenCache();
        await log(`SETTINGS_UPDATED baseUrl=${updated.apiBaseUrl} clientId=${Boolean(updated.clientId)} clientSecret=${Boolean(updated.clientSecret)}`);
        return sendJson(res, 200, { ok: true, ...publicSettings(updated) });
      }

      if (req.method === "POST" && url.pathname === "/api/test-connection") {
        const config = loadConfig();
        if (!isApiConfigured(config)) {
          return sendJson(res, 400, { ok: false, error: "Client ID/Client Secret chua duoc cau hinh day du." });
        }
        const token = await getAccessToken(config, true);
        return sendJson(res, 200, {
          ok: true,
          tokenType: token.tokenType,
          expiresAtUtc: new Date(token.expiresAtMs).toISOString()
        });
      }

      if (req.method === "POST" &&
        (url.pathname === "/api/settlements/export" || url.pathname === "/api/settlements/import")) {
        const body = await readJsonBody(req, 64 * 1024 * 1024);
        const report = validateCanonicalSettlementReport(body);
        const config = loadConfig();
        const filePath = await saveCanonicalSettlementCsv(report, config);
        const shouldImport = url.pathname.endsWith("/import");
        await log(`SETTLEMENT_${shouldImport ? "IMPORT" : "EXPORT"}_FILE validations=${report.validationCount} rows=${report.rows.length} file=${filePath}`);

        if (!shouldImport) {
          return sendJson(res, 200, {
            ok: true,
            validationCount: report.validationCount,
            rowCount: report.rows.length,
            filePath
          });
        }
        if (!isApiConfigured(config)) {
          return sendJson(res, 400, {
            ok: false,
            error: "Client ID/Client Secret CatsBack chua duoc cau hinh.",
            filePath
          });
        }

        const upload = await uploadSettlementReport(filePath, config);
        const parsed = parseJsonOrNull(upload.body);
        const importSummary = summarizeSettlementImport(parsed, upload.body);
        await log(`SETTLEMENT_IMPORT_OK status=${upload.status} rows=${report.rows.length}${importSummary ? ` response=${importSummary}` : ""}`);
        return sendJson(res, 200, {
          ok: true,
          validationCount: report.validationCount,
          rowCount: report.rows.length,
          filePath,
          importSummary,
          importResult: parsed
        });
      }

      if (req.method === "GET" && (url.pathname === "/" || url.pathname === "/settings")) {
        return sendHtml(res, 200, settingsHtml(port));
      }

      sendJson(res, 404, { ok: false, error: "Not found" });
    } catch (error) {
      await log(`SETTINGS_SERVER_ERROR ${sanitizeError(error.stack || error.message || error)}`);
      sendJson(res, 400, { ok: false, error: sanitizeError(error.message || String(error)) });
    }
  });

  server.listen(port, "127.0.0.1", () => {
    log(`SETTINGS_READY http://127.0.0.1:${port}/settings`).catch(() => {});
  });

  server.on("error", error => {
    log(`SETTINGS_SERVER_LISTEN_ERROR ${sanitizeError(error.message)}`).catch(() => {});
  });
}

function allowRequestOrigin(req, res, port) {
  const origin = req.headers.origin;
  const sameLocal = origin === `http://127.0.0.1:${port}` || origin === `http://localhost:${port}`;
  const extension = typeof origin === "string" && origin.startsWith("chrome-extension://");
  const allowed = !origin || sameLocal || extension;

  if (!allowed) {
    sendJson(res, 403, { ok: false, error: "Origin not allowed" });
    return false;
  }

  if (origin && (sameLocal || extension)) {
    res.setHeader("Access-Control-Allow-Origin", origin);
    res.setHeader("Vary", "Origin");
    res.setHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type");
  }

  return true;
}

function publicSettings(config) {
  return {
    apiBaseUrl: config.apiBaseUrl || DEFAULT_CONFIG.apiBaseUrl,
    tokenPath: config.tokenPath || DEFAULT_CONFIG.tokenPath,
    importPath: config.importPath || DEFAULT_CONFIG.importPath,
    settlementImportPath: config.settlementImportPath || DEFAULT_CONFIG.settlementImportPath,
    settlementOutputDir: expandEnv(config.settlementOutputDir || DEFAULT_CONFIG.settlementOutputDir),
    tokenUrl: resolveApiUrl(config.apiBaseUrl, config.tokenPath),
    importUrl: resolveApiUrl(config.apiBaseUrl, config.importPath),
    settlementImportUrl: resolveApiUrl(config.apiBaseUrl, config.settlementImportPath),
    clientId: config.clientId || "",
    hasClientSecret: Boolean(config.clientSecret),
    apiConfigured: isApiConfigured(config),
    formFieldName: config.formFieldName || "report",
    tokenRefreshSkewSeconds: Number(config.tokenRefreshSkewSeconds) || 60,
    watchDir: expandEnv(config.watchDir)
  };
}

function applyApiSettings(current, input) {
  const next = { ...current };

  next.apiBaseUrl = validateHttpUrl(String(input.apiBaseUrl ?? current.apiBaseUrl ?? DEFAULT_CONFIG.apiBaseUrl).trim(), "API Base URL");
  next.tokenPath = validateEndpoint(String(input.tokenPath ?? current.tokenPath ?? DEFAULT_CONFIG.tokenPath).trim(), "Token endpoint");
  next.importPath = validateEndpoint(String(input.importPath ?? current.importPath ?? DEFAULT_CONFIG.importPath).trim(), "Import endpoint");
  next.settlementImportPath = validateEndpoint(String(input.settlementImportPath ?? current.settlementImportPath ?? DEFAULT_CONFIG.settlementImportPath).trim(), "Settlement import endpoint");
  next.settlementOutputDir = String(input.settlementOutputDir ?? current.settlementOutputDir ?? DEFAULT_CONFIG.settlementOutputDir).trim() || DEFAULT_CONFIG.settlementOutputDir;
  next.clientId = String(input.clientId ?? current.clientId ?? "").trim();

  if (input.clearClientSecret === true) next.clientSecret = "";
  else if (typeof input.clientSecret === "string" && input.clientSecret.length > 0) next.clientSecret = input.clientSecret;

  next.formFieldName = String(input.formFieldName ?? current.formFieldName ?? "report").trim() || "report";
  next.tokenRefreshSkewSeconds = Math.max(0, Math.min(600, Number(input.tokenRefreshSkewSeconds ?? current.tokenRefreshSkewSeconds ?? 60) || 60));

  return next;
}

function validateHttpUrl(value, label) {
  if (!value) throw new Error(`${label} is required.`);
  const parsed = new URL(value);
  if (!/^https?:$/.test(parsed.protocol)) throw new Error(`${label} must use http or https.`);
  return value.replace(/\/+$/, "");
}

function validateEndpoint(value, label) {
  if (!value) throw new Error(`${label} is required.`);
  if (/^https?:\/\//i.test(value)) {
    validateHttpUrl(value, label);
    return value;
  }
  return value.startsWith("/") ? value : `/${value}`;
}

function readJsonBody(req, maxBytes) {
  return new Promise((resolve, reject) => {
    let total = 0;
    const chunks = [];
    req.on("data", chunk => {
      total += chunk.length;
      if (total > maxBytes) {
        reject(new Error("Request body too large"));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => {
      try {
        const raw = Buffer.concat(chunks).toString("utf8") || "{}";
        resolve(JSON.parse(raw));
      } catch {
        reject(new Error("Invalid JSON"));
      }
    });
    req.on("error", reject);
  });
}

function sendJson(res, status, value) {
  if (res.headersSent) return;
  const body = JSON.stringify(value);
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body),
    "Cache-Control": "no-store"
  });
  res.end(body);
}

function sendHtml(res, status, html) {
  const body = Buffer.from(html, "utf8");
  res.writeHead(status, {
    "Content-Type": "text/html; charset=utf-8",
    "Content-Length": body.length,
    "Cache-Control": "no-store"
  });
  res.end(body);
}

function settingsHtml(port) {
  return `<!doctype html>
<html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>CatsBack Sync Helper Settings</title>
<style>
body{font-family:Arial,sans-serif;max-width:820px;margin:32px auto;padding:0 18px;color:#1f2937}h1{font-size:24px}h2{font-size:18px;margin-top:28px;padding-top:20px;border-top:1px solid #e5e7eb}label{display:block;font-weight:600;margin-top:16px}input{box-sizing:border-box;width:100%;margin-top:6px;padding:10px;border:1px solid #d1d5db;border-radius:8px}.row{display:flex;gap:16px}.row>div{flex:1}.check{display:flex;gap:8px;align-items:center;font-weight:400}.check input{width:auto;margin:0}button{margin-top:20px;padding:10px 16px;border:0;border-radius:8px;background:#111827;color:#fff;cursor:pointer}button.secondary{background:#e5e7eb;color:#111827;margin-left:8px}.hint{font-size:12px;color:#6b7280;margin-top:4px;line-height:1.45}.status{margin-top:16px;padding:10px 12px;background:#f3f4f6;border-radius:8px;white-space:pre-wrap}</style></head>
<body>
<h1>CatsBack Sync Helper Settings v0.7.4</h1>
<p>Helper tu lay Bearer token ngan han bang Client ID/Client Secret. Access token chi duoc giu trong RAM va tu refresh khi het han.</p>
<label>CatsBack API Base URL<input id="apiBaseUrl"></label>
<div class="row"><div><label>Client ID<input id="clientId" autocomplete="off"></label></div><div><label>Client Secret<input id="clientSecret" type="password" autocomplete="new-password" placeholder="De trong de giu secret cu"></label></div></div>
<div id="secretState" class="hint"></div>
<label class="check"><input id="clearClientSecret" type="checkbox"> Xoa Client Secret dang luu</label>
<h2>Endpoint</h2>
<label>Token endpoint<input id="tokenPath"></label>
<label>Conversion import endpoint<input id="importPath"></label>
<label>Settlement import endpoint<input id="settlementImportPath"></label>
<label>Thu muc luu CSV doi soat<input id="settlementOutputDir"></label>
<div class="row"><div><label>Form field name<input id="formFieldName"></label></div><div><label>Refresh token truoc khi het han (giay)<input id="tokenRefreshSkewSeconds" type="number" min="0" max="600"></label></div></div>
<button id="save">Luu cai dat</button><button id="test" class="secondary">Kiem tra ket noi</button><div id="status" class="status">Dang tai...</div>
<script>
async function load(){const r=await fetch('/api/settings');const d=await r.json();apiBaseUrl.value=d.apiBaseUrl||'';clientId.value=d.clientId||'';clientSecret.value='';tokenPath.value=d.tokenPath||'';importPath.value=d.importPath||'';settlementImportPath.value=d.settlementImportPath||'';settlementOutputDir.value=d.settlementOutputDir||'';formFieldName.value=d.formFieldName||'report';tokenRefreshSkewSeconds.value=d.tokenRefreshSkewSeconds??60;secretState.textContent=d.hasClientSecret?'Da co Client Secret. De trong o Secret khi luu de giu gia tri cu.':'Chua co Client Secret.';status.textContent=d.apiConfigured?'Da cau hinh credentials. Helper san sang tu lay token.':'Chua du Client ID/Client Secret.'}
async function saveSettings(){const p={apiBaseUrl:apiBaseUrl.value.trim(),clientId:clientId.value.trim(),clientSecret:clientSecret.value,clearClientSecret:clearClientSecret.checked,tokenPath:tokenPath.value.trim(),importPath:importPath.value.trim(),settlementImportPath:settlementImportPath.value.trim(),settlementOutputDir:settlementOutputDir.value.trim(),formFieldName:formFieldName.value.trim(),tokenRefreshSkewSeconds:Number(tokenRefreshSkewSeconds.value)||60};const r=await fetch('/api/settings',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(p)});const d=await r.json();if(!r.ok){status.textContent=d.error||'Loi luu';return false}clientSecret.value='';clearClientSecret.checked=false;secretState.textContent=d.hasClientSecret?'Da co Client Secret. De trong o Secret khi luu de giu gia tri cu.':'Chua co Client Secret.';status.textContent=d.apiConfigured?'Da luu. Helper san sang tu lay token.':'Da luu nhung chua du Client ID/Client Secret.';return true}
save.onclick=saveSettings;test.onclick=async()=>{test.disabled=true;status.textContent='Dang kiem tra token endpoint...';try{if(!await saveSettings())return;const r=await fetch('/api/test-connection',{method:'POST'});const d=await r.json();status.textContent=r.ok?'Ket noi OK. Token '+d.tokenType+' het han luc '+d.expiresAtUtc:(d.error||'Kiem tra that bai');}catch(e){status.textContent=e.message}finally{test.disabled=false}};load().catch(e=>status.textContent=e.message);
</script></body></html>`;
}

function sanitizeError(value) {
  let text = String(value || "");
  const config = fs.existsSync(CONFIG_PATH) ? (() => { try { return loadConfig(); } catch { return null; } })() : null;
  if (config?.clientSecret) text = text.split(config.clientSecret).join("***");
  if (tokenCache.accessToken) text = text.split(tokenCache.accessToken).join("***");
  return text;
}

async function log(message) {
  await fsp.mkdir(LOG_DIR, { recursive: true });
  const safe = sanitizeError(message);
  const line = `[${new Date().toISOString()}] ${safe}\n`;
  process.stdout.write(line);
  await fsp.appendFile(LOG_PATH, line, "utf8");
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}
