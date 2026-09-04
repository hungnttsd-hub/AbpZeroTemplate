(() => {
  'use strict';

  if (window.CatBackLoading) return;

  const buttonStates = new WeakMap();
  const iconStates = new WeakMap();
  let pageTimer;
  let pageRequestId = 0;

  const pageElements = () => ({
    root: document.querySelector('[data-cb-page-loader]'),
    message: document.querySelector('[data-cb-page-loader-message]')
  });

  const setButtonLoading = (button, loading, options = {}) => {
    if (!(button instanceof HTMLElement)) return;

    if (loading) {
      if (buttonStates.has(button)) return;
      const isInput = button instanceof HTMLInputElement;
      const rect = button.getBoundingClientRect();
      buttonStates.set(button, {
        html: isInput ? null : button.innerHTML,
        value: isInput ? button.value : null,
        isInput,
        minWidth: button.style.minWidth,
        minHeight: button.style.minHeight,
        disabled: 'disabled' in button ? button.disabled : undefined,
        ariaBusy: button.getAttribute('aria-busy')
      });
      if ('disabled' in button) button.disabled = true;
      if (rect.width > 0) button.style.minWidth = `${Math.ceil(rect.width)}px`;
      if (rect.height > 0) button.style.minHeight = `${Math.ceil(rect.height)}px`;
      button.classList.add('cb-is-loading');
      button.setAttribute('aria-busy', 'true');

      if (isInput) {
        button.value = options.text || button.dataset.loadingText || 'Đang xử lý...';
        return;
      }

      const content = document.createElement('span');
      content.className = 'cb-loading-button-content';
      const spinner = document.createElement('span');
      spinner.className = 'cb-ring-spinner cb-ring-spinner--sm';
      spinner.setAttribute('aria-hidden', 'true');
      const label = document.createElement('span');
      label.textContent = options.text || button.dataset.loadingText || 'Đang xử lý...';
      content.append(spinner, label);
      button.replaceChildren(content);
      return;
    }

    const state = buttonStates.get(button);
    if (!state) return;
    if (state.isInput) button.value = state.value;
    else button.innerHTML = state.html;
    if ('disabled' in button && state.disabled !== undefined) button.disabled = state.disabled;
    button.style.minWidth = state.minWidth;
    button.style.minHeight = state.minHeight;
    button.classList.remove('cb-is-loading');
    if (state.ariaBusy === null) button.removeAttribute('aria-busy');
    else button.setAttribute('aria-busy', state.ariaBusy);
    buttonStates.delete(button);
  };

  const setIconLoading = (control, loading, label = 'Đang xử lý') => {
    if (!(control instanceof HTMLElement)) return;

    if (loading) {
      if (iconStates.has(control)) return;
      iconStates.set(control, {
        disabled: 'disabled' in control ? control.disabled : undefined,
        ariaBusy: control.getAttribute('aria-busy'),
        ariaLabel: control.getAttribute('aria-label')
      });
      if ('disabled' in control) control.disabled = true;
      control.classList.add('cb-icon-is-loading');
      control.setAttribute('aria-busy', 'true');
      control.setAttribute('aria-label', label);
      const loader = document.createElement('span');
      loader.className = 'cb-icon-loader';
      loader.setAttribute('aria-hidden', 'true');
      loader.innerHTML = '<i></i><i></i><i></i>';
      control.appendChild(loader);
      return;
    }

    const state = iconStates.get(control);
    if (!state) return;
    control.querySelector(':scope > .cb-icon-loader')?.remove();
    control.classList.remove('cb-icon-is-loading');
    if ('disabled' in control && state.disabled !== undefined) control.disabled = state.disabled;
    if (state.ariaBusy === null) control.removeAttribute('aria-busy');
    else control.setAttribute('aria-busy', state.ariaBusy);
    if (state.ariaLabel === null) control.removeAttribute('aria-label');
    else control.setAttribute('aria-label', state.ariaLabel);
    iconStates.delete(control);
  };

  const showOverlay = (container, message = 'Đang tải...') => {
    if (!(container instanceof HTMLElement)) return null;
    const existing = container.querySelector(':scope > .cb-loading-overlay');
    if (existing) return existing;
    container.classList.add('cb-loading-host');
    container.setAttribute('aria-busy', 'true');
    const overlay = document.createElement('div');
    overlay.className = 'cb-loading-overlay';
    overlay.setAttribute('role', 'status');
    overlay.innerHTML = '<span class="cb-ring-spinner" aria-hidden="true"></span>';
    const label = document.createElement('span');
    label.textContent = message;
    overlay.appendChild(label);
    container.appendChild(overlay);
    return overlay;
  };

  const hideOverlay = (container, overlay) => {
    if (!(container instanceof HTMLElement)) return;
    (overlay || container.querySelector(':scope > .cb-loading-overlay'))?.remove();
    if (!container.querySelector(':scope > .cb-loading-overlay')) {
      container.classList.remove('cb-loading-host');
      container.removeAttribute('aria-busy');
    }
  };

  const showSkeleton = (container, options = {}) => {
    if (!(container instanceof HTMLElement)) return null;
    const existing = container.querySelector(':scope > .cb-skeleton-list');
    if (existing) return existing;
    const skeleton = document.createElement('div');
    skeleton.className = 'cb-skeleton-list';
    skeleton.dataset.cbLoadingGenerated = '';
    skeleton.setAttribute('aria-hidden', 'true');
    const rowCount = Math.min(5, Math.max(1, Number(options.rows) || 3));
    for (let index = 0; index < rowCount; index += 1) {
      const row = document.createElement('div');
      row.className = 'cb-skeleton-row';
      row.innerHTML = '<span class="cb-skeleton cb-skeleton-thumb"></span><span class="cb-skeleton-copy"><i class="cb-skeleton-line"></i><i class="cb-skeleton-line"></i><i class="cb-skeleton-line"></i></span>';
      skeleton.appendChild(row);
    }
    container.setAttribute('aria-busy', 'true');
    container.appendChild(skeleton);
    return skeleton;
  };

  const hideSkeleton = (container, skeleton) => {
    skeleton?.remove();
    if (container instanceof HTMLElement && !container.querySelector(':scope > .cb-skeleton-list')) {
      container.removeAttribute('aria-busy');
    }
  };

  const showPage = (message = 'Đang tải dữ liệu...', options = {}) => {
    const requestId = ++pageRequestId;
    window.clearTimeout(pageTimer);
    pageTimer = window.setTimeout(() => {
      if (requestId !== pageRequestId) return;
      const elements = pageElements();
      if (!elements.root) return;
      if (elements.message) elements.message.textContent = message;
      elements.root.hidden = false;
      elements.root.setAttribute('aria-hidden', 'false');
      document.body.classList.add('cb-page-is-loading');
    }, Math.max(0, Number(options.delay ?? 160)));
    return requestId;
  };

  const hidePage = () => {
    pageRequestId += 1;
    window.clearTimeout(pageTimer);
    const elements = pageElements();
    if (elements.root) {
      elements.root.hidden = true;
      elements.root.setAttribute('aria-hidden', 'true');
    }
    document.body.classList.remove('cb-page-is-loading');
  };

  const showProcessing = (options = {}) => {
    const root = document.querySelector('[data-cb-processing-loader]');
    if (!root) return null;
    const token = String(Date.now()) + Math.random().toString(16).slice(2);
    root.dataset.token = token;
    root.querySelector('[data-cb-processing-title]').textContent = options.title || 'Đang xử lý...';
    root.querySelector('[data-cb-processing-message]').textContent = options.message || 'Vui lòng không tắt cửa sổ này';
    root.hidden = false;
    root.setAttribute('aria-hidden', 'false');
    document.body.classList.add('cb-processing-is-open');
    return token;
  };

  const hideProcessing = (token) => {
    const root = document.querySelector('[data-cb-processing-loader]');
    if (!root || (token && root.dataset.token !== token)) return;
    root.hidden = true;
    root.setAttribute('aria-hidden', 'true');
    delete root.dataset.token;
    document.body.classList.remove('cb-processing-is-open');
  };

  const beginLongTask = (options = {}) => {
    let token;
    let finished = false;
    const timer = window.setTimeout(() => {
      if (!finished) token = showProcessing(options);
    }, Math.max(1000, Number(options.delay ?? 2500)));
    return () => {
      finished = true;
      window.clearTimeout(timer);
      if (token) hideProcessing(token);
    };
  };

  const shouldLoadPageForLink = (event, anchor) => {
    if (!anchor || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
    if (anchor.target && anchor.target !== '_self' || anchor.hasAttribute('download')) return false;
    const url = new URL(anchor.href, window.location.href);
    if (url.origin !== window.location.origin || !['http:', 'https:'].includes(url.protocol)) return false;
    return url.pathname !== window.location.pathname || url.search !== window.location.search;
  };

  window.CatBackLoading = Object.freeze({
    setButtonLoading,
    setIconLoading,
    showOverlay,
    hideOverlay,
    showSkeleton,
    hideSkeleton,
    showPage,
    hidePage,
    showProcessing,
    hideProcessing,
    beginLongTask
  });

  document.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const anchor = target?.closest('a[href]');
    if (shouldLoadPageForLink(event, anchor)) showPage('Đang tải trang...');
  });

  document.addEventListener('submit', (event) => {
    window.requestAnimationFrame(() => {
      if (event.defaultPrevented) return;
      const form = event.target instanceof HTMLFormElement ? event.target : null;
      if (!form) return;
      const submitter = event.submitter || form.querySelector('button[type="submit"], input[type="submit"]');
      setButtonLoading(submitter, true, { text: form.dataset.loadingText || 'Đang xử lý...' });
      showPage(form.dataset.pageLoadingText || 'Đang xử lý yêu cầu...', { delay: 220 });
    });
  });

  document.addEventListener('turbo:before-visit', () => showPage('Đang tải trang...'));
  document.addEventListener('turbo:before-cache', hidePage);
  document.addEventListener('turbo:before-fetch-request', (event) => {
    const form = event.target instanceof HTMLFormElement ? event.target : null;
    showPage(form?.dataset.pageLoadingText || (form ? 'Đang xử lý yêu cầu...' : 'Đang tải trang...'));
  });
  document.addEventListener('turbo:before-render', hidePage);
  document.addEventListener('turbo:fetch-request-error', hidePage);
  document.addEventListener('turbo:submit-end', (event) => {
    if (!event.detail?.success) hidePage();
  });
  document.addEventListener('turbo:load', hidePage);
  window.addEventListener('pageshow', hidePage);
})();
