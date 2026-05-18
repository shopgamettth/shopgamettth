(function () {
    const paymentRadios = Array.from(document.querySelectorAll('input[name="PaymentMethod"]'));
    const submitButton = document.querySelector('[data-submit-order]');
    const summaryHeading = document.querySelector('[data-summary-heading]');
    const paymentNoteTitle = document.querySelector('[data-payment-note-title]');
    const paymentNoteBody = document.querySelector('[data-payment-note-body]');
    const momoHint = document.querySelector('[data-momo-hint]');

    function updatePaymentUi() {
        if (!paymentRadios.length) {
            return;
        }

        const selectedValue = paymentRadios.find((radio) => radio.checked)?.value || 'COD';
        const isMomo = selectedValue === 'MOMO';

        if (submitButton) {
            submitButton.textContent = isMomo ? 'Tiep tuc thanh toan MoMo' : 'Dat hang COD';
        }

        if (summaryHeading) {
            summaryHeading.textContent = isMomo ? 'Thanh toan MoMo' : 'Thanh toan COD';
        }

        if (paymentNoteTitle) {
            paymentNoteTitle.textContent = isMomo ? 'Thanh toan MoMo' : 'Thanh toan COD';
        }

        if (paymentNoteBody) {
            paymentNoteBody.textContent = isMomo
                ? 'Sau khi ban thanh toan thanh cong qua MoMo va xac nhan, he thong se cap nhat don sang da thanh toan.'
                : 'Don COD se duoc tao voi trang thai chua thanh toan. Khi admin duyet don, he thong se cap nhat sang da thanh toan.';
        }

        if (momoHint) {
            momoHint.hidden = !isMomo;
        }
    }

    paymentRadios.forEach((radio) => {
        radio.addEventListener('change', updatePaymentUi);
    });

    updatePaymentUi();

    document.querySelectorAll('[data-copy-value]').forEach((button) => {
        button.addEventListener('click', async function () {
            const value = this.getAttribute('data-copy-value') || '';
            const originalText = this.textContent;

            try {
                await navigator.clipboard.writeText(value);
                this.textContent = 'Da copy';
                setTimeout(() => {
                    this.textContent = originalText;
                }, 1400);
            } catch {
                this.textContent = 'Copy that bai';
                setTimeout(() => {
                    this.textContent = originalText;
                }, 1400);
            }
        });
    });
})();
