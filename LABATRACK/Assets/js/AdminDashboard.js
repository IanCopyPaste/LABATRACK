// Admin/Dashboard.aspx
// The Stage filter above "Active job orders": shows only the loads at the chosen stage (the server
// writes each row's stage in data-stage-row, and each choice's count). The choice is remembered for
// this browser tab, so it stays after a refresh. Display only: every row is on the page, the filter
// just hides the others.
(function () {
    var select = document.querySelector('[data-stage-filter]');
    var rows = document.querySelectorAll('[data-stage-row]');
    var table = document.getElementById('activeTable');
    var empty = document.getElementById('stageEmpty');
    var caption = document.getElementById('activeCaption');
    if (!select || rows.length === 0) return;

    var allCaption = caption.textContent;

    function apply() {
        var stage = select.value, shown = 0;
        Array.prototype.forEach.call(rows, function (row) {
            var match = stage === '' || row.getAttribute('data-stage-row') === stage;
            row.hidden = !match;
            if (match) { shown++; }
        });
        table.hidden = shown === 0;
        empty.hidden = shown !== 0;
        caption.textContent = stage === ''
            ? allCaption
            : shown + (shown === 1 ? ' load' : ' loads') + ' at ' + stage + ', newest drop-off first';
        try { sessionStorage.setItem('lt-stage-filter', stage); } catch (e) { /* storage off: fine */ }
    }

    try {
        var saved = sessionStorage.getItem('lt-stage-filter');
        if (saved !== null && select.querySelector('option[value="' + saved + '"]')) select.value = saved;
    } catch (e) { /* storage off: start with All stages */ }

    select.addEventListener('change', apply);
    apply();
})();
