(() => {
  'use strict';

  if (window.CatBackSpa) return;

  const mounts = new Map();
  const hardReloadPath = /^\/(?:api|hangfire|swagger)(?:\/|$)/i;
  let pendingVisitUrl = null;
  let previousVisitUrl = null;
  let hasLoaded = false;
  let shouldFocusValidation = false;
  let errorPromptVisible = false;

  const asUrl = (value) => {
    try {
      return new URL(value, window.location.href);
    } catch {
      return null;
    }
  };

  const isHardReloadUrl = (value) => {
    const url = asUrl(value);
    return !url || url.origin !== window.location.origin || hardReloadPath.test(url.pathname);
  };

  const cleanup = (name) => {
    const mounted = mounts.get(name);
    if (!mounted) return;

    mounted.controller.abort();
    try {
      mounted.dispose?.();
    } catch (error) {
      console.error(`Unable to dispose CatBack page module '${name}'.`, error);
    }
    mounts.delete(name);
  };

  const cleanupAll = () => {
    Array.from(mounts.keys()).forEach(cleanup);
  };

  const visit = (value, action = 'advance') => {
    const url = asUrl(value);
    if (!url) return;

    if (!isHardReloadUrl(url.href) && window.Turbo) {
      window.Turbo.visit(url.href, { action: action === 'replace' ? 'replace' : 'advance' });
      return;
    }

    if (action === 'replace') {
      window.location.replace(url.href);
    } else {
      window.location.assign(url.href);
    }
  };

  const back = (fallbackUrl = '/', expectedPathPrefix = null) => {
    const previous = asUrl(previousVisitUrl);
    const canRestore = previous &&
      previous.origin === window.location.origin &&
      (!expectedPathPrefix || previous.pathname.startsWith(expectedPathPrefix));

    if (canRestore) {
      window.history.back();
      return;
    }

    visit(fallbackUrl, 'replace');
  };

  const mount = (name, initializer) => {
    if (!name || typeof initializer !== 'function') return;

    cleanup(name);
    const controller = new AbortController();
    const mounted = { controller, dispose: null };
    mounts.set(name, mounted);

    try {
      const dispose = initializer({
        signal: controller.signal,
        visit,
        back
      });
      if (typeof dispose === 'function') mounted.dispose = dispose;
    } catch (error) {
      cleanup(name);
      console.error(`Unable to initialize CatBack page module '${name}'.`, error);
    }
  };

  const setTurboHeader = (event) => {
    const headers = event.detail?.fetchOptions?.headers;
    if (!headers) return;

    if (headers instanceof Headers) {
      headers.set('X-CatBack-Turbo', '1');
    } else {
      headers['X-CatBack-Turbo'] = '1';
    }
  };

  const focusAfterRender = () => {
    if (!hasLoaded) {
      hasLoaded = true;
      return;
    }

    window.requestAnimationFrame(() => {
      if (shouldFocusValidation) {
        shouldFocusValidation = false;
        const validationTarget = document.querySelector(
          '[data-valmsg-summary="true"]:not(:empty), .validation-summary-errors, ' +
          '.field-validation-error:not(:empty), .field-error:not(:empty), ' +
          '.status-message.status-error, [aria-invalid="true"]'
        );
        if (validationTarget instanceof HTMLElement) {
          validationTarget.tabIndex = -1;
          validationTarget.focus({ preventScroll: false });
          return;
        }
      }

      if (window.location.hash || document.documentElement.dataset.turboVisitDirection === 'back') return;
      const main = document.getElementById('main-content');
      main?.focus({ preventScroll: true });
    });
  };

  const showFetchError = async () => {
    if (errorPromptVisible) return;
    errorPromptVisible = true;
    const retryUrl = pendingVisitUrl || window.location.href;
    pendingVisitUrl = null;

    try {
      const retry = window.CatsBackModal?.confirm
        ? await window.CatsBackModal.confirm({
            title: 'Không thể tải trang',
            message: 'Kết nối tới máy chủ bị gián đoạn. Bạn có muốn tải lại bằng trình duyệt không?',
            confirmText: 'Tải lại',
            cancelText: 'Để sau'
          })
        : window.confirm('Không thể tải trang. Bạn có muốn tải lại không?');

      if (retry) window.location.assign(retryUrl);
    } finally {
      errorPromptVisible = false;
    }
  };

  window.CatBackSpa = Object.freeze({
    mount,
    cleanup,
    visit,
    back,
    isHardReloadUrl
  });

  document.addEventListener('turbo:before-fetch-request', setTurboHeader);
  document.addEventListener('turbo:before-fetch-response', (event) => {
    shouldFocusValidation = event.detail?.fetchResponse?.response?.status === 422;
  });
  document.addEventListener('turbo:before-visit', (event) => {
    const targetUrl = event.detail?.url;
    if (!targetUrl) return;

    if (isHardReloadUrl(targetUrl)) {
      event.preventDefault();
      window.location.assign(targetUrl);
      return;
    }

    previousVisitUrl = window.location.href;
    pendingVisitUrl = targetUrl;
  });
  document.addEventListener('turbo:before-cache', cleanupAll);
  document.addEventListener('turbo:before-render', (event) => {
    cleanupAll();
    const newBody = event.detail?.newBody;
    if (newBody instanceof HTMLBodyElement && !newBody.hasAttribute('data-catback-spa-shell')) {
      event.preventDefault();
      window.location.assign(pendingVisitUrl || window.location.href);
    }
  });
  document.addEventListener('turbo:submit-end', (event) => {
    shouldFocusValidation ||= event.detail?.fetchResponse?.response?.status === 422;
  });
  document.addEventListener('turbo:fetch-request-error', (event) => {
    event.preventDefault();
    void showFetchError();
  });
  document.addEventListener('turbo:render', () => {
    if (shouldFocusValidation) focusAfterRender();
  });
  document.addEventListener('turbo:load', () => {
    pendingVisitUrl = null;
    focusAfterRender();
  });
})();
