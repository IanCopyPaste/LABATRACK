// Pages/Shared/Dashboard.aspx
// Filters the board as staff type a claim number, name, or contact number. Display only:
// hidden cards are still on the page, and the filter text survives a card move because
// the box is a server TextBox.
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
    // Esc clears the search, the quickest way back to the whole board.
    box.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { box.value = ''; apply(); }
    });
    // Enter would submit the form and reload the page for nothing.
    box.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); }
    });
    apply();
})();
