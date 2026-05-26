// Checkout form validation. Moved out of _CheckoutLayout inline <script>
// to comply with CSP script-src 'self'.
(() => {
    'use strict';

    const consentCb = document.getElementById('consentCb');
    const submitCard = document.getElementById('submit-card');
    const consentLbl = document.getElementById('consent-lbl');
    const form = document.getElementById('submit-form');

    if (form) {
        form.addEventListener('submit', (e) => {
            if (!form.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
                if (submitCard) submitCard.classList.toggle('bg-danger', !(consentCb && consentCb.checked));
                if (consentLbl) consentLbl.classList.toggle('consent', !(consentCb && consentCb.checked));
            } else {
                $('#checkout-submit-btn').attr('disabled', true);
            }
            form.classList.add('was-validated');
        }, false);
    }

    document.addEventListener('htmx:configRequest', (e) => {
        const targetForm = e.target;
        if (targetForm && !targetForm.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
            targetForm.classList.add('was-validated');
        }
    });
})();
