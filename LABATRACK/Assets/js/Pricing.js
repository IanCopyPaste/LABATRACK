// Admin/Pricing.aspx
// One edit window (a <dialog>) for services, service add-ons and product add-ons. Edit fills it
// from the clicked row; Add opens it empty. DRAFT: Save only updates the table on this page.
// Phase 2 posts the same fields to the server, which checks them again and saves them to
// Services or AddOns; the checks below are there so the owner sees a mistake straight away.
(function () {
    var dialog = document.getElementById('priceDialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;

    var nameBox = document.getElementById('pdName');
    var priceBox = document.getElementById('pdPrice');
    var activeBox = document.getElementById('pdActive');
    var nameError = document.getElementById('pdNameError');
    var priceError = document.getElementById('pdPriceError');

    // What changes between the three lists: the words used, and what the price means.
    var kinds = {
        'service': {
            noun: 'service', priceLabel: 'Rate per kg',
            hint: 'Charged per kilo, after the weight is rounded up.'
        },
        'service-addon': {
            noun: 'service add-on', priceLabel: 'Price',
            hint: 'Charged once per job, whatever the weight.'
        },
        'product': {
            noun: 'product add-on', priceLabel: 'Price each',
            hint: 'Billed as quantity × this price.'
        }
    };

    var kind = null;     // which list the window is working on
    var row = null;      // the row being edited, or null when adding

    function peso(n) {
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function cellsOf(tr) {
        return {
            name: tr.cells[0].textContent.trim(),
            price: parseFloat(tr.cells[1].textContent.replace(/[₱,\s]/g, '')),
            active: tr.cells[2].textContent.trim() === 'Active'
        };
    }

    function showError(el, text) {
        el.textContent = text;
        el.hidden = !text;
    }

    function open(k, tr) {
        kind = k;
        row = tr;
        var info = kinds[k];
        var title = (tr ? 'Edit ' : 'Add ') + info.noun;
        document.getElementById('priceDialogTitle').textContent = title.charAt(0).toUpperCase() + title.slice(1);
        document.getElementById('pdPriceLabel').textContent = info.priceLabel;
        document.getElementById('pdSaveText').textContent = tr ? 'Save changes' : 'Add ' + info.noun;
        showError(nameError, '');
        showError(priceError, '');

        if (tr) {
            var current = cellsOf(tr);
            nameBox.value = current.name;
            priceBox.value = current.price.toFixed(2);
            activeBox.checked = current.active;
            document.getElementById('priceDialogSub').textContent = 'Now ' + peso(current.price) + (current.active ? '' : ' · inactive');
            document.getElementById('pdPriceHint').textContent = info.hint + ' Was ' + peso(current.price) + '.';
        } else {
            nameBox.value = '';
            priceBox.value = '';
            activeBox.checked = true;
            document.getElementById('priceDialogSub').textContent = 'It will show on new job orders once saved.';
            document.getElementById('pdPriceHint').textContent = info.hint;
        }

        dialog.showModal();
        nameBox.focus();
        nameBox.select();
    }

    function validate() {
        var ok = true;
        var name = nameBox.value.trim();
        var priceText = priceBox.value.trim().replace(/,/g, '');

        if (name === '') {
            showError(nameError, 'Enter a name.'); ok = false;
        } else {
            // Two items with the same name in one list would be impossible to tell apart at the counter.
            var table = document.querySelector('[data-price-table="' + kind + '"]');
            var clash = Array.prototype.some.call(table.tBodies[0].rows, function (tr) {
                return tr !== row && cellsOf(tr).name.toLowerCase() === name.toLowerCase();
            });
            showError(nameError, clash ? 'There is already one called "' + name + '" in this list.' : '');
            if (clash) ok = false;
        }

        // Money is DECIMAL(10,2): digits only, at most two places after the point, more than zero.
        if (!/^\d{1,6}(\.\d{1,2})?$/.test(priceText) || parseFloat(priceText) <= 0) {
            showError(priceError, 'Enter an amount in pesos, for example 40 or 42.50.'); ok = false;
        } else {
            showError(priceError, '');
        }
        return ok;
    }

    function paint(tr, name, price, active) {
        tr.cells[0].textContent = name;
        tr.cells[1].textContent = peso(price);
        tr.cells[0].className = 'cell-main' + (active ? '' : ' muted');
        tr.cells[1].className = 'right num' + (active ? '' : ' muted');
        var pill = document.createElement('span');
        pill.className = 'pill ' + (active ? 'pill-ok' : 'st-claimed');
        pill.textContent = active ? 'Active' : 'Inactive';
        tr.cells[2].textContent = '';
        tr.cells[2].appendChild(pill);

        // Picks the changed row out for a moment so the owner sees where it went.
        tr.classList.remove('is-changed');
        void tr.offsetWidth;
        tr.classList.add('is-changed');
    }

    function save() {
        if (!validate()) return;
        var name = nameBox.value.trim();
        var price = parseFloat(priceBox.value.replace(/,/g, ''));
        var target = row;
        if (!target) {
            // A new row copies an existing one so it keeps the same cells and Edit button.
            var body = document.querySelector('[data-price-table="' + kind + '"]').tBodies[0];
            target = body.rows[0].cloneNode(true);
            body.appendChild(target);
        }
        paint(target, name, price, activeBox.checked);
        dialog.close();
    }

    document.addEventListener('click', function (e) {
        var edit = e.target.closest('[data-price-edit]');
        if (edit) {
            var tr = edit.closest('tr');
            open(tr.closest('[data-price-table]').getAttribute('data-price-table'), tr);
            return;
        }
        var add = e.target.closest('[data-price-add]');
        if (add) open(add.getAttribute('data-price-add'), null);
    });

    document.getElementById('pdSave').addEventListener('click', save);
    Array.prototype.forEach.call(dialog.querySelectorAll('[data-modal-close]'), function (b) {
        b.addEventListener('click', function () { dialog.close(); });
    });

    // A click on the dimmed backdrop (outside the window) closes it, like Cancel.
    dialog.addEventListener('click', function (e) {
        if (e.target === dialog) dialog.close();
    });

    // The window sits inside the page's one server form, so Enter would post the whole page.
    // Here it saves the window instead.
    dialog.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') {
            e.preventDefault();
            save();
        }
    });
})();
