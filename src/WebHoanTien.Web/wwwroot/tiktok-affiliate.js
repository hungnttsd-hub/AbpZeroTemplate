(() => {
  if (window.catBackTikTokAffiliate) { window.catBackTikTokAffiliate.init(); return; }
  function init() {
    const root = document.querySelector('[data-tiktok-affiliate]');
    if (!root || root.dataset.initialized) return;
    root.dataset.initialized = 'true';
    const find = selector => root.querySelector(selector);
    const state = JSON.parse(find('[data-tt-initial]').textContent);
    let generatedLink = null;
    let revision = 0;
    let busy = false;
    const currency = (value, code = '') => value == null ? '—' :
      new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value) + (code ? ' ' + code : '');
    const date = value => new Intl.DateTimeFormat('vi-VN', {
      dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Ho_Chi_Minh'
    }).format(new Date(value));
    const text = (selector, value) => { find(selector).textContent = value; };
    const statusLabel = value => ({
      Pending: 'Chờ xử lý', Settled: 'Đã chốt hoa hồng',
      Cancelled: 'Đã hủy / hoàn', Generated: 'Đã tạo', ProductUrl: 'Link sản phẩm gốc'
    }[value] || value);

    function feedback(message, kind = 'error') {
      const element = find('[data-tt-feedback]');
      element.hidden = !message; element.textContent = message; element.dataset.kind = kind;
    }
    function enableWorkflow() {
      find('#tt-product-url').disabled = !state.creator;
      find('#tt-product-url').readOnly = busy;
      find('[data-tt-create]').disabled = !state.creator || busy;
      root.querySelectorAll('[data-tt-hide-link], [data-tt-refresh-links], [data-tt-refresh-orders]')
        .forEach(button => { button.disabled = busy; });
    }
    async function request(handler, values = null) {
      const options = { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } };
      if (values) {
        options.method = 'POST';
        const body = new URLSearchParams(values);
        body.set('__RequestVerificationToken', find('input[name="__RequestVerificationToken"]').value);
        options.body = body;
      }
      const response = await fetch('/tiktok-affiliate?handler=' + handler, options);
      if (response.redirected || response.status === 401 || response.status === 403)
        throw new Error('Phiên CatBack đã hết hạn hoặc bạn chưa được cấp quyền. Đăng nhập lại để tiếp tục.');
      const payload = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error((typeof payload?.error === 'string' ? payload.error : payload?.error?.message) || 'Không thể hoàn tất thao tác. Vui lòng thử lại.');
      if (!payload || !('data' in payload)) throw new Error('Phản hồi không hợp lệ. Vui lòng tải lại trang.');
      return payload.data;
    }
    async function run(button, label, action) {
      if (busy) return;
      busy = true; feedback(''); enableWorkflow();
      const previous = button.textContent;
      button.textContent = label; button.setAttribute('aria-busy', 'true');
      try { await action(); } catch (error) { feedback(error.message); }
      finally { busy = false; button.textContent = previous; button.removeAttribute('aria-busy'); enableWorkflow(); }
    }
    function safeSharingUrl(value) {
      try {
        const url = new URL(value, window.location.origin);
        if (url.username || url.password) return null;
        if (url.origin === window.location.origin && /^\/go\/cb_[a-f0-9]{32}$/.test(url.pathname)) return url.href;
        return url.protocol === 'https:' && (url.hostname === 'tiktok.com' || url.hostname.endsWith('.tiktok.com')) ? url.href : null;
      } catch { return null; }
    }
    async function copy(value) {
      const url = safeSharingUrl(value);
      if (!url) { feedback('Liên kết không hợp lệ.'); return; }
      try {
        if (navigator.clipboard?.writeText) await navigator.clipboard.writeText(url);
        else {
          const input = document.createElement('textarea');
          input.value = url; input.style.position = 'fixed'; input.style.opacity = '0';
          root.append(input); input.select();
          const copied = document.execCommand('copy'); input.remove();
          if (!copied) throw new Error('copy unavailable');
        }
        feedback('Đã sao chép liên kết TikTok.', 'success');
      } catch { feedback('Không thể tự động sao chép. Hãy chọn và sao chép liên kết thủ công.'); }
    }
    function cell(row, value) {
      const td = document.createElement('td'); td.textContent = value; row.append(td); return td;
    }
    function badge(td, value) {
      const span = document.createElement('span'); span.className = 'tt-status';
      const style = { Pending: 'pending', Settled: 'settled', Cancelled: 'cancelled', Generated: 'generated' }[value];
      if (style) span.classList.add('is-' + style);
      span.textContent = statusLabel(value); td.append(span);
    }
    function clearResult() {
      revision++; generatedLink = null;
      find('[data-tt-product]').hidden = true;
      find('[data-tt-result]').hidden = true;
      find('[data-tt-result-placeholder]').hidden = false;
      find('#tt-sharing-link').value = '';
    }
    function renderProduct(link) {
      const product = link.product;
      text('[data-tt-product-title]', product?.title || link.productTitle);
      text('[data-tt-product-shop]', product?.shopName || '');
      text('[data-tt-product-price]', product?.price != null ? currency(product.price, product.currency) : 'Giá chưa được cung cấp');
      text('[data-tt-product-commission]', product?.commissionAmount != null
        ? currency(product.commissionAmount, product.currency)
        : product?.commissionRate != null
        ? new Intl.NumberFormat('vi-VN', { style: 'percent', maximumFractionDigits: 2 }).format(product.commissionRate)
        : 'Chưa có dữ liệu');
      text('[data-tt-rate-note]', product?.commissionSource === 'configured'
        ? link.status === 'ProductUrl'
          ? 'Tỷ lệ tham khảo từ đơn của nhà sáng tạo khác; hoa hồng thực tế có thể thay đổi.'
          : 'Hoa hồng cố định theo cấu hình sản phẩm; số tiền thực nhận được đối soát từ đơn RioHub.'
        : product?.commissionSource === 'global'
        ? 'Tỷ lệ tham khảo từ đơn của nhà sáng tạo khác; hoa hồng thực tế có thể thay đổi.'
        : product?.commissionSource === 'creator'
          ? 'Tỷ lệ từ đơn gần nhất của nhà sáng tạo; hoa hồng thực tế có thể thay đổi.'
          : 'Tỷ lệ tham khảo theo dữ liệu sản phẩm, có thể chưa gồm hoa hồng thưởng.');
      const image = find('[data-tt-product-image]');
      let imageUrl = null;
      try {
        if (product?.imageUrl) {
          const url = new URL(product.imageUrl, window.location.origin);
          if (url.protocol === 'https:' || (url.origin === window.location.origin && url.pathname.startsWith('/products/tiktok/')))
            imageUrl = url.href;
        }
      } catch { /* Metadata is best-effort. */ }
      image.hidden = !imageUrl;
      image.onerror = () => { image.hidden = true; find('[data-tt-product]').classList.add('has-no-image'); };
      if (imageUrl) { image.src = imageUrl; image.alt = product.title; }
      else image.removeAttribute('src');
      find('[data-tt-product]').classList.toggle('has-no-image', !imageUrl);
      find('[data-tt-product]').hidden = false;
    }
    function renderLinks() {
      const links = state.links || [];
      text('[data-tt-link-count]', links.length);
      find('[data-tt-links-empty]').hidden = links.length > 0;
      find('[data-tt-links-table]').hidden = !links.length;
      const body = find('[data-tt-links-body]'); body.replaceChildren();
      links.forEach(link => {
        const row = document.createElement('tr');
        const title = cell(row, link.productTitle); const note = document.createElement('small');
        note.textContent = link.productId; title.append(note);
        cell(row, date(link.createdAt)); badge(cell(row, ''), link.status);
        const actions = cell(row, ''); const container = document.createElement('div'); container.className = 'tt-table-actions';
        const url = safeSharingUrl(link.sharingLink);
        if (url) {
          const open = document.createElement('a'); open.href = url; open.target = '_blank';
          open.rel = 'noopener noreferrer'; open.referrerPolicy = 'no-referrer'; open.dataset.turbo = 'false';
          open.textContent = 'Mở liên kết ↗'; open.setAttribute('aria-label', 'Mở liên kết của ' + link.productTitle);
          container.append(open);
          const button = document.createElement('button'); button.type = 'button'; button.textContent = 'Sao chép';
          button.setAttribute('aria-label', 'Sao chép liên kết của ' + link.productTitle);
          button.addEventListener('click', () => copy(link.sharingLink)); container.append(button);
        }
        if (link.trackingId) {
          const hide = document.createElement('button'); hide.type = 'button'; hide.textContent = 'Ẩn liên kết';
          hide.dataset.ttHideLink = ''; hide.disabled = busy;
          hide.setAttribute('aria-label', 'Ẩn liên kết của ' + link.productTitle);
          hide.addEventListener('click', () => run(hide, 'Đang ẩn...', async () => {
            state.links = await request('DeleteLink', { trackingId: link.trackingId });
            if (generatedLink?.trackingId === link.trackingId) clearResult();
            renderLinks(); feedback('Đã ẩn liên kết khỏi danh sách.', 'success');
          }));
          container.append(hide);
        }
        actions.append(container); body.append(row);
      });
    }
    function renderOrders() {
      const orders = state.orders || [];
      const orderCount = new Set(orders.map(order => order.orderId)).size;
      text('[data-tt-orders-count]', orderCount + ' đơn / ' + orders.length + ' SKU');
      const error = find('[data-tt-orders-error]');
      error.hidden = !state.ordersError; error.textContent = state.ordersError || '';
      find('[data-tt-orders-empty]').hidden = orders.length > 0 || !!state.ordersError;
      find('[data-tt-orders-table]').hidden = !orders.length;
      const body = find('[data-tt-orders-body]'); body.replaceChildren();
      orders.forEach(order => {
        const row = document.createElement('tr'); const id = cell(row, order.orderId);
        const sku = document.createElement('small'); sku.textContent = 'SKU: ' + order.skuId; id.append(sku);
        const source = document.createElement('small');
        source.textContent = order.source === 'Manual' ? 'Tạo thủ công' : 'RioHub'; id.append(source);
        const product = cell(row, order.product);
        product.className = 'tt-order-product';
        if (order.productId) { const productId = document.createElement('small'); productId.textContent = 'ID: ' + order.productId; product.append(productId); }
        cell(row, currency(order.orderAmount, order.currency));
        cell(row, currency(order.estimatedCommission, order.currency));
        cell(row, currency(order.actualCommission, order.currency)); badge(cell(row, ''), order.status);
        const created = cell(row, date(order.createdAt));
        if (order.settledAt) { const settled = document.createElement('small'); settled.textContent = 'Chốt: ' + date(order.settledAt); created.append(settled); }
        body.append(row);
      });
    }
    function selectTab(name, focus = false) {
      root.querySelectorAll('[data-tt-tab]').forEach(tab => {
        const active = tab.dataset.ttTab === name; tab.setAttribute('aria-selected', String(active)); tab.tabIndex = active ? 0 : -1;
        find('#tt-' + tab.dataset.ttTab).hidden = !active; if (active && focus) tab.focus();
      });
    }
    const connectForm = find('[data-tt-connect-form]');
    connectForm?.addEventListener('submit', event => {
      if (event.defaultPrevented) return;
      if (connectForm.dataset.submitting === 'true') { event.preventDefault(); return; }
      connectForm.dataset.submitting = 'true';
      const button = connectForm.querySelector('button[type="submit"]');
      if (!button) return;
      connectForm.dataset.submitLabel = button.textContent;
      if (window.CatBackLoading)
        window.CatBackLoading.setButtonLoading(button, true, { text: 'Đang kết nối…' });
      else {
        button.disabled = true;
        button.setAttribute('aria-busy', 'true');
        button.textContent = 'Đang kết nối…';
      }
    });
    root.querySelectorAll('[data-tt-tab]').forEach(tab => {
      tab.addEventListener('click', () => selectTab(tab.dataset.ttTab));
      tab.addEventListener('keydown', event => {
        const tabs = [...root.querySelectorAll('[data-tt-tab]')]; let index = tabs.indexOf(tab);
        if (event.key === 'ArrowRight') index = (index + 1) % tabs.length;
        else if (event.key === 'ArrowLeft') index = (index + tabs.length - 1) % tabs.length;
        else if (event.key === 'Home') index = 0; else if (event.key === 'End') index = tabs.length - 1; else return;
        event.preventDefault(); selectTab(tabs[index].dataset.ttTab, true);
      });
    });
    find('#tt-product-url').addEventListener('input', clearResult);
    find('[data-tt-product-form]').addEventListener('submit', event => {
      event.preventDefault(); if (busy) return;
      clearResult(); const currentRevision = revision;
      run(find('[data-tt-create]'), 'Đang tạo link TikTok...', async () => {
        const result = await request('Generate', { productUrlOrId: find('#tt-product-url').value });
        if (currentRevision !== revision) return;
        const url = safeSharingUrl(result.link.sharingLink);
        if (!url) throw new Error('Liên kết trả về không hợp lệ.');
        generatedLink = result.link; state.links = result.links;
        const productUrlOnly = generatedLink.status === 'ProductUrl';
        find('#tt-sharing-link').value = url;
        text('[data-tt-result-title]', productUrlOnly ? 'Đã lưu link sản phẩm gốc.' : 'Đã tạo liên kết tiếp thị.');
        text('[data-tt-result-note]', productUrlOnly
          ? 'Sản phẩm dùng thông tin mặc định. Chưa tạo link affiliate qua RioHub.'
          : 'Liên kết đã được lưu trong tài khoản CatBack của bạn.');
        text('[data-tt-link-time]', 'Đã lưu lúc ' + date(generatedLink.createdAt) + ' (giờ Việt Nam)');
        find('[data-tt-result]').hidden = false; find('[data-tt-result-placeholder]').hidden = true;
        renderProduct(generatedLink); renderLinks();
        feedback(productUrlOnly ? 'Đã lấy thông tin mặc định và lưu link sản phẩm.' : 'Đã tạo và lưu liên kết TikTok.', 'success');
      });
    });
    find('[data-tt-copy-result]').addEventListener('click', () => generatedLink && copy(generatedLink.sharingLink));
    find('[data-tt-show-links]').addEventListener('click', () => selectTab('links', true));
    find('[data-tt-show-overview]').addEventListener('click', () => { selectTab('overview', true); find('#tt-product-url').focus(); });
    find('[data-tt-refresh-links]').addEventListener('click', () => run(find('[data-tt-refresh-links]'), 'Đang tải...', async () => {
      state.links = await request('Links'); renderLinks(); feedback('Đã cập nhật danh sách liên kết.', 'success');
    }));
    find('[data-tt-refresh-orders]').addEventListener('click', () => run(find('[data-tt-refresh-orders]'), 'Đang tải...', async () => {
      try { state.orders = await request('Orders'); state.ordersError = null; renderOrders(); }
      catch (error) { state.ordersError = error.message; renderOrders(); throw error; }
    }));
    renderLinks(); renderOrders(); enableWorkflow();
  }
  window.catBackTikTokAffiliate = { init };
  // Native OAuth navigation can restore this document from the browser back/forward cache.
  // Restore the single-submit lock and button so the user can start again after going back.
  window.addEventListener('pageshow', () => {
    const form = document.querySelector('[data-tt-connect-form]');
    if (form?.dataset.submitting !== 'true') return;
    const button = form.querySelector('button[type="submit"]');
    if (button) {
      window.CatBackLoading?.setButtonLoading(button, false);
      button.disabled = false;
      button.removeAttribute('aria-busy');
      if (form.dataset.submitLabel) button.textContent = form.dataset.submitLabel;
    }
    delete form.dataset.submitting;
    delete form.dataset.submitLabel;
  });
  document.addEventListener('DOMContentLoaded', init);
  document.addEventListener('turbo:load', init);
  init();
})();
