window.CatBackSpa.mount('admin-notifications', ({ signal }) => {
    const form = document.querySelector('[data-admin-notification-form]');
    if (!form) return;
    const audience = form.querySelector('[data-notification-audience]');
    const targetEmail = form.querySelector('[data-target-email]');
    const status = form.querySelector('[data-admin-notification-status]');
    const list = document.querySelector('[data-admin-campaign-list]');
    const toast = document.querySelector('[data-admin-notification-toast]');

    function syncAudience() {
        const single = audience.value === 'SingleUser';
        targetEmail.hidden = !single;
        targetEmail.querySelector('input').required = single;
    }

    function showToast(message) {
        toast.textContent = message;
        toast.hidden = false;
        window.clearTimeout(showToast.timer);
        showToast.timer = window.setTimeout(() => { toast.hidden = true; }, 3000);
    }

    function createCampaign(campaign) {
        const article = document.createElement('article');
        article.dataset.campaignId = campaign.id;
        const icon = document.createElement('span'); icon.className = 'admin-campaign-icon'; icon.textContent = '%';
        const copy = document.createElement('div');
        const title = document.createElement('h3'); title.textContent = campaign.title;
        const message = document.createElement('p'); message.textContent = campaign.message;
        const meta = document.createElement('small');
        meta.textContent = `${new Date(campaign.publishedAt).toLocaleString('vi-VN')} · ${campaign.audience === 1 ? 'Toàn bộ người dùng' : campaign.targetEmail}`;
        copy.append(title, message, meta);
        const count = document.createElement('strong'); count.textContent = `${campaign.recipientCount} người nhận`;
        article.append(icon, copy, count);
        return article;
    }

    audience.addEventListener('change', syncAudience, { signal });
    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const submit = form.querySelector('button[type="submit"]');
        submit.disabled = true;
        status.classList.remove('is-error');
        status.textContent = 'Đang gửi thông báo…';
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
            const data = await response.json();
            if (!response.ok || data.success === false) throw new Error(data.error || 'Không thể gửi thông báo.');
            list.querySelector('[data-admin-campaign-empty]')?.remove();
            list.prepend(createCampaign(data.campaign));
            form.reset();
            syncAudience();
            status.textContent = data.message;
            showToast(data.message);
        } catch (error) {
            status.classList.add('is-error');
            status.textContent = error.message;
        } finally {
            submit.disabled = false;
        }
    }, { signal });
    syncAudience();
});
