(() => {
  if (window.__catBackNavigationInitialized) return;
  window.__catBackNavigationInitialized = true;

  const desktopViewport = window.matchMedia('(min-width: 768px)');

  const updateLinkTargets = () => {
    const links = document.querySelectorAll('[data-open-in-new-tab-on-desktop]');
    links.forEach((link) => {
      if (desktopViewport.matches) {
        link.target = '_blank';
      } else {
        link.removeAttribute('target');
      }
    });
  };

  updateLinkTargets();
  desktopViewport.addEventListener('change', updateLinkTargets);
  document.addEventListener('turbo:load', updateLinkTargets);
})();
