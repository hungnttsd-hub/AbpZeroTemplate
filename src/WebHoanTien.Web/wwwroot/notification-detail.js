window.CatBackSpa.mount('notification-detail', ({ signal, back }) => {
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
        event.preventDefault();
        back('/Notifications', '/Notifications');
    }, { signal });

    deleteButton?.addEventListener('click', async () => {
        const confirmed = await window.CatsBackModal.confirm({
            title: 'Xóa thông báo?',
            message: 'Bạn có chắc chắn muốn xóa thông báo này?\nHành động này không thể hoàn tác.',
            cancelText: 'Hủy',
            confirmText: 'Xóa'
        });
        if (!confirmed) return;
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
            await window.CatsBackModal.success({
                title: 'Đã xóa thành công!',
                message: 'Thông báo đã được xóa khỏi hệ thống.',
                confirmText: 'Đóng'
            });
            back(data.redirectUrl || '/Notifications', '/Notifications');
        } catch (error) {
            deleteButton.disabled = false;
            showToast(error.message);
        }
    }, { signal });
});
