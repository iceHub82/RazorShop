document.addEventListener('DOMContentLoaded', function() {
    const productSize = document.getElementById('productSize');
    if (productSize) {
        productSize.addEventListener('change', function () {
            const checkedSize = document.getElementById('checkedSize');
            if (checkedSize) {
                checkedSize.value = productSize.value;
            }
        });
    }

    // Use strict mode
    'use strict';

    const form = document.getElementById('newsletter-form');
    if (form) {
        form.addEventListener('submit', function(e) {
            if (!form.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    }

    document.addEventListener('htmx:configRequest', function(e) {
        const targetForm = e.target;
        if (targetForm && targetForm.id === 'newsletter-form' && !targetForm.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
            targetForm.classList.add('was-validated');
        }
    });
});