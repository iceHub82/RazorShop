document.addEventListener('DOMContentLoaded', function() {
    // Sync the selected size radio into the hidden 'size' field that htmx posts.
    // All radios share name="productSize", so bind every one — not just the first
    // (getElementById only ever returned one element, so non-default sizes were ignored).
    const checkedSize = document.getElementById('checkedSize');
    document.querySelectorAll('input[name="productSize"]').forEach(function (radio) {
        radio.addEventListener('change', function () {
            if (checkedSize) {
                checkedSize.value = radio.value;
            }
        });
    });

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