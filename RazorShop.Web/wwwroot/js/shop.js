$(document).on('change', '#productSize', function () {
    $('#checkedSize').val($(this).val())
});

(() => {
    'use strict'

    const form = document.getElementById('newsletter-form')

    if (form) {
        form.addEventListener('submit', (e) => {
            if (!form.checkValidity()) {
                e.preventDefault()
                e.stopPropagation()
            }
            form.classList.add('was-validated')
        }, false)
    }

    document.addEventListener('htmx:configRequest', (e) => {
        let form = e.target;
        if (form && form.id == 'newsletter-form' && !form.checkValidity()) {
            e.preventDefault()
            e.stopPropagation()
            form.classList.add('was-validated')
        }
    })
})()