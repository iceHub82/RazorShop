$(document).on('change', '#productSize', function () {
    $('#checkedSize').val($(this).val())
});

var mainImg = document.getElementById('main-img');

changeImg = (target) => {
    mainImg.src = target.src;
}