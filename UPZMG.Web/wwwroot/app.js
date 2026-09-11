// ---------------------------------------------------------------------------
// SIGIE — small progressive-enhancement layer.
// Purely cosmetic: if this file fails to load the app still works, it just
// loses the condensed / auto-hiding header. No Blazor interop, no dependencies.
// ---------------------------------------------------------------------------
(function () {
    'use strict';

    var SCROLLED = 'sigie-scrolled';   // past the top → header condenses
    var HIDDEN = 'sigie-header-hidden'; // scrolling down → header slides away

    var TOP_THRESHOLD = 8;    // treat anything above this as "at the top"
    var HIDE_AFTER = 120;     // don't hide until the user is genuinely scrolling
    var DELTA_MIN = 6;        // ignore jitter / rubber-banding

    var lastY = 0;
    var ticking = false;

    function offset() {
        // MudLayout normally scrolls the document, but a fixed-height layout
        // scrolls .mud-main-content instead — read whichever is actually moving.
        var doc = window.pageYOffset || document.documentElement.scrollTop || 0;
        if (doc > TOP_THRESHOLD) return doc;
        var main = document.querySelector('.mud-main-content');
        return main && main.scrollTop ? main.scrollTop : doc;
    }

    function sync() {
        ticking = false;

        var y = offset();
        var body = document.body;

        body.classList.toggle(SCROLLED, y > TOP_THRESHOLD);

        var delta = y - lastY;
        if (y <= TOP_THRESHOLD) {
            body.classList.remove(HIDDEN);          // at the top: always visible
        } else if (Math.abs(delta) >= DELTA_MIN) {
            if (delta > 0 && y > HIDE_AFTER) {
                body.classList.add(HIDDEN);         // scrolling down
            } else if (delta < 0) {
                body.classList.remove(HIDDEN);      // scrolling up
            }
        }

        lastY = y;
    }

    function onScroll() {
        if (ticking) return;
        ticking = true;
        window.requestAnimationFrame(sync);
    }

    // A menu/dropdown opening while the header is hidden would look broken.
    function reveal() { document.body.classList.remove(HIDDEN); }

    // Ctrl/Cmd+K jumps to the module search, the way every tool this audience
    // already uses behaves. Escape gives the field back.
    function shortcuts(e) {
        var input = document.querySelector('.sigie-quicknav input');

        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            if (!input) return;
            e.preventDefault();
            reveal();
            input.focus();
            input.select();
            return;
        }

        if (e.key === 'Escape' && input && document.activeElement === input) {
            input.blur();
        }
    }

    window.addEventListener('keydown', shortcuts);

    // Streams a generated file (CSV export) to the browser without a round-trip.
    window.sigieDownload = function (filename, text, mime) {
        // The BOM makes Excel open UTF-8 accents correctly.
        var blob = new Blob(['﻿' + text], { type: mime || 'text/csv;charset=utf-8;' });
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');

        link.href = url;
        link.download = filename;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    };

    document.addEventListener('click', function (e) {
        var btn = e.target && e.target.closest ? e.target.closest('[data-sigie-reveal]') : null;
        if (!btn) { return; }

        var input = document.getElementById(btn.getAttribute('data-sigie-reveal'));
        if (!input) { return; }

        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';

        btn.setAttribute('aria-pressed', show ? 'true' : 'false');
        btn.setAttribute('aria-label', show ? 'Ocultar contraseña' : 'Mostrar contraseña');
        btn.classList.toggle('is-on', show);

        input.focus();
    });

    window.addEventListener('scroll', onScroll, { passive: true, capture: true });
    window.addEventListener('resize', onScroll, { passive: true });
    window.addEventListener('keydown', reveal, { passive: true });
    document.addEventListener('DOMContentLoaded', sync);
    sync();
})();
