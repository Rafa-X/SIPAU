// ============================================================
//  SIGIE — Site JavaScript
//  Nav toggle (Menú), user menu, and content animations.
// ============================================================

document.addEventListener('DOMContentLoaded', function () {
    const layout = document.getElementById('appLayout');
    const navToggle = document.getElementById('navToggle');
    const backdrop = document.getElementById('appBackdrop');

    // === Nav drawer (mobile only; on desktop the sidebar is always visible) ===
    if (layout && navToggle) {
        navToggle.addEventListener('click', function () {
            layout.classList.toggle('nav-open');
        });
    }

    if (layout && backdrop) {
        backdrop.addEventListener('click', function () {
            layout.classList.remove('nav-open');
        });
    }

    // Close the mobile drawer after navigating (link click)
    document.querySelectorAll('.sidebar-nav-link').forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth <= 900) layout.classList.remove('nav-open');
        });
    });

    // === User menu dropdown ===
    const userTrigger = document.getElementById('userMenuTrigger');
    const userDropdown = document.getElementById('userMenuDropdown');
    if (userTrigger && userDropdown) {
        userTrigger.addEventListener('click', function (e) {
            e.stopPropagation();
            const open = userDropdown.classList.toggle('is-open');
            userTrigger.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
        document.addEventListener('click', function (e) {
            if (!userDropdown.contains(e.target) && !userTrigger.contains(e.target)) {
                userDropdown.classList.remove('is-open');
                userTrigger.setAttribute('aria-expanded', 'false');
            }
        });
    }

});
