// Tooltips for the charts drawn by Helpers/SvgChart.cs (Admin/Reports.aspx).
// Every mark that can show a tooltip carries data-tip-title and data-tip-rows (JSON rows of
// [name, value, colour]). The same tooltip appears on hover and on keyboard focus, and it
// only repeats what the table under each chart already shows. Names are put in with
// textContent, never as HTML, so a customer's name can never turn into markup.
(function () {
    var tip = document.createElement('div');
    tip.className = 'chart-tip';
    tip.setAttribute('role', 'tooltip');
    tip.hidden = true;
    document.body.appendChild(tip);

    var current = null;

    function build(el) {
        var rows;
        try { rows = JSON.parse(el.getAttribute('data-tip-rows') || '[]'); } catch (e) { rows = []; }

        tip.textContent = '';
        var title = document.createElement('div');
        title.className = 'tip-title';
        title.textContent = el.getAttribute('data-tip-title');
        tip.appendChild(title);

        rows.forEach(function (r) {
            var row = document.createElement('div');
            row.className = 'tip-row';
            if (r[2]) {
                // A short stroke of the series colour identifies the row; the text stays in ink.
                var key = document.createElement('i');
                key.className = 'key-line';
                key.style.background = r[2];
                row.appendChild(key);
            }
            var value = document.createElement('b');
            value.textContent = r[1];
            var name = document.createElement('span');
            name.textContent = r[0];
            row.appendChild(value);
            row.appendChild(name);
            tip.appendChild(row);
        });
    }

    function place(x, y) {
        var w = tip.offsetWidth, h = tip.offsetHeight;
        var left = x + 14, top = y + 14;
        if (left + w > window.innerWidth - 8) left = x - w - 14;
        if (top + h > window.innerHeight - 8) top = y - h - 14;
        tip.style.left = Math.max(8, left) + 'px';
        tip.style.top = Math.max(8, top) + 'px';
    }

    // Line charts: move the hairline to the point being read.
    function crosshair(el, on) {
        var svg = el.closest('svg');
        var line = svg && svg.querySelector('.crosshair');
        if (!line) return;
        var x = el.getAttribute('data-x');
        if (on && x) {
            line.setAttribute('x1', x);
            line.setAttribute('x2', x);
            line.classList.add('on');
        } else {
            line.classList.remove('on');
        }
    }

    function show(el, x, y) {
        if (current !== el) {
            if (current) crosshair(current, false);
            current = el;
            build(el);
            crosshair(el, true);
        }
        tip.hidden = false;
        place(x, y);
    }

    function hide() {
        if (current) crosshair(current, false);
        current = null;
        tip.hidden = true;
    }

    function target(e) {
        return e.target.closest ? e.target.closest('[data-tip-title]') : null;
    }

    document.addEventListener('pointermove', function (e) {
        var el = target(e);
        if (el) show(el, e.clientX, e.clientY);
        else if (current) hide();
    });

    document.addEventListener('focusin', function (e) {
        var el = target(e);
        if (!el) return;
        var box = el.getBoundingClientRect();
        show(el, box.left + box.width / 2, box.top);
    });

    document.addEventListener('focusout', hide);
    document.addEventListener('scroll', hide, true);
})();
