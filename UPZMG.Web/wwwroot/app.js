// ---------------------------------------------------------------------------
// SIPAU — small progressive-enhancement layer.
// Purely cosmetic: if this file fails to load the app still works, it just
// loses the condensed / auto-hiding header. No Blazor interop, no dependencies.
// ---------------------------------------------------------------------------
(function () {
    'use strict';

    var SCROLLED = 'sipau-scrolled';   // past the top → header condenses
    var HIDDEN = 'sipau-header-hidden'; // scrolling down → header slides away

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
        var input = document.querySelector('.sipau-quicknav input');

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

    // Export the chart plot together with its HTML axes and legend.
    window.sigieDownloadChartPng = function (chartId, filename, title) {
        var chart = document.getElementById(chartId);
        var source = chart && chart.querySelector('svg');
        if (!source) return;

        // Ask for the destination while the click still has user activation.
        var saveFile = null;
        try {
            saveFile = window.showSaveFilePicker
                ? window.showSaveFilePicker({
                    suggestedName: filename || 'grafica.png',
                    types: [{ description: 'Imagen PNG', accept: { 'image/png': ['.png'] } }]
                })
                : null;
        } catch (error) {
            console.error('No se pudo abrir el selector de archivos:', error);
        }

        var chartRect = chart.getBoundingClientRect();
        var svgRect = source.getBoundingClientRect();
        var card = chart.closest('.sipau-card');
        var header = card && card.querySelector('.sipau-card-head');
        var footer = card && card.querySelector('.sipau-card-foot');
        var iconPath = header && header.querySelector('.sipau-icon-badge path');
        var width = Math.max(1, Math.round(chartRect.width));
        var titleHeight = title ? 64 : 0;
        var footerHeight = footer ? Math.round(footer.getBoundingClientRect().height) : 0;
        var height = Math.max(1, Math.round(chartRect.height)) + titleHeight + footerHeight;
        var scale = 2;
        var canvas = document.createElement('canvas');
        canvas.width = width * scale;
        canvas.height = height * scale;
        var context = canvas.getContext('2d');
        if (!context) return;
        context.scale(scale, scale);

        var chartStyles = getComputedStyle(chart);
        context.fillStyle = chartStyles.backgroundColor === 'rgba(0, 0, 0, 0)'
            ? '#ffffff'
            : chartStyles.backgroundColor;
        context.fillRect(0, 0, width, height);

        if (title) {
            var iconSize = 34;
            var iconX = 0;
            var iconY = 10;
            var iconPathData = iconPath && iconPath.getAttribute('d');
            var titleX = iconPathData ? iconSize + 14 : 0;

            if (iconPathData) {
                var badge = header.querySelector('.sipau-icon-badge');
                var badgeStyles = getComputedStyle(badge);
                context.fillStyle = badgeStyles.backgroundColor;
                context.fillRect(iconX, iconY, iconSize, iconSize);
                context.save();
                context.translate(iconX + 8, iconY + 8);
                context.scale(18 / 24, 18 / 24);
                context.fillStyle = getComputedStyle(iconPath).fill;
                context.fill(new Path2D(iconPathData));
                context.restore();
            }

            context.font = '700 18px Archivo, sans-serif';
            context.fillStyle = getComputedStyle(chart).color;
            context.textAlign = 'left';
            context.textBaseline = 'middle';
            context.fillText(title, titleX, 27);

            context.fillStyle = header ? getComputedStyle(header).borderBottomColor : '#D7E1EC';
            context.fillRect(0, titleHeight - 1, width, 1);
        }

        var clone = source.cloneNode(true);
        clone.setAttribute('width', Math.round(svgRect.width));
        clone.setAttribute('height', Math.round(svgRect.height));
        clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
        var originalElements = source.querySelectorAll('*');
        var clonedElements = clone.querySelectorAll('*');
        clonedElements.forEach(function (element, index) {
            var computed = getComputedStyle(originalElements[index]);
            [
                'fill', 'stroke', 'color', 'opacity',
                'stop-color', 'stop-opacity',
                'stroke-width', 'stroke-dasharray', 'stroke-linecap', 'stroke-linejoin'
            ].forEach(function (property) {
                var value = computed.getPropertyValue(property);
                if (value) element.style.setProperty(property, value);
            });
        });

        var svgText = new XMLSerializer().serializeToString(clone);
        var encoded = btoa(unescape(encodeURIComponent(svgText)));
        var url = 'data:image/svg+xml;base64,' + encoded;
        var image = new Image();

        image.onload = function () {
            context.drawImage(image, svgRect.left - chartRect.left, titleHeight + svgRect.top - chartRect.top,
                svgRect.width, svgRect.height);

            function drawText(element, align) {
                var elementRect = element.getBoundingClientRect();
                var styles = getComputedStyle(element);
                var x = (align === 'right' ? elementRect.right : elementRect.left) - chartRect.left;
                var y = titleHeight + elementRect.top - chartRect.top + elementRect.height / 2;
                context.font = styles.font;
                context.fillStyle = styles.color;
                context.textAlign = align;
                context.textBaseline = 'middle';
                context.fillText(element.textContent.trim(), x, y);
            }

            chart.querySelectorAll('.sipau-chart-yaxis span').forEach(function (element) {
                drawText(element, 'right');
            });
            chart.querySelectorAll('.sipau-chart-xaxis span').forEach(function (element) {
                drawText(element, 'center');
            });

            chart.querySelectorAll('.sipau-chart-legend .sipau-chart-key').forEach(function (element) {
                var elementRect = element.getBoundingClientRect();
                var styles = getComputedStyle(element);
                var x = elementRect.left - chartRect.left;
                var y = titleHeight + elementRect.top - chartRect.top + elementRect.height / 2;
                var swatch = element.querySelector('i');
                var swatchStyles = swatch ? getComputedStyle(swatch) : styles;
                context.fillStyle = swatchStyles.backgroundColor;
                context.fillRect(x, y - 1.5, 14, 3);
                context.font = styles.font;
                context.fillStyle = styles.color;
                context.textAlign = 'left';
                context.textBaseline = 'middle';
                context.fillText(element.textContent.trim(), x + 20, y);
            });

            if (footer) {
                var footerStyles = getComputedStyle(footer);
                var footerY = titleHeight + chartRect.height;
                var footerPadding = parseFloat(footerStyles.paddingLeft) || 0;
                var footerText = footer.textContent.trim().replace(/\s+/g, ' ');
                context.fillStyle = footerStyles.borderTopColor;
                context.fillRect(0, footerY, width, 1);
                context.font = footerStyles.font;
                context.fillStyle = footerStyles.color;
                context.textAlign = 'left';
                context.textBaseline = 'middle';
                var words = footerText.split(' ');
                var lines = [];
                var line = '';
                var maxWidth = width - footerPadding * 2;
                words.forEach(function (word) {
                    var candidate = line ? line + ' ' + word : word;
                    if (line && context.measureText(candidate).width > maxWidth) {
                        lines.push(line);
                        line = word;
                    } else {
                        line = candidate;
                    }
                });
                if (line) lines.push(line);

                var lineHeight = parseFloat(footerStyles.fontSize) * 1.4;
                var firstLineY = footerY + (footerHeight - lineHeight * (lines.length - 1)) / 2;
                lines.forEach(function (text, index) {
                    context.fillText(text, footerPadding, firstLineY + index * lineHeight);
                });
            }

            canvas.toBlob(function (png) {
                if (!png) return;

                function downloadPng() {
                    var downloadUrl = URL.createObjectURL(png);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = filename || 'grafica.png';
                    link.style.display = 'none';
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    setTimeout(function () { URL.revokeObjectURL(downloadUrl); }, 1000);
                }

                if (saveFile) {
                    saveFile.then(async function (handle) {
                        var writable = await handle.createWritable();
                        await writable.write(png);
                        await writable.close();
                    }).catch(function (error) {
                        // Closing the picker is an expected user action.
                        if (error.name === 'AbortError') return;
                        console.error('No se pudo guardar el PNG:', error);
                        downloadPng();
                    });
                    return;
                }

                downloadPng();
            }, 'image/png');
        };

        image.src = url;
    };

    document.addEventListener('click', function (e) {
        var button = e.target && e.target.closest ? e.target.closest('[data-sipau-chart-download]') : null;
        if (!button) return;
        e.preventDefault();
        window.sigieDownloadChartPng(
            button.getAttribute('data-sipau-chart-download'),
            button.getAttribute('data-sipau-chart-filename'),
            button.getAttribute('data-sipau-chart-title'));
    });

    // Opens the browser's print dialog; the document pages style themselves for it.
    window.sigiePrint = function () { window.print(); };

    // Streams generated bytes (an .xlsx workbook) without a server round-trip.
    window.sigieDownloadBytes = function (filename, base64, mime) {
        var binary = atob(base64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) { bytes[i] = binary.charCodeAt(i); }

        var blob = new Blob([bytes], { type: mime || 'application/octet-stream' });
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
        var btn = e.target && e.target.closest ? e.target.closest('[data-sipau-reveal]') : null;
        if (!btn) { return; }

        var input = document.getElementById(btn.getAttribute('data-sipau-reveal'));
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
