(function () {
    var box = document.querySelector('[data-board-filter]');
    var status = document.getElementById('filterCount');
    var columns = document.querySelectorAll('[data-board-col]');
    if (!box) return;

    function apply() {
        var term = box.value.trim().toLowerCase();
        var shown = 0, total = 0;

        Array.prototype.forEach.call(columns, function (col) {
            var cards = col.querySelectorAll('.job-card');
            var inColumn = 0;
            Array.prototype.forEach.call(cards, function (card) {
                var match = term === '' || card.getAttribute('data-search').indexOf(term) !== -1;
                card.hidden = !match;
                if (match) { inColumn++; }
            });
            col.querySelector('[data-col-count]').textContent = inColumn;
            col.className = 'board-col' + (inColumn === 0 ? ' is-empty' : '');
            shown += inColumn;
            total += cards.length;
        });

        status.textContent = term === '' ? '' : shown + ' of ' + total + ' loads match';
    }

    box.addEventListener('input', apply);
    box.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { box.value = ''; apply(); }
    });
    box.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); }
    });
    apply();
})();
