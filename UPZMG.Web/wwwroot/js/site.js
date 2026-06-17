// ============================================================
// UPZMG — Site JavaScript
// Sidebar toggle, progress bar animation, and countUp effect
// ============================================================

document.addEventListener('DOMContentLoaded', function () {

    // === Sidebar Toggle (Mobile) ===
    const sidebar = document.getElementById('sidebar');
    const toggle = document.getElementById('sidebarToggle');
    const overlay = document.getElementById('sidebarOverlay');

    if (toggle && sidebar && overlay) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
            overlay.classList.toggle('active');
        });

        overlay.addEventListener('click', function () {
            sidebar.classList.remove('open');
            overlay.classList.remove('active');
        });
    }

    // === Animate Progress Bars ===
    const progressBars = document.querySelectorAll('.capture-progress-fill[data-width]');
    if (progressBars.length > 0) {
        // Use IntersectionObserver for scroll-triggered animation
        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    const bar = entry.target;
                    setTimeout(function () {
                        bar.style.width = bar.getAttribute('data-width');
                    }, 200);
                    observer.unobserve(bar);
                }
            });
        }, { threshold: 0.2 });

        progressBars.forEach(function (bar) {
            observer.observe(bar);
        });
    }

    // === CountUp Animation for Stat Cards ===
    const statValues = document.querySelectorAll('.stat-card-value');
    statValues.forEach(function (el) {
        var text = el.textContent.trim();

        // Try to extract a numeric value (handles $, %, commas)
        var cleaned = text.replace(/[$,%]/g, '').replace(/,/g, '');
        var numericVal = parseFloat(cleaned);

        // Only animate if we got a valid number
        if (!isNaN(numericVal) && numericVal > 0) {
            var prefix = '';
            var suffix = '';

            // Detect prefix ($)
            if (text.indexOf('$') === 0) prefix = '$';

            // Detect suffix (%)
            if (text.indexOf('%') !== -1) suffix = '%';

            // Determine decimal places
            var decimalPlaces = 0;
            var dotIndex = cleaned.indexOf('.');
            if (dotIndex !== -1) {
                decimalPlaces = cleaned.length - dotIndex - 1;
            }

            // Detect if original used commas (number formatting)
            var useCommas = text.replace(/[$%]/g, '').indexOf(',') !== -1;

            animateValue(el, 0, numericVal, 1200, prefix, suffix, decimalPlaces, useCommas);
        }
    });

    function animateValue(element, start, end, duration, prefix, suffix, decimals, useCommas) {
        var startTime = null;

        function step(timestamp) {
            if (!startTime) startTime = timestamp;
            var progress = Math.min((timestamp - startTime) / duration, 1);

            // Ease out cubic
            var eased = 1 - Math.pow(1 - progress, 3);
            var current = start + (end - start) * eased;

            var formatted = current.toFixed(decimals);
            if (useCommas) {
                var parts = formatted.split('.');
                parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                formatted = parts.join('.');
            }

            element.textContent = prefix + formatted + suffix;

            if (progress < 1) {
                requestAnimationFrame(step);
            }
        }

        requestAnimationFrame(step);
    }

    // === Active Nav Highlight ===
    const navLinks = document.querySelectorAll('.sidebar-nav-link');
    var currentPath = window.location.pathname.toLowerCase();

    navLinks.forEach(function (link) {
        var href = link.getAttribute('href');
        if (href) {
            var linkPath = href.toLowerCase();
            // Remove trailing slashes for comparison
            currentPath = currentPath.replace(/\/$/, '');
            linkPath = linkPath.replace(/\/$/, '');

            if (currentPath === linkPath || currentPath.startsWith(linkPath + '/')) {
                link.classList.add('active');
            }
        }
    });

});
