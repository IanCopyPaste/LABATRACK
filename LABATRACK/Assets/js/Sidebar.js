// Shared: every page with the menu (NavBar.cs).
// The button at the top of the menu collapses it to its icons, or opens it again. The choice is
// kept in a cookie for a year, and NavBar.cs reads it, so the next page is drawn the same way
// straight away (no flash of the open menu). Display only.
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
