(() => {
  document.querySelectorAll('[data-demo-form]').forEach(form => {
    form.addEventListener('submit', event => {
      if (form.dataset.submitting) { event.preventDefault(); return; }
      form.dataset.submitting = 'true';
      const button = form.querySelector('[data-loading-label]');
      button.disabled = true;
      button.setAttribute('aria-busy', 'true');
      button.textContent = button.dataset.loadingLabel;
    });
  });
  // A back/forward cache restore must not leave the previous submit button disabled.
  window.addEventListener('pageshow', event => { if (event.persisted) window.location.reload(); });
})();
