(function () {
    const root = document.querySelector('[data-notification-detail]');
    if (!root) return;

    const backButton = root.querySelector('[data-notification-detail-back]');
    const deleteButton = root.querySelector('[data-delete-notification]');
    const token = document.querySelector('[data-notification-detail-antiforgery] input[name="__RequestVerificationToken"]')?.value || '';
    const toast = document.querySelector('[data-notification-detail-toast]');

    function showToast(message) {
        toast.textContent = message;
        toast.hidden = false;
        window.clearTimeout(showToast.timer);
        showToast.timer = window.setTimeout(() => { toast.hidden = true; }, 2800);
    }

    backButton?.addEventListener('click', (event) => {
        if (!document.referrer) return;
        try {
            const referrer = new URL(document.referrer);
            if (referrer.origin !== window.location.origin || !referrer.pathname.startsWith('/Notifications')) return;
            event.preventDefault();
            window.history.back();
        } catch {
            // Keep the notification-list fallback from the anchor.
        }
    });

    deleteButton?.addEventListener('click', async () => {
        if (!window.confirm('Bạn có chắc muốn xóa thông báo này?')) return;
        deleteButton.disabled = true;
        try {
            const response = await fetch(`${window.location.pathname}?handler=Delete`, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8',
                    RequestVerificationToken: token
                },
                body: new URLSearchParams({ id: deleteButton.dataset.notificationId })
            });
            const data = await response.json();
            if (!response.ok || data.success === false) throw new Error(data.error || 'Không thể xóa thông báo.');
            window.location.replace(data.redirectUrl || '/Notifications');
        } catch (error) {
            deleteButton.disabled = false;
            showToast(error.message);
        }
    });
})();
