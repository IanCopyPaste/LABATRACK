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
        try { sessionStorage.setItem('lt-stage-filter', stage); } catch (e) {  }
    }

    try {
        var saved = sessionStorage.getItem('lt-stage-filter');
        if (saved !== null && select.querySelector('option[value="' + saved + '"]')) select.value = saved;
    } catch (e) {  }

    select.addEventListener('change', apply);
    apply();
})();
