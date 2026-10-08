(() => {
  if (window.catBackTikTokAffiliate) { window.catBackTikTokAffiliate.init(); return; }
  function init() {
    const root = document.querySelector('[data-tiktok-affiliate]');
    if (!root || root.dataset.initialized) return;
    root.dataset.initialized = 'true';
    const find = selector => root.querySelector(selector);
    const state = JSON.parse(find('[data-tt-initial]').textContent);
    const isDemo = state.integration.isDemo;
    let product = null;
    let generatedLink = null;
    let productRevision = 0;
    let busy = false;
    const currency = (value, code = 'VND') => `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(value)} ${code}`;
    const date = value => new Intl.DateTimeFormat('vi-VN', {
      dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Ho_Chi_Minh'
    }).format(new Date(value));
    const text = (selector, value) => { find(selector).textContent = value; };
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
        throw new Error('Your CatBack session has expired. Sign in again to continue.');
      const payload = await response.json().catch(() => null);
      if (!response.ok) throw new Error((typeof payload?.error === 'string' ? payload.error : payload?.error?.message) || 'Unable to complete this action. Please try again.');
      if (!payload || !('data' in payload)) throw new Error('Unexpected response. Please reload this page.');
      return payload.data;
    }
    async function run(button, label, action) {
      if (busy) return;
      busy = true; feedback(''); enableWorkflow();
      const previous = button.textContent;
      button.textContent = label; button.setAttribute('aria-busy', 'true');
      try { await action(); } catch (error) { feedback(error.message); }
      finally { busy = false; button.textContent = button.matches('[data-tt-connect]') && state.creator ? 'Creator Connected ✓' : previous; button.removeAttribute('aria-busy'); enableWorkflow(); }
    }
    function renderCreator() {
      const creator = state.creator;
      text('[data-tt-creator-name]', creator?.displayName || 'Connect your TikTok Creator');
      text('[data-tt-creator-description]', creator ? `@${creator.username} · ${creator.market} · ${creator.status}` : 'Start by connecting a Creator profile.');
      text('[data-tt-creator-id]', creator?.creatorId || ''); find('[data-tt-creator-id]').hidden = !creator;
      const status = find('[data-tt-connection-status]');
      status.textContent = creator ? 'Connected' : 'Not connected'; status.classList.toggle('is-connected', !!creator);
      text('[data-tt-connect]', creator ? 'Creator Connected ✓' : 'Connect TikTok Creator →');
      text('[data-tt-product-hint]', creator ? (isDemo ? 'Use the demo product to explore this workflow. Product data is simulated.' : 'Paste a product URL to continue.') : 'Connect your Creator to enable product lookup.');
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
        feedback(isDemo ? 'Demo link copied. This is a sample URL, not a live tracking link.' : 'Affiliate link copied.', 'success');
      } catch { feedback('Copy is unavailable. Select and copy the sharing link manually.'); }
    }
    function cell(row, value) { const td = document.createElement('td'); td.textContent = value; row.append(td); return td; }
    function badge(td, value) { const span = document.createElement('span'); span.className = 'tt-status';
      const style = { Pending: 'pending', Settled: 'settled', Cancelled: 'cancelled', Generated: 'generated' }[value];
      if (style) span.classList.add(`is-${style}`); span.textContent = value; td.append(span); }
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
          open.textContent = isDemo ? 'Open demo link ↗' : 'Open ↗'; open.setAttribute('aria-label', `Open ${link.productTitle}${isDemo ? ' demo link' : ' affiliate link'}`); container.append(open); }
        const button = document.createElement('button'); button.type = 'button'; button.textContent = 'Copy Link';
        button.setAttribute('aria-label', `Copy link for ${link.productTitle}`); button.addEventListener('click', () => copy(link.sharingLink));
        container.append(button); actions.append(container); body.append(row);
        if (isDemo) {
          const remove = document.createElement('button'); remove.type = 'button'; remove.textContent = 'Delete Link';
          remove.dataset.ttDeleteLink = ''; remove.disabled = busy;
          remove.setAttribute('aria-label', `Delete demo link for ${link.productTitle}`);
          remove.addEventListener('click', () => run(remove, 'Deleting...', async () => {
            state.links = await request('DeleteLink', { productId: link.productId });
            if (generatedLink?.productId === link.productId) {
              generatedLink = null; find('[data-tt-result]').hidden = true; find('#tt-sharing-link').value = '';
            }
            renderLinks(); feedback('Demo link deleted. You can generate it again.', 'success');
          }));
          container.append(remove);
        }
      });
    }
    function renderOrders() {
      const orders = state.orders || [];
      text('[data-tt-orders-count]', `${orders.length} ${isDemo ? 'demo ' : ''}orders`);
      find('[data-tt-orders-empty]').hidden = orders.length > 0; find('[data-tt-orders-table]').hidden = !orders.length;
      const body = find('[data-tt-orders-body]'); body.replaceChildren();
      orders.forEach(order => {
        const row = document.createElement('tr'); const id = cell(row, order.orderId);
        if (isDemo) { const label = document.createElement('small'); label.textContent = 'Demo order'; id.append(label); }
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
    find('[data-tt-connect]').addEventListener('click', () => run(find('[data-tt-connect]'), 'Connecting Creator...', async () => {
      const result = await request('Connect', {}); state.creator = result.creator; state.orders = result.orders;
      renderCreator(); renderOrders(); feedback(isDemo ? 'Demo Creator connected. No real TikTok account was authorized.' : 'Creator connected.', 'success');
    }));
    const clearProduct = () => { productRevision++; product = null; generatedLink = null; find('[data-tt-product]').hidden = true;
      find('[data-tt-result]').hidden = true; text('[data-tt-generate-hint]', 'Check a product to continue.'); enableWorkflow(); };
    find('[data-tt-disconnect]')?.addEventListener('click', () => run(find('[data-tt-disconnect]'), 'Disconnecting...', async () => {
      const result = await request('Disconnect', {}); state.creator = result.creator; state.orders = result.orders;
      clearProduct(); find('#tt-product-url').value = ''; find('#tt-sharing-link').value = '';
      renderCreator(); renderOrders();
      feedback('Demo Creator disconnected. Connect again to restart the walkthrough. Generated links remain available to delete.', 'success');
    }));
    find('#tt-product-url').addEventListener('input', clearProduct);
    find('[data-tt-example]')?.addEventListener('click', () => { find('#tt-product-url').value = state.integration.sampleProductUrl || ''; clearProduct(); find('#tt-product-url').focus(); });
    find('[data-tt-product-form]').addEventListener('submit', event => {
      event.preventDefault(); clearProduct(); const revision = productRevision;
      run(find('[data-tt-check]'), 'Checking product...', async () => {
        const checked = await request('Product', { productUrl: find('#tt-product-url').value });
        if (revision !== productRevision) return;
        product = checked; text('[data-tt-product-title]', product.title); text('[data-tt-product-shop]', product.shopName);
        text('[data-tt-product-source]', isDemo ? 'DEMO PRODUCT' : 'PRODUCT');
        text('[data-tt-product-price]', currency(product.price, product.currency));
        text('[data-tt-product-commission]', `${new Intl.NumberFormat('vi-VN', { style: 'percent' }).format(product.commissionRate)} · ${currency(product.price * product.commissionRate, product.currency)}`);
        const image = find('[data-tt-product-image]'); image.src = product.imageUrl; image.alt = `${product.title}${isDemo ? ' — demo illustration' : ''}`;
        find('[data-tt-product]').hidden = false; text('[data-tt-generate-hint]', isDemo ? 'Generates a demo sharing link. No TikTok API request is made.' : 'Ready to generate your affiliate sharing link.');
      });
    });
    find('[data-tt-generate]').addEventListener('click', () => run(find('[data-tt-generate]'), 'Generating TikTok affiliate link...', async () => {
      if (!product) return;
      const result = await request('Generate', { productId: product.productId }); generatedLink = result.link; state.links = result.links;
      find('#tt-sharing-link').value = generatedLink.sharingLink; text('[data-tt-link-time]', `Created ${date(generatedLink.createdAt)} (VN)`);
      find('[data-tt-result]').hidden = false; renderLinks();
    }));
    find('[data-tt-copy-result]').addEventListener('click', () => generatedLink && copy(generatedLink.sharingLink));
    find('[data-tt-show-links]').addEventListener('click', () => selectTab('links', true));
    find('[data-tt-show-overview]').addEventListener('click', () => selectTab('overview', true));
    text('[data-tt-connection-note]', isDemo ? 'Demo connection only. This does not authorize or connect a real TikTok account.' : 'Use the official Creator authorization flow when live integration is available.');
    text('[data-tt-link-note]', isDemo ? 'Demo link only — not issued by TikTok and not valid for affiliate attribution.' : 'Your affiliate sharing link is ready to copy.');
    text('[data-tt-guide-note]', isDemo ? 'Review Demo uses sample profiles, products, links and orders. No real TikTok data or transactions are involved.' : state.integration.isAvailable ? 'Creator data is available within the permissions granted through official authorization.' : 'Live integration is pending. Creator data will be available after official authorization.');
    text('[data-tt-history-note]', isDemo ? 'Demo links only. History is temporary and separate from live affiliate data.' : 'Your generated affiliate sharing links.');
    text('[data-tt-orders-note]', isDemo ? 'Sample orders for review. Status and commission are simulated and have no financial effect.' : 'Orders attributed to your connected Creator.');
    renderCreator(); renderLinks(); renderOrders();
  }
  window.catBackTikTokAffiliate = { init };
  document.addEventListener('DOMContentLoaded', init);
  document.addEventListener('turbo:load', init);
  init();
})();
