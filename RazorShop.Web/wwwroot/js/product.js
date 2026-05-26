// Product page: swap main image when a thumbnail is clicked.
// Inline onclick="changeImg(this)" handlers moved here to comply with
// CSP script-src 'self' (no 'unsafe-inline').
document.addEventListener('DOMContentLoaded', () => {
    const mainImg = document.getElementById('main-img');
    if (!mainImg) return;

    document.querySelectorAll('.js-product-thumb').forEach(el => {
        el.addEventListener('click', () => {
            mainImg.src = el.src.replace('thumbnail', 'product');
        });
    });
});
