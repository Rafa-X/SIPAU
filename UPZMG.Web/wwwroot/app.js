// ---------------------------------------------------------------------------
// SIGIE — small progressive-enhancement layer.
// Purely cosmetic: if this file fails to load the app still works, it just
// loses the condensed header. No Blazor interop, no dependencies.
// ---------------------------------------------------------------------------
(function () {
    'use strict';

    var SCROLLED = 'sigie-scrolled';
    var THRESHOLD = 8;
    var ticking = false;

    function offset() {
        // MudLayout normally scrolls the document, but a fixed-height layout
        // scrolls .mud-main-content instead — read whichever is actually moving.
        var doc = window.pageYOffset || document.documentElement.scrollTop || 0;
        if (doc > THRESHOLD) return doc;
        var main = document.querySelector('.mud-main-content');
        return main ? main.scrollTop : 0;
    }

    function sync() {
        ticking = false;
        document.body.classList.toggle(SCROLLED, offset() > THRESHOLD);
    }

    function onScroll() {
        if (ticking) return;
        ticking = true;
        window.requestAnimationFrame(sync);
    }

    window.addEventListener('scroll', onScroll, { passive: true, capture: true });
    window.addEventListener('resize', onScroll, { passive: true });
    document.addEventListener('DOMContentLoaded', sync);
    sync();
})();
