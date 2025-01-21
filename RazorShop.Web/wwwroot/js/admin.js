document.addEventListener('htmx:afterRequest', function (e) {

    var form = e.detail.elt.closest('form');
    if (!form) return;

    switch (form.id) {
        case 'category-form':
            hideModal('category-modal');
            reloadDataTable('categories-datatable');
            break;
        case 'new-category-form':
            hideModal('new-category-modal');
            reloadDataTable('categories-datatable');
            break;
        case 'sizes-form':
            hideModal('sizes-modal');
            reloadDataTable('sizes-datatable');
            break;
        case 'size-form':
            hideModal('size-modal');
            reloadDataTable('sizes-datatable');
            break;
        case 'new-size-form':
            hideModal('new-size-modal');
            reloadDataTable('sizes-datatable');
            break;
        case 'new-product-form':
            hideModal('new-product-modal');
            reloadDataTable('products-datatable');
            break;
        case 'product-form':
            hideModal('product-modal');
            reloadDataTable('products-datatable');
            break;
    }
});

hideModal = (modal) => {
    bootstrap.Modal.getInstance($(`#${modal}`)).hide();
}

reloadDataTable = (datatable) => {
    var dTable = $(`#${datatable}`).DataTable();
    dTable.ajax.reload();
}