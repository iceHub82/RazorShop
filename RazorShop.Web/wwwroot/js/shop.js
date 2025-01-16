$(document).on('change', '#productSize', function () {
    $('#checkedSize').val($(this).val())
});

const form = document.getElementById('newsletter-form')

document.addEventListener('htmx:configRequest', (e) => {
    let form = e.target;
    if (form && !form.checkValidity()) {
        e.preventDefault()
        e.stopPropagation()
        form.classList.add('was-validated')
    }
})