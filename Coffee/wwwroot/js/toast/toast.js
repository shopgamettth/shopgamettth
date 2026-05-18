// =============================================
// TOAST UTILITY - Tái sử dụng cho toàn dự án
// Gọi: showToast("Thông báo")
//      showToast("Thông báo", "success" | "error" | "warning" | "info")
//      showToast("Thông báo", "success", 3000)
// =============================================

(function () {
    const STYLE_ID = "toast-global-style";

    const CSS = `
        #toast-container {
            position: fixed;
            z-index: 99999;
            display: flex;
            flex-direction: column;
            gap: 8px;
            pointer-events: none;

            /* Desktop: góc phải dưới */
            bottom: 24px;
            right: 24px;
            align-items: flex-end;
        }

        /* Mobile: full-width, căn giữa ở dưới */
        @media (max-width: 576px) {
            #toast-container {
                bottom: 16px;
                right: 12px;
                left: 12px;
                align-items: stretch;
            }
        }

        .toast-item {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 10px 16px;
            border-radius: 8px;
            font-size: 13.5px;
            font-weight: 500;
            font-family: inherit;
            color: #fff;
            line-height: 1.4;
            max-width: 300px;
            box-shadow: 0 3px 14px rgba(0,0,0,0.20);
            pointer-events: auto;
            cursor: pointer;
            opacity: 0;
            transform: translateX(30px);
            transition:
                opacity 0.28s ease,
                transform 0.30s cubic-bezier(0.22, 1, 0.36, 1);
        }

        /* Mobile: full-width, slide từ dưới lên */
        @media (max-width: 576px) {
            .toast-item {
                max-width: 100%;
                transform: translateY(20px);
                border-radius: 10px;
            }
        }

        .toast-item.toast-show {
            opacity: 1;
            transform: translateX(0);
        }

        @media (max-width: 576px) {
            .toast-item.toast-show {
                transform: translateY(0);
            }
        }

        .toast-item.toast-hide {
            opacity: 0;
            transform: translateX(30px);
            transition: opacity 0.22s ease, transform 0.22s ease;
        }

        @media (max-width: 576px) {
            .toast-item.toast-hide {
                transform: translateY(20px);
            }
        }

        .toast-item.toast-success { background: #1e7e4a; }
        .toast-item.toast-error   { background: #b83232; }
        .toast-item.toast-warning { background: #b87a10; }
        .toast-item.toast-info    { background: #1a5fa8; }

        .toast-icon  { font-size: 15px; flex-shrink: 0; }
        .toast-msg   { flex: 1; }
        .toast-close {
            font-size: 13px;
            opacity: 0.65;
            flex-shrink: 0;
            margin-left: 4px;
            transition: opacity 0.15s;
        }
        .toast-item:hover .toast-close { opacity: 1; }
    `;

    const ICONS = {
        success: "✓",
        error: "✕",
        warning: "⚠",
        info: "ℹ"
    };

    function injectCSS() {
        if (document.getElementById(STYLE_ID)) return;
        const s = document.createElement("style");
        s.id = STYLE_ID;
        s.textContent = CSS;
        document.head.appendChild(s);
    }

    function getContainer() {
        let el = document.getElementById("toast-container");
        if (!el) {
            el = document.createElement("div");
            el.id = "toast-container";
            document.body.appendChild(el);
        }
        return el;
    }

    /**
     * @param {string} message
     * @param {"success"|"error"|"warning"|"info"} type
     * @param {number} duration  ms
     */
    window.showToast = function (message, type = "success", duration = 3000) {
        injectCSS();
        const container = getContainer();

        const toast = document.createElement("div");
        toast.className = `toast-item toast-${type}`;
        toast.innerHTML =
            `<span class="toast-icon">${ICONS[type] ?? ICONS.info}</span>` +
            `<span class="toast-msg">${message}</span>` +
            `<span class="toast-close">✕</span>`;

        container.appendChild(toast);

        requestAnimationFrame(() =>
            requestAnimationFrame(() => toast.classList.add("toast-show"))
        );

        function dismiss() {
            toast.classList.remove("toast-show");
            toast.classList.add("toast-hide");
            toast.addEventListener("transitionend", () => toast.remove(), { once: true });
        }

        const timer = setTimeout(dismiss, duration);
        toast.addEventListener("click", () => { clearTimeout(timer); dismiss(); });
    };
})();