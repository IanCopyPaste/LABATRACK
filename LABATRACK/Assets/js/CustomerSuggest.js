(function () {
    var box = document.querySelector('[data-suggest]');
    if (!box) return;
    var list = document.getElementById(box.getAttribute('data-suggest'));
    if (!list) return;
    var items = Array.prototype.slice.call(list.querySelectorAll('li[data-number]'));
    var emptyRow = list.querySelector('.suggest-empty');
    var findBtn = document.querySelector('button[name="action"][value="find"]');
    var active = -1;
    var shown = [];
    var MAX = 8;

    box.setAttribute('role', 'combobox');
    box.setAttribute('aria-autocomplete', 'list');
    box.setAttribute('aria-expanded', 'false');
    box.setAttribute('aria-controls', list.id);

    function digitsOf(text) { return text.replace(/\D/g, ''); }

    function matches() {
        var typed = digitsOf(box.value);
        if (typed === '') return items.slice(0, MAX);
        var starts = [], contains = [];
        items.forEach(function (li) {
            var d = li.getAttribute('data-digits');
            var at = d.indexOf(typed);
            if (at === 0) starts.push(li); else if (at > 0) contains.push(li);
        });
        return starts.concat(contains).slice(0, MAX);
    }

    function highlight(index) {
        active = index;
        shown.forEach(function (li, i) {
            li.classList.toggle('is-active', i === active);
            li.setAttribute('aria-selected', i === active ? 'true' : 'false');
        });
        if (active >= 0 && shown[active].scrollIntoView) shown[active].scrollIntoView({ block: 'nearest' });
    }

    function close() {
        list.hidden = true;
        box.setAttribute('aria-expanded', 'false');
        active = -1;
    }

    function render() {
        shown = matches();
        var typed = digitsOf(box.value);
        if (shown.length === 1 && typed !== '' && shown[0].getAttribute('data-digits') === typed) { close(); return; }
        items.forEach(function (li) { li.hidden = shown.indexOf(li) < 0; });
        shown.forEach(function (li) { list.insertBefore(li, emptyRow); });
        emptyRow.hidden = shown.length > 0 || items.length === 0;
        if (shown.length === 0 && items.length === 0) { close(); return; }
        list.hidden = false;
        box.setAttribute('aria-expanded', 'true');
        highlight(-1);
    }

    function choose(li) {
        box.value = li.getAttribute('data-number');
        box.dispatchEvent(new Event('input', { bubbles: true }));
        close();
        if (findBtn) findBtn.click();
    }

    box.addEventListener('focus', render);
    box.addEventListener('input', render);
    box.addEventListener('keydown', function (e) {
        if (list.hidden) {
            if (e.key === 'ArrowDown') { render(); e.preventDefault(); }
            return;
        }
        if (e.key === 'ArrowDown') { highlight(Math.min(shown.length - 1, active + 1)); e.preventDefault(); }
        else if (e.key === 'ArrowUp') { highlight(Math.max(0, active - 1)); e.preventDefault(); }
        else if (e.key === 'Escape') { close(); e.preventDefault(); }
        else if (e.key === 'Enter' && active >= 0) { e.preventDefault(); e.stopPropagation(); choose(shown[active]); }
    }, true);
    list.addEventListener('mousedown', function (e) {
        var li = e.target.closest('li[data-number]');
        if (li) { e.preventDefault(); choose(li); }
    });
    list.addEventListener('mousemove', function (e) {
        var li = e.target.closest('li[data-number]');
        if (li) highlight(shown.indexOf(li));
    });
    box.addEventListener('blur', function () { setTimeout(close, 120); });
})();
