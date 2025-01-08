document.addEventListener('htmx:afterRequest', function(e) {
    if (e.detail.elt.id == 'categories-form') {
        hideModal('categories-modal');
        reloadDataTable('categories-datatable');
    }
    if (e.detail.elt.id == 'sizes-form') {
        hideModal('sizes-modal');
        reloadDataTable('sizes-datatable');
    }
});

hideModal = (modal) => {
    bootstrap.Modal.getInstance($(`#${modal}`)).hide();
}

reloadDataTable = (datatable) => {
    var dTable = $(`#${datatable}`).DataTable();
    dTable.ajax.reload();
}