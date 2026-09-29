// Pages/Shared/Customers.aspx
// The owner's window (a <dialog>) to edit one customer: a row's "Edit" button fills it from that
// row's data- attributes. Save is a real submit button: the server checks everything again
// (CustomerService) and, if it refuses, draws the window open again with the reason (data-open="true").
(function () {
    var dialog = document.getElementById('customerDialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;   // staff have no edit window

    function $(id) { return document.getElementById(id); }
    var idBox = $('cdId'), nameBox = $('cdName'), contactBox = $('cdContact'), emailBox = $('cdEmail'), saveBtn = $('cdSave');

    function open(row) {
        idBox.value = row.getAttribute('data-customer-id');
        nameBox.value = row.getAttribute('data-name');
        contactBox.value = row.getAttribute('data-contact');
        emailBox.value = row.getAttribute('data-email');
        $('customerDialogTitle').textContent = 'Edit ' + nameBox.value;
        $('customerDialogSub').textContent = 'Customer since ' + row.getAttribute('data-since') + '. Fixes apply to every past and future job.';

        // Whatever the server said about an earlier try no longer applies.
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
    // A click on the dimmed backdrop (outside the window) closes it, like Cancel.
    dialog.addEventListener('click', function (e) {
        if (e.target === dialog) dialog.close();
    });

    // Enter in a box saves. Left alone it would press the page's first submit button (Add customer).
    dialog.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') {
            e.preventDefault();
            saveBtn.click();
        }
    });

    // The server refused a save: show the window again as it was sent.
    if (dialog.getAttribute('data-open') === 'true') {
        dialog.showModal();
        nameBox.focus();
    }
})();
