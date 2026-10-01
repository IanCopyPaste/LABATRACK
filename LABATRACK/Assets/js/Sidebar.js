(function () {
    var sidebar = document.querySelector('[data-sidebar]');
    var button = document.querySelector('[data-sidebar-toggle]');
    if (!sidebar || !button) return;

    button.addEventListener('click', function () {
        var collapse = !sidebar.classList.contains('is-collapsed');
        var label = collapse ? 'Expand the menu' : 'Collapse the menu';
        sidebar.classList.toggle('is-collapsed', collapse);
        button.setAttribute('aria-expanded', collapse ? 'false' : 'true');
        button.setAttribute('aria-label', label);
        button.title = label;
        document.cookie = 'lt-sidebar=' + (collapse ? 'collapsed' : 'open') + '; path=/; max-age=31536000; SameSite=Lax';
    });
})();
