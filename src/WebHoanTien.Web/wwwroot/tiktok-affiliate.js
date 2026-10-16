(() => {
  if (window.catBackTikTokAffiliate) { window.catBackTikTokAffiliate.init(); return; }
  function init() {
    const root = document.querySelector('[data-tiktok-affiliate]');
    if (!root || root.dataset.initialized) return;
    root.dataset.initialized = 'true';
    const find = selector => root.querySelector(selector);
    const state = JSON.parse(find('[data-tt-initial]').textContent);
    let generatedLink = null, revision = 0, busy = false;
    const currency = value => value == null ? '—' : new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(value) + '₫';
    const moneyRange = (min, max) => min == null ? 'Chưa có dữ liệu' : max != null && max > min ? currency(min) + '–' + currency(max) : currency(min);
    const date = value => new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date(value));
    const text = (selector, value) => { const element = find(selector); if (element) element.textContent = value; };
    const statusLabel = value => ({
      CREATED: 'Đã tạo', HIDDEN: 'Đã ẩn', FAILED: 'Tạo link thất bại', DISABLED: 'Đã tắt',
      1: 'Chờ thanh toán', 2: 'Chờ xử lý', 3: 'Chờ CatBack đối soát',
      4: 'Đã hủy', 5: 'Đã hoàn trả', 6: 'Không được ghi nhận', 7: 'Đã ghi nhận'
    }[value] || value);
    function feedback(message, kind = 'error') {
      const element = find('[data-tt-feedback]');
      element.hidden = !message; element.textContent = message; element.dataset.kind = kind;
    }
    function enableWorkflow() {
      find('#tt-product-url').disabled = !state.creator;
      find('#tt-product-url').readOnly = busy;
      find('[data-tt-create]').disabled = !state.creator || busy;
      root.querySelectorAll('[data-tt-hide-link], [data-tt-refresh-links], [data-tt-refresh-orders]').forEach(button => { button.disabled = busy; });
      const sync = find('[data-tt-sync-orders]');
      if (sync) sync.disabled = busy || !state.creator;
      const pager = state.orders || {};
      find('[data-tt-orders-prev]').disabled = busy || (pager.pageNumber || 1) <= 1;
      find('[data-tt-orders-next]').disabled = busy || (pager.pageNumber || 1) >= (pager.totalPages || 1);
    }
    function orderValues(page = state.orders?.pageNumber || 1) {
      const values = new URLSearchParams(new FormData(find('[data-tt-order-filters]')));
      values.delete('Tab'); values.set('PageNumber', String(page));
      return Object.fromEntries(values);
    }
    async function request(handler, values = null, query = null) {
      const url = new URL('/tiktok-affiliate', window.location.origin);
      url.searchParams.set('handler', handler);
      if (query) Object.entries(query).forEach(([key, value]) => url.searchParams.set(key, value));
      const options = { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } };
      if (values) {
        options.method = 'POST';
        const body = new URLSearchParams(values);
        body.set('__RequestVerificationToken', find('input[name="__RequestVerificationToken"]').value);
        options.body = body;
      }
      const response = await fetch(url, options);
      if (response.redirected || response.status === 401 || response.status === 403) throw new Error('Phiên CatBack đã hết hạn hoặc bạn chưa được cấp quyền.');
      const payload = await response.json().catch(() => null);
      if (!response.ok) throw new Error((typeof payload?.error === 'string' ? payload.error : payload?.error?.message) || 'Không thể hoàn tất thao tác TikTok. Vui lòng thử lại.');
      if (!payload || !('data' in payload)) throw new Error('Phản hồi không hợp lệ. Vui lòng tải lại trang.');
      return payload.data;
    }
    async function run(button, label, action) {
      if (busy || !button) return;
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
        // All generated links go through CatBack so every click receives an event ID.
        return url.origin === window.location.origin && /^\/go\/(?:cb_)?[a-f0-9]{32}$/.test(url.pathname) ? url.href : null;
      } catch { return null; }
    }
    function safeProductImageUrl(value) {
      if (!value) return null;
      try {
        const url = new URL(value, window.location.origin);
        if (url.username || url.password) return null;
        return url.protocol === 'https:' || url.origin === window.location.origin && url.pathname.startsWith('/products/tiktok/') ? url.href : null;
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
      } catch { feedback('Không thể sao chép. Hãy chọn và sao chép liên kết thủ công.'); }
    }
    function cell(row, value) { const td = document.createElement('td'); td.textContent = value; row.append(td); return td; }
    function badge(td, value) {
      const span = document.createElement('span'); span.className = 'tt-status';
      span.textContent = statusLabel(value); td.append(span);
    }
    function clearResult() {
      revision++; generatedLink = null;
      find('[data-tt-product]').hidden = true; find('[data-tt-result]').hidden = true;
      find('[data-tt-result-placeholder]').hidden = false; find('#tt-sharing-link').value = '';
    }
    function renderProduct(link) {
      const product = link.product;
      text('[data-tt-product-title]', product?.title || link.productTitle);
      text('[data-tt-product-shop]', product?.shopName || 'TikTok Shop');
      text('[data-tt-product-price]', product?.price != null ? moneyRange(product.price, product.priceMaximum) : 'TikTok chưa cung cấp giá bán');
      text('[data-tt-product-commission]', moneyRange(product?.commissionAmount, product?.commissionMaximum));
      text('[data-tt-product-cashback]', moneyRange(product?.estimatedCashback, product?.estimatedCashbackMaximum));
      text('[data-tt-rate-note]', 'Tiền hoàn dự kiến phụ thuộc SKU, ưu đãi và hoa hồng thực tế khi đối soát. Hiện chưa cộng tiền vào ví.');
      const image = find('[data-tt-product-image]');
      const imageUrl = safeProductImageUrl(product?.imageUrl);
      image.hidden = !imageUrl;
      image.onerror = () => { image.hidden = true; find('[data-tt-product]').classList.add('has-no-image'); };
      if (imageUrl) { image.src = imageUrl; image.alt = product.title || link.productTitle; }
      else image.removeAttribute('src');
      find('[data-tt-product]').classList.toggle('has-no-image', !imageUrl);
      find('[data-tt-product]').hidden = false;
    }
    function renderLinks() {
      const links = state.links || [];
      text('[data-tt-link-count]', links.length);
      find('[data-tt-links-empty]').hidden = !!links.length; find('[data-tt-links-table]').hidden = !links.length;
      const body = find('[data-tt-links-body]'); body.replaceChildren();
      links.forEach(link => {
        const row = document.createElement('tr');
        const product = link.product;
        const productTitle = product?.title || link.productTitle || 'Sản phẩm TikTok Shop';
        const productCell = cell(row, ''); productCell.className = 'tt-link-product';
        const summary = document.createElement('div'); summary.className = 'tt-link-product-summary';
        const thumbnail = document.createElement('div'); thumbnail.className = 'tt-link-thumbnail';
        const placeholder = document.createElement('span'); placeholder.className = 'tt-link-image-placeholder';
        placeholder.textContent = 'Chưa có ảnh'; thumbnail.append(placeholder);
        const imageUrl = safeProductImageUrl(product?.imageUrl);
        if (imageUrl) {
          const image = document.createElement('img'); image.width = 64; image.height = 64;
          image.alt = productTitle; image.loading = 'lazy'; image.decoding = 'async'; image.referrerPolicy = 'no-referrer';
          image.onerror = () => { image.remove(); placeholder.hidden = false; };
          image.src = imageUrl; placeholder.hidden = true; thumbnail.append(image);
        }
        const productCopy = document.createElement('div'); productCopy.className = 'tt-link-product-copy';
        const title = document.createElement('strong'); title.textContent = productTitle;
        const note = document.createElement('small'); note.textContent = link.productId;
        productCopy.append(title, note); summary.append(thumbnail, productCopy); productCell.append(summary);
        const commission = cell(row, moneyRange(product?.commissionAmount, product?.commissionMaximum));
        commission.className = 'tt-link-commission';
        cell(row, date(link.createdAt)); badge(cell(row, ''), link.status);
        const actions = cell(row, ''); const container = document.createElement('div'); container.className = 'tt-table-actions';
        const url = safeSharingUrl(link.sharingLink);
        if (url) {
          const open = document.createElement('a'); open.href = url; open.target = '_blank';
          open.rel = 'noopener noreferrer sponsored'; open.referrerPolicy = 'no-referrer'; open.dataset.turbo = 'false';
          open.textContent = 'Mở liên kết ↗'; container.append(open);
          const button = document.createElement('button'); button.type = 'button'; button.textContent = 'Sao chép';
          button.addEventListener('click', () => copy(link.sharingLink)); container.append(button);
        }
        if (link.trackingId && link.canHide) {
          const hide = document.createElement('button'); hide.type = 'button'; hide.textContent = 'Ẩn liên kết'; hide.dataset.ttHideLink = ''; hide.disabled = busy;
          hide.addEventListener('click', () => run(hide, 'Đang ẩn...', async () => {
            state.links = await request('DeleteLink', { trackingId: link.trackingId });
            if (generatedLink?.trackingId === link.trackingId) clearResult();
            renderLinks(); feedback('Đã ẩn liên kết khỏi danh sách cá nhân.', 'success');
          }));
          container.append(hide);
        }
        actions.append(container); body.append(row);
      });
    }
    function renderOrders() {
      const pager = state.orders || { items: [], totalCount: 0, pageNumber: 1, totalPages: 1 };
      const orders = pager.items || [];
      text('[data-tt-orders-count]', pager.totalCount + ' đơn');
      const error = find('[data-tt-orders-error]');
      error.hidden = !state.ordersError; error.textContent = state.ordersError || '';
      find('[data-tt-orders-empty]').hidden = !!orders.length || !!state.ordersError; find('[data-tt-orders-table]').hidden = !orders.length;
      const body = find('[data-tt-orders-body]'); body.replaceChildren();
      orders.forEach(order => {
        const row = document.createElement('tr'); cell(row, order.externalOrderId);
        cell(row, order.items.map(item => item.productName || 'Sản phẩm TikTok Shop').join(' · '));
        cell(row, currency(order.purchaseAmount));
        if (state.isAdminOrderView) {
          cell(row, currency(order.providerActualCommission));
        }
        const status = cell(row, ''); badge(status, order.providerStatus === 'FROZEN' && Number(order.status) !== 7 ? 'Tạm giữ hoa hồng' : order.status);
        if (order.providerStatus) { const source = document.createElement('small'); source.textContent = 'TikTok: ' + order.providerStatus; status.append(source); }
        if (order.reviewNote) { const note = document.createElement('small'); note.textContent = order.reviewNote; status.append(note); }
        cell(row, date(order.purchaseTime));
        const details = document.createElement('a'); details.href = '/Orders/' + order.id + '?returnUrl=%2Ftiktok-affiliate%3FTab%3Dorders'; details.textContent = 'Chi tiết';
        cell(row, '').append(details); body.append(row);
      });
      find('[data-tt-orders-pagination]').hidden = pager.totalPages <= 1;
      text('[data-tt-orders-page]', pager.pageNumber + ' / ' + pager.totalPages);
      find('[data-tt-order-filters] input[name="PageNumber"]').value = pager.pageNumber;
      enableWorkflow();
    }
    function selectTab(name, focus = false) {
      root.querySelectorAll('[data-tt-tab]').forEach(tab => {
        const active = tab.dataset.ttTab === name; tab.setAttribute('aria-selected', String(active)); tab.tabIndex = active ? 0 : -1;
        find('#tt-' + tab.dataset.ttTab).hidden = !active; if (active && focus) tab.focus();
      });
    }
    root.querySelectorAll('[data-tt-connect-form], [data-tt-disconnect-form]').forEach(form => {
      form.addEventListener('submit', event => {
        if (event.defaultPrevented) return;
        const forms = [...root.querySelectorAll('[data-tt-connect-form], [data-tt-disconnect-form]')];
        if (forms.some(item => item.dataset.submitting === 'true')) { event.preventDefault(); return; }
        form.dataset.submitting = 'true';
        const button = form.querySelector('button[type="submit"]');
        form.dataset.submitLabel = button.textContent;
        forms.forEach(item => {
          const control = item.querySelector('button[type="submit"]');
          item.dataset.initialDisabled = String(control.disabled);
          control.disabled = true;
        });
        button.setAttribute('aria-busy', 'true');
        button.textContent = form.hasAttribute('data-tt-disconnect-form') ? 'Đang ngắt kết nối…' : 'Đang mở TikTok…';
      });
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
      run(find('[data-tt-create]'), 'Đang tạo link TikTok…', async () => {
        const result = await request('Generate', { productUrlOrId: find('#tt-product-url').value });
        if (currentRevision !== revision) return;
        const url = safeSharingUrl(result.link.sharingLink);
        if (!url) throw new Error('Liên kết trả về không hợp lệ.');
        generatedLink = result.link; state.links = result.links; find('#tt-sharing-link').value = url;
        text('[data-tt-result-title]', 'Đã tạo liên kết tiếp thị qua TikTok.');
        text('[data-tt-result-note]', 'Liên kết đã lưu trong tài khoản CatBack của bạn.');
        text('[data-tt-link-time]', 'Đã lưu lúc ' + date(generatedLink.createdAt) + ' (giờ Việt Nam)');
        find('[data-tt-result]').hidden = false; find('[data-tt-result-placeholder]').hidden = true;
        renderProduct(generatedLink); renderLinks(); feedback('Đã tạo và lưu liên kết TikTok.', 'success');
      });
    });
    find('[data-tt-copy-result]').addEventListener('click', () => generatedLink && copy(generatedLink.sharingLink));
    find('[data-tt-show-links]').addEventListener('click', () => selectTab('links', true));
    find('[data-tt-show-overview]').addEventListener('click', () => { selectTab('overview', true); find('#tt-product-url').focus(); });
    find('[data-tt-refresh-links]').addEventListener('click', () => run(find('[data-tt-refresh-links]'), 'Đang tải…', async () => {
      state.links = await request('Links'); renderLinks(); feedback('Đã cập nhật danh sách liên kết.', 'success');
    }));
    async function loadOrders(page) {
      try { state.orders = await request('Orders', null, orderValues(page)); state.ordersError = null; renderOrders(); }
      catch (error) { state.ordersError = error.message; renderOrders(); throw error; }
    }
    find('[data-tt-refresh-orders]').addEventListener('click', () => run(find('[data-tt-refresh-orders]'), 'Đang tải…', () => loadOrders(state.orders?.pageNumber || 1)));
    find('[data-tt-orders-prev]').addEventListener('click', () => run(find('[data-tt-orders-prev]'), 'Đang tải…', () => loadOrders(Math.max(1, state.orders.pageNumber - 1))));
    find('[data-tt-orders-next]').addEventListener('click', () => run(find('[data-tt-orders-next]'), 'Đang tải…', () => loadOrders(Math.min(state.orders.totalPages, state.orders.pageNumber + 1))));
    find('[data-tt-order-filters]').addEventListener('submit', event => {
      event.preventDefault();
      run(find('[data-tt-order-filters] button[type="submit"]'), 'Đang lọc…', () => loadOrders(1));
    });
    find('[data-tt-sync-orders]')?.addEventListener('click', () => run(find('[data-tt-sync-orders]'), 'Đang lấy đơn từ TikTok…', async () => {
      const payload = await request('Sync', orderValues(1));
      state.orders = payload.orders; state.ordersError = null; renderOrders();
      const result = payload.result;
      feedback('Đã đọc ' + result.fetchedOrders + ' đơn: thêm ' + result.insertedOrders + ', cập nhật ' + result.updatedOrders + ', chưa khớp ' + result.unmatchedOrders + ', cần kiểm tra ' + result.needsReview + '. ' + (result.notice || ''), 'success');
    }));
    renderLinks(); renderOrders(); selectTab(state.initialTab || 'overview'); enableWorkflow();
  }
  window.catBackTikTokAffiliate = { init };
  window.addEventListener('pageshow', () => {
    document.querySelectorAll('[data-tt-connect-form], [data-tt-disconnect-form]').forEach(form => {
      const button = form.querySelector('button[type="submit"]');
      if (button && form.dataset.initialDisabled !== undefined) {
        window.CatBackLoading?.setButtonLoading(button, false);
        button.disabled = form.dataset.initialDisabled === 'true';
        button.removeAttribute('aria-busy');
        if (form.dataset.submitLabel) button.textContent = form.dataset.submitLabel;
      }
      delete form.dataset.submitting;
      delete form.dataset.initialDisabled;
      delete form.dataset.submitLabel;
    });
  });
  document.addEventListener('turbo:load', init);
  document.addEventListener('turbo:render', init);
  document.addEventListener('DOMContentLoaded', init);
  init();
})();
