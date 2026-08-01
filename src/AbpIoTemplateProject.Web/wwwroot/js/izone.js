(() => {
  const menuButton = document.querySelector('.iz-menu-toggle');
  const menu = document.querySelector('#iz-menu');
  const toTop = document.querySelector('.iz-to-top');
  menuButton?.addEventListener('click', () => {
    const open = menu.classList.toggle('is-open');
    menuButton.setAttribute('aria-expanded', String(open));
  });
  menu?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
    menu.classList.remove('is-open');
    menuButton?.setAttribute('aria-expanded', 'false');
  }));
  window.addEventListener('scroll', () => toTop?.classList.toggle('is-visible', window.scrollY > 500), { passive: true });
  toTop?.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
  document.querySelector('[data-izone-form]')?.addEventListener('submit', event => {
    event.preventDefault();
    const message = event.currentTarget.querySelector('.iz-form-message');
    message.textContent = 'Cảm ơn bạn! IZONE sẽ liên hệ tư vấn trong thời gian sớm nhất.';
    event.currentTarget.reset();
  });
})();
