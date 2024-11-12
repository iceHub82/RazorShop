$(document).on('change', '#productSize', function() {
    $('#checkedSize').val($(this).val());
});

//document.body.addEventListener("htmx:historyRestore", (e) => {
//    console.log('historyRestore');
//    const element = document.getElementById('your-element-id');
//    // Perform any cleanup or adjustments to prevent duplicates
//    element.innerHTML = "";  // Clear or reset as needed
//});