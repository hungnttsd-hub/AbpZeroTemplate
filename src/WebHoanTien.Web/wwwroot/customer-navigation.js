(() => {
  const links = document.querySelectorAll('[data-open-in-new-tab-on-desktop]');
  const desktopViewport = window.matchMedia('(min-width: 768px)');

  const updateLinkTargets = () => {
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
})();
