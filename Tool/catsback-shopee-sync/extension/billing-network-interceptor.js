(() => {
  const STATE_KEY = "__catsBackBillingListNetworkCaptureV1";
  const BILLING_LIST_PATH = "/api/v3/payment/billing_list";
  const existing = window[STATE_KEY];
  if (existing?.installed) return;

  const state = { installed: true, latest: null };
  Object.defineProperty(window, STATE_KEY, {
    value: state,
    configurable: false,
    enumerable: false,
    writable: false
  });

  const normalizeUrl = value => {
    try {
      if (value instanceof Request) return new URL(value.url, location.href).toString();
      return new URL(String(value || ""), location.href).toString();
    } catch (_) {
      return "";
    }
  };

  const isBillingListUrl = value => {
    const normalized = normalizeUrl(value);
    if (!normalized) return false;
    try {
      const url = new URL(normalized);
      return url.origin === location.origin && url.pathname === BILLING_LIST_PATH;
    } catch (_) {
      return false;
    }
  };

  const record = (url, status, payload, error = "") => {
    state.latest = {
      url: normalizeUrl(url),
      status: Number(status) || 0,
      capturedAt: new Date().toISOString(),
      payload: payload && typeof payload === "object" ? payload : null,
      error: String(error || "")
    };
  };

  const nativeFetch = window.fetch;
  if (typeof nativeFetch === "function") {
    window.fetch = async function (...args) {
      const requestUrl = normalizeUrl(args[0]);
      const response = await nativeFetch.apply(this, args);
      if (isBillingListUrl(requestUrl)) {
        void response.clone().json()
          .then(payload => record(response.url || requestUrl, response.status, payload))
          .catch(error => record(response.url || requestUrl, response.status, null, error?.message || error));
      }
      return response;
    };
  }

  const xhrUrl = Symbol("catsBackBillingListUrl");
  const nativeOpen = XMLHttpRequest.prototype.open;
  const nativeSend = XMLHttpRequest.prototype.send;

  XMLHttpRequest.prototype.open = function (method, url, ...args) {
    this[xhrUrl] = normalizeUrl(url);
    return nativeOpen.call(this, method, url, ...args);
  };

  XMLHttpRequest.prototype.send = function (...args) {
    const requestUrl = this[xhrUrl];
    if (isBillingListUrl(requestUrl)) {
      this.addEventListener("loadend", () => {
        try {
          const payload = this.responseType === "json"
            ? this.response
            : JSON.parse(this.responseText || "null");
          record(this.responseURL || requestUrl, this.status, payload);
        } catch (error) {
          record(this.responseURL || requestUrl, this.status, null, error?.message || error);
        }
      }, { once: true });
    }
    return nativeSend.apply(this, args);
  };
})();
