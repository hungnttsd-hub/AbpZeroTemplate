(() => {
  const selectAll = document.querySelector('[data-authorization-select-all]');
  const permissions = [...document.querySelectorAll('[data-authorization-permission]')];
  if (!selectAll || !permissions.length) return;

  const syncSelectAll = () => {
    const count = permissions.filter(permission => permission.checked).length;
    selectAll.checked = count === permissions.length;
    selectAll.indeterminate = count > 0 && count < permissions.length;
  };

  selectAll.addEventListener('change', () => {
    permissions.forEach(permission => { permission.checked = selectAll.checked; });
    syncSelectAll();
  });
  permissions.forEach(permission => permission.addEventListener('change', syncSelectAll));
  syncSelectAll();
})();
