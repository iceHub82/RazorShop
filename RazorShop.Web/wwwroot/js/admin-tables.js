// Initialises every admin DataTable that is actually present on the page.
// Each block is guarded by a feature check on the target element id so a
// single file can serve all admin slices.
$(function () {
    if (document.getElementById('products-datatable')) {
        $('#products-datatable').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: { url: '/admin/products/table' },
            columns: [{
                data: null, name: 'Name',
                render: function (data) {
                    return `<a hx-get="/admin/product/modal/edit/${data.id}" hx-target="#content" data-bs-toggle="modal" data-bs-target="#product-modal">${data.name}</a>`;
                }
            }],
            drawCallback: function () {
                htmx.process(document.getElementById('products-datatable'));
            }
        });
    }

    if (document.getElementById('orders-datatable')) {
        $('#orders-datatable').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: { url: '/admin/orders/table' },
            columns: [
                {
                    name: 'Reference', data: null, render: function (data) {
                        return `<a hx-get="/admin/product-modal/${data.id}" hx-target="#content" data-bs-toggle="modal" data-bs-target="#product-modal">${data.reference}</a>`;
                    }
                },
                { name: 'Created', data: 'created' },
            ],
            drawCallback: function () {
                htmx.process(document.getElementById('orders-datatable'));
            }
        });
    }

    if (document.getElementById('categories-datatable')) {
        $('#categories-datatable').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: { url: '/admin/settings/categories/table' },
            columns: [{
                data: null, name: 'Name',
                render: function (data) {
                    return `<a hx-get="/admin/settings/categories/modal/edit/${data.id}" hx-target="#content" data-bs-toggle="modal" data-bs-target="#category-modal">${data.name}</a>`;
                }
            }],
            drawCallback: function () {
                htmx.process(document.getElementById('categories-datatable'));
            }
        });
    }

    if (document.getElementById('sizes-datatable')) {
        $('#sizes-datatable').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: { url: '/admin/settings/sizes/table' },
            columns: [{
                data: null, name: 'Name',
                render: function (data) {
                    return `<a hx-get="/admin/settings/sizes/modal/edit/${data.id}" hx-target="#content" data-bs-toggle="modal" data-bs-target="#sizes-modal">${data.name}</a>`;
                }
            }],
            drawCallback: function () {
                htmx.process(document.getElementById('sizes-datatable'));
            }
        });
    }
});
