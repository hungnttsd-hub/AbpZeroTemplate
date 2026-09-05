(() => {
  'use strict';

  const userAgent = window.navigator.userAgent;
  const isAndroid = /Android/i.test(userAgent);
  const isIPhone = /iPhone/i.test(userAgent);
  const isStandalone = window.navigator.standalone === true;
  const isSafari = /Safari/i.test(userAgent) && !/(CriOS|FxiOS|EdgiOS|OPiOS)/i.test(userAgent);
  const usesIPhoneDirectNavigation = isIPhone && (isSafari || isStandalone);
  if (!isAndroid && !usesIPhoneDirectNavigation) return;

  const selector = 'a[data-iphone-external-url]';

  const prepareAnchor = (anchor) => {
    if (!(anchor instanceof HTMLAnchorElement)) return null;

    if (isAndroid) {
      anchor.removeAttribute('target');
      return 'android';
    }

    const directUrl = anchor.dataset.iphoneExternalUrl;
    if (!directUrl) return null;

    let externalUrl;
    try {
      externalUrl = new URL(directUrl, window.location.origin);
    } catch {
      return null;
    }

    if (externalUrl.protocol !== 'https:' || externalUrl.origin === window.location.origin) return null;

    anchor.href = externalUrl.href;
    anchor.removeAttribute('target');
    anchor.dataset.iphoneExternalReady = 'true';
    return 'iphone';
  };

  const prepareWithin = (root) => {
    if (!(root instanceof Element || root instanceof Document)) return;
    if (root instanceof Element && root.matches(selector)) prepareAnchor(root);
    root.querySelectorAll(selector).forEach(prepareAnchor);
  };

  const trackClick = (trackingUrl) => {
    if (!trackingUrl) return;

    let url;
    try {
      url = new URL(trackingUrl, window.location.origin);
    } catch {
      return;
    }
    if (url.origin !== window.location.origin || !['http:', 'https:'].includes(url.protocol)) return;

    if (typeof window.navigator.sendBeacon === 'function') {
      const queued = window.navigator.sendBeacon(url.href);
      if (queued) return;
    }

    void window.fetch(url.href, {
      method: 'POST',
      credentials: 'same-origin',
      keepalive: true
    }).catch(() => {});
  };

  prepareWithin(document);

  const observer = new MutationObserver((mutations) => {
    mutations.forEach((mutation) => {
      if (mutation.type === 'attributes') {
        prepareAnchor(mutation.target);
        return;
      }
      mutation.addedNodes.forEach((node) => prepareWithin(node));
    });
  });
  observer.observe(document.documentElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeFilter: ['data-iphone-external-url']
  });

  document.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const anchor = target?.closest(selector);
    if (!anchor || prepareAnchor(anchor) !== 'iphone') return;

    trackClick(anchor.dataset.iphoneTrackUrl);
  }, { capture: true });
})();
