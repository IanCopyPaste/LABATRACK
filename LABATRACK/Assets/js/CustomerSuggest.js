// Pages/Shared/NewJob.aspx
// The dropdown of saved contact numbers under the contact box, so a returning customer is one click
// away. The page writes every saved customer into the list (most recent first) as <li data-number
// data-digits data-name>; this only filters and shows it. It matches the digits typed anywhere in a
// number, numbers that start with them first. Clicking a number (or pressing Enter on the highlighted
// one) fills the box and presses Find, which fills in the customer. Arrow keys move, Esc closes.
// Display only: Find, and the save, check the number on the server.
(function () {
    var box = document.querySelector('[data-suggest]');
    if (!box) return;
    var list = document.getElementById(box.getAttribute('data-suggest'));
    if (!list) return;
    var items = Array.prototype.slice.call(list.querySelectorAll('li[data-number]'));
    var emptyRow = list.querySelector('.suggest-empty');
    var findBtn = document.querySelector('button[name="action"][value="find"]');
    var active = -1;      // the highlighted row among the ones shown
    var shown = [];
    var MAX = 8;

    box.setAttribute('role', 'combobox');
    box.setAttribute('aria-autocomplete', 'list');
    box.setAttribute('aria-expanded', 'false');
    box.setAttribute('aria-controls', list.id);

    function digitsOf(text) { return text.replace(/\D/g, ''); }

    // The rows that match what is typed: numbers starting with it first, then numbers containing it.
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
        // A number typed in full that is already the only match needs no list.
        if (shown.length === 1 && typed !== '' && shown[0].getAttribute('data-digits') === typed) { close(); return; }
        items.forEach(function (li) { li.hidden = shown.indexOf(li) < 0; });
        // The rows are shown in match order, not page order.
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
        if (findBtn) findBtn.click();   // Find fills in the customer, with the spinner from Loading.js
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
    // mousedown, not click: the box loses focus on mousedown, which would close the list before a click lands.
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
