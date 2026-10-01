(function () {
    var dialog = document.getElementById('customerDialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;

    function $(id) { return document.getElementById(id); }
    var idBox = $('cdId'), nameBox = $('cdName'), contactBox = $('cdContact'), emailBox = $('cdEmail'), saveBtn = $('cdSave');

    function open(row) {
        idBox.value = row.getAttribute('data-customer-id');
        nameBox.value = row.getAttribute('data-name');
        contactBox.value = row.getAttribute('data-contact');
        emailBox.value = row.getAttribute('data-email');
        $('customerDialogTitle').textContent = 'Edit ' + nameBox.value;
        $('customerDialogSub').textContent = 'Customer since ' + row.getAttribute('data-since') + '. Fixes apply to every past and future job.';

        ['cdError', 'cdNameError', 'cdContactError', 'cdEmailError'].forEach(function (id) { $(id).hidden = true; });

        dialog.showModal();
        nameBox.focus();
        nameBox.select();
    }

    document.addEventListener('click', function (e) {
        var edit = e.target.closest('[data-customer-edit]');
        if (edit) open(edit.closest('tr'));
    });

    Array.prototype.forEach.call(dialog.querySelectorAll('[data-modal-close]'), function (b) {
        b.addEventListener('click', function () { dialog.close(); });
    });
    dialog.addEventListener('click', function (e) {
        if (e.target === dialog) dialog.close();
    });

    dialog.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') {
            e.preventDefault();
            saveBtn.click();
        }
    });

    if (dialog.getAttribute('data-open') === 'true') {
        dialog.showModal();
        nameBox.focus();
    }
})();
