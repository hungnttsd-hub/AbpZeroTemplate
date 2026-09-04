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
        const emailInput = targetEmail.querySelector('input');
        form.classList.toggle('is-single-user', single);
        targetEmail.hidden = !single;
        emailInput.required = single;
        emailInput.disabled = !single;
    }

    async function readResponse(response) {
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            if (response.redirected || response.status === 401 || response.status === 403) {
                throw new Error('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền thực hiện thao tác này.');
            }
            throw new Error('Máy chủ trả về dữ liệu không hợp lệ. Vui lòng tải lại trang và thử lại.');
        }

        return response.json();
    }

    function getErrorMessage(data) {
        if (typeof data?.error === 'string') return data.error;
        if (typeof data?.error?.message === 'string') return data.error.message;
        return 'Không thể gửi thông báo.';
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
        window.CatBackLoading?.setButtonLoading(submit, true, { text: 'Đang gửi...' });
        if (!window.CatBackLoading) submit.disabled = true;
        status.classList.remove('is-error');
        status.textContent = 'Đang gửi thông báo…';
        const endLongTask = window.CatBackLoading?.beginLongTask({
            title: 'Đang gửi thông báo...',
            message: 'CatBack đang tạo thông báo cho người nhận.'
        });
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
            const data = await readResponse(response);
            if (!response.ok || data.success === false) throw new Error(getErrorMessage(data));
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
            endLongTask?.();
            window.CatBackLoading?.setButtonLoading(submit, false);
            if (!window.CatBackLoading) submit.disabled = false;
        }
    }, { signal });
    syncAudience();
});
