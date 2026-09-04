(function () {
    "use strict";
    const toast = document.querySelector("[data-admin-settlement-toast]");
    const money = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 });

    function notify(message, error) {
        if (!toast) return;
        toast.textContent = message;
        toast.classList.toggle("is-error", Boolean(error));
        toast.hidden = false;
        window.clearTimeout(notify.timer);
        notify.timer = window.setTimeout(() => { toast.hidden = true; }, 4500);
    }

    async function parse(response) {
        let data = {};
        try { data = await response.json(); } catch (_) { }
        if (response.ok && data.success !== false) return data;
        throw new Error(data.error?.message || data.error || data.message || "Không thể xử lý yêu cầu.");
    }

    function numeric(selector, fallback = 0) {
        const element = document.querySelector(selector);
        return { element, value: Number(element?.dataset.value ?? element?.textContent ?? fallback) || fallback };
    }

    function applySummary(result) {
        const approved = Number(result.approvedCount) || 0;
        const approvedAmount = Number(result.approvedCommission) || 0;
        const pendingCount = numeric("[data-summary-pending-count]");
        const pendingAmount = numeric("[data-summary-pending-amount]");
        const approvedCount = numeric("[data-summary-approved-count]");
        const approvedTotal = numeric("[data-summary-approved-amount]");
        if (pendingCount.element) pendingCount.element.textContent = Math.max(0, pendingCount.value - approved);
        if (pendingAmount.element) {
            const value = Math.max(0, pendingAmount.value - approvedAmount);
            pendingAmount.element.dataset.value = value;
            pendingAmount.element.textContent = `${money.format(value)}đ`;
        }
        if (approvedCount.element) approvedCount.element.textContent = approvedCount.value + approved;
        if (approvedTotal.element) {
            const value = approvedTotal.value + approvedAmount;
            approvedTotal.element.dataset.value = value;
            approvedTotal.element.textContent = `${money.format(value)}đ`;
        }
        const batchPending = document.querySelector("[data-batch-pending-count]");
        const batchApproved = document.querySelector("[data-batch-approved-count]");
        if (batchPending) batchPending.textContent = result.batch.pendingCount;
        if (batchApproved) batchApproved.textContent = result.batch.approvedCount;
        if (Number(result.batch.pendingCount) === 0) document.querySelector("[data-approve-all]")?.remove();
    }

    function markApproved(item) {
        if (!item) return;
        item.dataset.status = "approved";
        const badge = item.querySelector("[data-record-status]");
        if (badge) { badge.className = "admin-settlement-status approved"; badge.textContent = "Đã duyệt"; }
        item.querySelector("[data-approve-record]")?.remove();
    }

    document.addEventListener("submit", async event => {
        const form = event.target.closest("[data-refresh-matches]");
        if (!form) return;
        event.preventDefault();
        const confirmed = await window.CatsBackModal.confirm({
            variant: "info",
            title: "Đối chiếu lại các bản ghi lỗi?",
            message: "CatsBack sẽ kiểm tra lại đơn hàng và người dùng đã ghép. Thao tác này không cộng tiền vào ví.",
            cancelText: "Để sau",
            confirmText: "Đối chiếu lại"
        });
        if (!confirmed) return;
        const button = form.querySelector("button[type=submit]");
        const original = button.textContent;
        window.CatBackLoading?.setButtonLoading(button, true, { text: "Đang kiểm tra..." });
        if (!window.CatBackLoading) {
            button.disabled = true;
            button.textContent = "Đang kiểm tra...";
        }
        const endLongTask = window.CatBackLoading?.beginLongTask({
            title: "Đang đối chiếu dữ liệu...",
            message: "CatBack đang kiểm tra lại các bản ghi. Vui lòng không tắt cửa sổ này."
        });
        try {
            const payload = await parse(await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            }));
            notify(payload.message);
            window.setTimeout(() => window.location.reload(), 700);
        } catch (error) {
            window.CatBackLoading?.setButtonLoading(button, false);
            if (!window.CatBackLoading) {
                button.disabled = false;
                button.textContent = original;
            }
            notify(error.message, true);
        } finally {
            endLongTask?.();
        }
    });

    document.addEventListener("submit", async event => {
        const form = event.target.closest("[data-approve-record], [data-approve-all]");
        if (!form) return;
        event.preventDefault();
        const bulk = form.hasAttribute("data-approve-all");
        const count = bulk ? Number(form.dataset.count) || 0 : 1;
        const amount = bulk ? Number(form.dataset.amount) || 0 : Number(form.closest("[data-settlement-record]")?.dataset.paid) || 0;
        const confirmed = await window.CatsBackModal.confirm({
            variant: "info",
            title: bulk ? `Duyệt ${count} đơn đối soát?` : "Duyệt đơn đối soát?",
            message: amount > 0
                ? `Thao tác sẽ cộng tiền vào ví người dùng. Tổng hoa hồng tương ứng: ${money.format(amount)}đ.`
                : "Thao tác sẽ dùng giá trị đối soát đang hiển thị để cộng tiền vào ví người dùng.",
            cancelText: "Kiểm tra lại",
            confirmText: "Xác nhận duyệt"
        });
        if (!confirmed) return;
        const button = form.querySelector("button[type=submit]");
        const status = form.querySelector("[role=status]");
        const original = button.textContent;
        window.CatBackLoading?.setButtonLoading(button, true, { text: "Đang xử lý..." });
        if (!window.CatBackLoading) {
            button.disabled = true;
            button.textContent = "Đang xử lý...";
        }
        if (status) status.textContent = "";
        const endLongTask = window.CatBackLoading?.beginLongTask({
            title: bulk ? "Đang duyệt các đơn đối soát..." : "Đang duyệt đơn đối soát...",
            message: "CatBack đang cập nhật số dư ví. Vui lòng không tắt cửa sổ này."
        });
        try {
            const payload = await parse(await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            }));
            if (Number(payload.result?.approvedCount) === 0 || Number(payload.result?.skippedCount) > 0) {
                applySummary(payload.result);
                notify(payload.message);
                window.setTimeout(() => window.location.reload(), 700);
                return;
            }
            if (bulk) document.querySelectorAll('[data-settlement-record]').forEach(item => {
                if (!['approved', 'alreadysettled'].includes(item.dataset.status)) markApproved(item);
            });
            else markApproved(form.closest("[data-settlement-record]"));
            applySummary(payload.result);
            notify(payload.message);
        } catch (error) {
            window.CatBackLoading?.setButtonLoading(button, false);
            if (!window.CatBackLoading) {
                button.disabled = false;
                button.textContent = original;
            }
            if (status) status.textContent = error.message;
            notify(error.message, true);
        } finally {
            endLongTask?.();
        }
    });
})();
