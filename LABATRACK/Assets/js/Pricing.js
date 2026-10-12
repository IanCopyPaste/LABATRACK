(function () {
    var dialog = document.getElementById('priceDialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;

    var kindBox = document.getElementById('pdKind');
    var idBox = document.getElementById('pdId');
    var nameBox = document.getElementById('pdName');
    var priceBox = document.getElementById('pdPrice');
    var activeBox = document.getElementById('pdActive');
    var saveBtn = document.getElementById('pdSave');
    var nameError = document.getElementById('pdNameError');
    var priceError = document.getElementById('pdPriceError');
    var stagesField = document.getElementById('pdStagesField');
    var stagesError = document.getElementById('pdStagesError');
    var routeHint = document.getElementById('pdRoute');
    var stageBoxes = stagesField.querySelectorAll('[data-stage]');

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

    var kind = null;
    var row = null;

    function peso(n) {
        return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function cell(tr, col) {
        return tr.querySelector('[data-col="' + col + '"]');
    }

    function cellsOf(tr) {
        return {
            name: cell(tr, 'name').textContent.trim(),
            price: parseFloat(cell(tr, 'price').textContent.replace(/[₱,\s]/g, '')),
            active: cell(tr, 'status').querySelector('.pill').textContent.trim() === 'Active',
            stages: tr.getAttribute('data-stages') || ''
        };
    }

    function showError(el, text) {
        el.textContent = text;
        el.hidden = !text;
    }

    function stageCode() {
        return Array.prototype.map.call(stageBoxes, function (box) {
            return box.checked ? box.getAttribute('data-stage').charAt(0) : '';
        }).join('');
    }

    function paintRoute() {
        var work = Array.prototype.filter.call(stageBoxes, function (box) { return box.checked; })
            .map(function (box) { return box.getAttribute('data-stage'); });
        routeHint.textContent = work.length === 0
            ? ''
            : 'Route: ' + ['Queued'].concat(work, ['Inspection', 'Ready for pick-up']).join(' → ');
    }

    function open(k, tr) {
        kind = k;
        row = tr;
        var info = kinds[k];
        var title = (tr ? 'Edit ' : 'Add ') + info.noun;
        kindBox.value = k;
        idBox.value = tr ? tr.getAttribute('data-id') : '';
        document.getElementById('priceDialogTitle').textContent = title.charAt(0).toUpperCase() + title.slice(1);
        document.getElementById('pdPriceLabel').textContent = info.priceLabel;
        document.getElementById('pdSaveText').textContent = tr ? 'Save changes' : 'Add ' + info.noun;
        showError(nameError, '');
        showError(priceError, '');
        stagesError.hidden = true;
        stagesField.hidden = k !== 'service';

        var current = tr ? cellsOf(tr) : { name: '', price: NaN, active: true, stages: 'WDF' };
        nameBox.value = current.name;
        priceBox.value = tr ? current.price.toFixed(2) : '';
        activeBox.checked = current.active;
        Array.prototype.forEach.call(stageBoxes, function (box) {
            box.checked = k === 'service' && current.stages.indexOf(box.getAttribute('data-stage').charAt(0)) >= 0;
        });
        paintRoute();

        if (tr) {
            document.getElementById('priceDialogSub').textContent = 'Now ' + peso(current.price) + (current.active ? '' : ' · inactive');
            document.getElementById('pdPriceHint').textContent = info.hint + ' Was ' + peso(current.price) + '.';
        } else {
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
            var table = document.querySelector('[data-price-table="' + kind + '"]');
            var clash = Array.prototype.some.call(table.tBodies[0].rows, function (tr) {
                return tr !== row && cellsOf(tr).name.toLowerCase() === name.toLowerCase();
            });
            showError(nameError, clash ? 'There is already one called "' + name + '" in this list.' : '');
            if (clash) ok = false;
        }

        if (!/^\d{1,5}(\.\d{1,2})?$/.test(priceText) || parseFloat(priceText) <= 0) {
            showError(priceError, 'Enter an amount in pesos, for example 40 or 42.50.'); ok = false;
        } else {
            showError(priceError, '');
        }

        if (kind === 'service' && stageCode() === '') {
            stagesError.hidden = false; ok = false;
        } else {
            stagesError.hidden = true;
        }
        return ok;
    }

    document.addEventListener('click', function (e) {
        var edit = e.target.closest('[data-price-edit]');
        if (edit) {
            if (edit.disabled) return;
            var tr = edit.closest('tr');
            open(tr.closest('[data-price-table]').getAttribute('data-price-table'), tr);
            return;
        }
        var add = e.target.closest('[data-price-add]');
        if (add) open(add.getAttribute('data-price-add'), null);
    });

    Array.prototype.forEach.call(stageBoxes, function (box) {
        box.addEventListener('change', function () {
            paintRoute();
            stagesError.hidden = stageCode() !== '';
        });
    });

    saveBtn.addEventListener('click', function (e) {
        if (!validate()) e.preventDefault();
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
})();
