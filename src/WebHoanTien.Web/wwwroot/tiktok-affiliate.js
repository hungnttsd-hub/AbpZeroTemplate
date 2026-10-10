(() => {
  if (window.catBackTikTokAffiliate) { window.catBackTikTokAffiliate.init(); return; }
  function init() {
    const root = document.querySelector('[data-tiktok-affiliate]');
    if (!root || root.dataset.initialized) return;
    root.dataset.initialized = 'true';
    const find = selector => root.querySelector(selector);
    const state = JSON.parse(find('[data-tt-initial]').textContent);
    const isDemo = state.integration.isDemo;
    const showDemoLabel = state.showDemoLabel !== false;
    let product = null;
    let generatedLink = null;
    let productRevision = 0;
    let busy = false;
    const currency = (value, code = 'VND') => `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(value)} ${code}`;
    const date = value => new Intl.DateTimeFormat('vi-VN', {
      dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Ho_Chi_Minh'
    }).format(new Date(value));
    const text = (selector, value) => { find(selector).textContent = value; };
    const statusLabel = value => ({
      CONNECTED: 'Đã kết nối', Pending: 'Chờ xử lý', Settled: 'Đã quyết toán',
      Cancelled: 'Đã hủy', Generated: 'Đã tạo'
    }[value] || value);
    function feedback(message, kind = 'error') {
      const el = find('[data-tt-feedback]');
      el.hidden = !message; el.textContent = message; el.dataset.kind = kind;
    }
    function enableWorkflow() {
      const connected = !!state.creator && isDemo;
      find('[data-tt-connect]').disabled = !!state.creator || busy || !isDemo;
      const disconnect = find('[data-tt-disconnect]');
      if (disconnect) { disconnect.hidden = !connected; disconnect.disabled = busy; }
      root.querySelectorAll('[data-tt-delete-link]').forEach(button => { button.disabled = busy; });
      find('#tt-product-url').disabled = !connected || busy;
      find('[data-tt-check]').disabled = !connected || busy;
      const example = find('[data-tt-example]');
      if (example) example.disabled = !connected || busy;
      find('[data-tt-generate]').disabled = !product || busy || !isDemo;
    }
    async function request(handler, values = null) {
      const options = { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } };
      if (values) {
        options.method = 'POST';
        const body = new URLSearchParams(values);
        body.set('__RequestVerificationToken', find('input[name="__RequestVerificationToken"]').value);
        options.body = body;
      }
      const response = await fetch(`/tiktok-affiliate?handler=${handler}`, options);
      if (response.redirected || response.status === 401 || response.status === 403)
        throw new Error('Phiên CatBack đã hết hạn. Đăng nhập lại để tiếp tục.');
      const payload = await response.json().catch(() => null);
      if (!response.ok) throw new Error((typeof payload?.error === 'string' ? payload.error : payload?.error?.message) || 'Không thể hoàn tất thao tác. Vui lòng thử lại.');
      if (!payload || !('data' in payload)) throw new Error('Phản hồi không hợp lệ. Vui lòng tải lại trang.');
      return payload.data;
    }
    async function run(button, label, action) {
      if (busy) return;
      busy = true; feedback(''); enableWorkflow();
      const previous = button.textContent;
      button.textContent = label; button.setAttribute('aria-busy', 'true');
      try { await action(); } catch (error) { feedback(error.message); }
      finally { busy = false; button.textContent = button.matches('[data-tt-connect]') && state.creator ? 'Đã kết nối nhà sáng tạo ✓' : previous; button.removeAttribute('aria-busy'); enableWorkflow(); }
    }
    function renderCreator() {
      const creator = state.creator;
      text('[data-tt-creator-name]', creator?.displayName || 'Kết nối nhà sáng tạo TikTok');
      text('[data-tt-creator-description]', creator ? `@${creator.username} · ${creator.market === 'VN' ? 'Việt Nam' : creator.market} · ${statusLabel(creator.status)}` : 'Bắt đầu bằng cách kết nối hồ sơ nhà sáng tạo.');
      text('[data-tt-creator-id]', creator?.creatorId || ''); find('[data-tt-creator-id]').hidden = !creator;
      const status = find('[data-tt-connection-status]');
      status.textContent = creator ? 'Đã kết nối' : 'Chưa kết nối'; status.classList.toggle('is-connected', !!creator);
      text('[data-tt-connect]', creator ? 'Đã kết nối nhà sáng tạo ✓' : isDemo ? 'Mở màn hình cấp quyền demo →' : 'Kết nối nhà sáng tạo TikTok →');
      text('[data-tt-product-hint]', creator ? (isDemo ? 'Dùng sản phẩm mẫu để trải nghiệm quy trình. Thông tin sản phẩm là dữ liệu mô phỏng.' : 'Dán liên kết sản phẩm để tiếp tục.') : 'Kết nối nhà sáng tạo để tra cứu sản phẩm.');
      enableWorkflow();
    }
    function safeSharingUrl(value) {
      try { const url = new URL(value); return url.protocol === 'https:' && !url.username && !url.password &&
        (url.hostname === 'shop.tiktok.com' || url.hostname === 'www.tiktok.com' || url.hostname === 'vt.tiktok.com' || url.hostname === 'vm.tiktok.com') ? url.href : null; }
      catch { return null; }
    }
    async function copy(value) {
      try {
        if (navigator.clipboard?.writeText) await navigator.clipboard.writeText(value);
        else {
          const input = document.createElement('textarea'); input.value = value; input.style.position = 'fixed'; input.style.opacity = '0';
          root.append(input); input.select(); const copied = document.execCommand('copy'); input.remove();
          if (!copied) throw new Error('copy unavailable');
        }
        feedback(isDemo ? 'Đã sao chép liên kết mẫu. Đây không phải liên kết ghi nhận hoa hồng thật.' : 'Đã sao chép liên kết tiếp thị.', 'success');
      } catch { feedback('Không thể tự động sao chép. Hãy chọn và sao chép liên kết thủ công.'); }
    }
    function cell(row, value) { const td = document.createElement('td'); td.textContent = value; row.append(td); return td; }
    function badge(td, value) { const span = document.createElement('span'); span.className = 'tt-status';
      const style = { Pending: 'pending', Settled: 'settled', Cancelled: 'cancelled', Generated: 'generated' }[value];
      if (style) span.classList.add(`is-${style}`); span.textContent = statusLabel(value); td.append(span); }
    function renderLinks() {
      const links = state.links || [];
      text('[data-tt-link-count]', links.length);
      find('[data-tt-links-empty]').hidden = links.length > 0; find('[data-tt-links-table]').hidden = !links.length;
      const body = find('[data-tt-links-body]'); body.replaceChildren();
      links.forEach(link => {
        const row = document.createElement('tr');
        const title = cell(row, link.productTitle); const note = document.createElement('small');
        note.textContent = `${isDemo ? 'Demo · ' : ''}${link.productId}`; title.append(note);
        cell(row, date(link.createdAt)); badge(cell(row, ''), link.status);
        const actions = cell(row, ''); const container = document.createElement('div'); container.className = 'tt-table-actions';
        const url = safeSharingUrl(link.sharingLink);
        if (url) { const open = document.createElement('a'); open.href = url; open.target = '_blank';
          open.rel = 'noopener noreferrer'; open.referrerPolicy = 'no-referrer'; open.dataset.turbo = 'false';
          open.textContent = isDemo ? 'Mở liên kết mẫu ↗' : 'Mở liên kết ↗'; open.setAttribute('aria-label', `Mở liên kết ${isDemo ? 'mẫu' : 'tiếp thị'} của ${link.productTitle}`); container.append(open); }
        const button = document.createElement('button'); button.type = 'button'; button.textContent = 'Sao chép liên kết';
        button.setAttribute('aria-label', `Sao chép liên kết của ${link.productTitle}`); button.addEventListener('click', () => copy(link.sharingLink));
        container.append(button); actions.append(container); body.append(row);
        if (isDemo) {
          const remove = document.createElement('button'); remove.type = 'button'; remove.textContent = 'Xóa liên kết';
          remove.dataset.ttDeleteLink = ''; remove.disabled = busy;
          remove.setAttribute('aria-label', `Xóa liên kết mẫu của ${link.productTitle}`);
          remove.addEventListener('click', () => run(remove, 'Đang xóa...', async () => {
            state.links = await request('DeleteLink', { productId: link.productId });
            if (generatedLink?.productId === link.productId) {
              generatedLink = null; find('[data-tt-result]').hidden = true; find('#tt-sharing-link').value = '';
            }
            renderLinks(); feedback('Đã xóa liên kết mẫu. Bạn có thể tạo lại.', 'success');
          }));
          container.append(remove);
        }
      });
    }
    function renderOrders() {
      const orders = state.orders || [];
      text('[data-tt-orders-count]', `${orders.length} đơn hàng${isDemo ? ' mẫu' : ''}`);
      find('[data-tt-orders-empty]').hidden = orders.length > 0; find('[data-tt-orders-table]').hidden = !orders.length;
      const body = find('[data-tt-orders-body]'); body.replaceChildren();
      orders.forEach(order => {
        const row = document.createElement('tr'); const id = cell(row, order.orderId);
        if (isDemo) { const label = document.createElement('small'); label.textContent = 'Đơn hàng mẫu'; id.append(label); }
        cell(row, order.product); cell(row, currency(order.orderAmount, order.currency));
        cell(row, currency(order.affiliateCommission, order.currency)); badge(cell(row, ''), order.status);
        cell(row, date(order.createdAt)); body.append(row);
      });
    }
    function selectTab(name, focus = false) {
      root.querySelectorAll('[data-tt-tab]').forEach(tab => {
        const active = tab.dataset.ttTab === name; tab.setAttribute('aria-selected', String(active)); tab.tabIndex = active ? 0 : -1;
        find(`#tt-${tab.dataset.ttTab}`).hidden = !active; if (active && focus) tab.focus();
      });
    }
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
    find('[data-tt-connect]').addEventListener('click', () => {
      if (isDemo && !busy && !state.creator) window.location.assign('/tiktok-affiliate/demo/authorize');
    });
    const clearProduct = () => { productRevision++; product = null; generatedLink = null; find('[data-tt-product]').hidden = true;
      find('[data-tt-result]').hidden = true; text('[data-tt-generate-hint]', 'Kiểm tra sản phẩm để tiếp tục.'); enableWorkflow(); };
    find('[data-tt-disconnect]')?.addEventListener('click', () => run(find('[data-tt-disconnect]'), 'Đang ngắt kết nối...', async () => {
      const result = await request('Disconnect', {}); state.creator = result.creator; state.orders = result.orders;
      clearProduct(); find('#tt-product-url').value = ''; find('#tt-sharing-link').value = '';
      renderCreator(); renderOrders();
      feedback('Đã ngắt kết nối nhà sáng tạo mẫu. Kết nối lại để trải nghiệm từ đầu. Bạn vẫn có thể xóa các liên kết đã tạo.', 'success');
    }));
    find('#tt-product-url').addEventListener('input', clearProduct);
    find('[data-tt-example]')?.addEventListener('click', () => { find('#tt-product-url').value = state.integration.sampleProductUrl || ''; clearProduct(); find('#tt-product-url').focus(); });
    find('[data-tt-product-form]').addEventListener('submit', event => {
      event.preventDefault(); clearProduct(); const revision = productRevision;
      run(find('[data-tt-check]'), 'Đang kiểm tra sản phẩm...', async () => {
        const checked = await request('Product', { productUrl: find('#tt-product-url').value });
        if (revision !== productRevision) return;
        product = checked; text('[data-tt-product-title]', product.title); text('[data-tt-product-shop]', product.shopName);
        text('[data-tt-product-source]', isDemo && showDemoLabel ? 'SẢN PHẨM MẪU' : 'SẢN PHẨM');
        text('[data-tt-product-price]', currency(product.price, product.currency));
        text('[data-tt-product-commission]', `${new Intl.NumberFormat('vi-VN', { style: 'percent' }).format(product.commissionRate)} · ${currency(product.price * product.commissionRate, product.currency)}`);
        const image = find('[data-tt-product-image]'); image.src = product.imageUrl; image.alt = `${product.title}${isDemo ? ' — ảnh minh họa' : ''}`;
        find('[data-tt-product]').hidden = false; text('[data-tt-generate-hint]', isDemo ? 'Tạo liên kết chia sẻ mẫu, không gọi API TikTok.' : 'Sẵn sàng tạo liên kết chia sẻ tiếp thị.');
      });
    });
    find('[data-tt-generate]').addEventListener('click', () => run(find('[data-tt-generate]'), 'Đang tạo liên kết tiếp thị TikTok...', async () => {
      if (!product) return;
      const result = await request('Generate', { productId: product.productId }); generatedLink = result.link; state.links = result.links;
      find('#tt-sharing-link').value = generatedLink.sharingLink; text('[data-tt-link-time]', `Đã tạo lúc ${date(generatedLink.createdAt)} (giờ Việt Nam)`);
      find('[data-tt-result]').hidden = false; renderLinks();
    }));
    find('[data-tt-copy-result]').addEventListener('click', () => generatedLink && copy(generatedLink.sharingLink));
    find('[data-tt-show-links]').addEventListener('click', () => selectTab('links', true));
    find('[data-tt-show-overview]').addEventListener('click', () => selectTab('overview', true));
    text('[data-tt-connection-note]', isDemo ? `${showDemoLabel ? 'Chế độ mô phỏng · ' : ''}Đăng nhập trên TikTok bằng nút kết nối thật phía trên. Trang demo chỉ mô phỏng bước cấp quyền và không xác minh tài khoản TikTok thật.` : 'Sử dụng luồng cấp quyền chính thức của nhà sáng tạo khi tích hợp thật khả dụng.');
    text('[data-tt-link-note]', isDemo ? 'Liên kết mẫu do CatBack tạo, không do TikTok cấp và không ghi nhận hoa hồng.' : 'Liên kết chia sẻ tiếp thị đã sẵn sàng để sao chép.');
    text('[data-tt-guide-note]', isDemo ? 'Bản mô phỏng sử dụng hồ sơ, sản phẩm, liên kết và đơn hàng mẫu. Không có dữ liệu hoặc giao dịch TikTok thật.' : state.integration.isAvailable ? 'Dữ liệu nhà sáng tạo được truy cập trong phạm vi quyền đã cấp qua luồng chính thức.' : 'Đang hoàn thiện tích hợp thật. Tra sản phẩm, tạo liên kết và đọc đơn hàng qua API chưa khả dụng.');
    text('[data-tt-history-note]', isDemo ? 'Chỉ có liên kết mẫu. Lịch sử được lưu tạm và tách biệt với dữ liệu tiếp thị thật.' : 'Các liên kết chia sẻ tiếp thị bạn đã tạo.');
    text('[data-tt-orders-note]', isDemo ? 'Đơn hàng mẫu để xem trước. Trạng thái và hoa hồng được mô phỏng, không có giá trị thanh toán.' : 'Đơn hàng được ghi nhận cho nhà sáng tạo đã kết nối.');
    renderCreator(); renderLinks(); renderOrders();
  }
  window.catBackTikTokAffiliate = { init };
  document.addEventListener('DOMContentLoaded', init);
  document.addEventListener('turbo:load', init);
  init();
})();
