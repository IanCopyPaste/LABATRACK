(function () {
    var dialog = document.getElementById('userDialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;

    function $(id) { return document.getElementById(id); }
    var idBox = $('udId'), nameBox = $('udUsername'), passBox = $('udPassword'), activeBox = $('udActive');
    var deleteBtn = $('udDelete'), saveBtn = $('udSave');

    function show(el, visible) { el.hidden = !visible; }

    function paintActive() {
        $('udActiveText').textContent = activeBox.checked ? 'Active · can sign in' : 'Inactive · can\'t sign in';
    }

    function open(row) {
        var manage = row !== null;
        var username = manage ? row.getAttribute('data-username') : '';
        var hasHistory = manage && row.getAttribute('data-history') === '1';

        idBox.value = manage ? row.getAttribute('data-user-id') : '';
        nameBox.value = username;
        passBox.value = '';
        activeBox.checked = manage ? row.getAttribute('data-active') === '1' : true;
        paintActive();

        $('userDialogTitle').textContent = manage ? 'Manage ' + username : 'Add a staff account';
        $('userDialogSub').textContent = manage ? row.getAttribute('data-status')
                                                : 'New accounts get the Staff role and sign in at the counter login.';
        $('udPasswordLabel').textContent = manage ? 'New password' : 'Password';
        $('udPasswordHint').textContent = manage
            ? 'Leave it blank to keep their current password.'
            : 'At least 8 characters. Share it with them in person.';
        $('udSaveText').textContent = manage ? 'Save changes' : 'Create account';
        show($('udRenameHint'), manage);

        show(deleteBtn, manage);
        deleteBtn.disabled = hasHistory;
        show($('udDeleteNote'), hasHistory);
        deleteBtn.setAttribute('data-confirm-text', 'Delete ' + username + ' for good? It has no jobs, payments or history, '
            + 'so nothing else changes. This can\'t be undone.');

        ['udError', 'udUsernameError', 'udPasswordError'].forEach(function (id) { show($(id), false); });

        dialog.showModal();
        nameBox.focus();
        nameBox.select();
    }

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-user-add]')) { open(null); return; }
        var manage = e.target.closest('[data-user-manage]');
        if (manage) open(manage.closest('tr'));
    });

    activeBox.addEventListener('change', paintActive);

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
