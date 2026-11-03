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

    window.sipauInitLoginNetwork = function () {
        var canvas = document.getElementById('sipau-login-network');
        var wall = canvas && canvas.closest('.sipau-login-wall');
        if (!canvas || !wall) return;

        if (typeof canvas._sipauNetworkDispose === 'function') {
            canvas._sipauNetworkDispose();
        }

        var context = canvas.getContext('2d');
        if (!context) return;

        var nodes = [];
        var frameId = 0;
        var width = 0;
        var height = 0;
        var dpr = 1;
        var settings = { density: 24000, minNodes: 26, maxNodes: 48, maxDistance: 210, speed: 0.4 };

        function makeNode() {
            var angle = Math.random() * Math.PI * 2;
            var speed = settings.speed * (0.45 + Math.random() * 0.8);
            return {
                x: Math.random() * width,
                y: Math.random() * height,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                radius: 2.2 + Math.random() * 2.2,
                phase: Math.random() * Math.PI * 2
            };
        }

        function resize() {
            var rect = wall.getBoundingClientRect();
            width = Math.max(1, rect.width);
            height = Math.max(1, rect.height);
            dpr = Math.min(window.devicePixelRatio || 1, 2);
            canvas.width = Math.round(width * dpr);
            canvas.height = Math.round(height * dpr);
            context.setTransform(dpr, 0, 0, dpr, 0, 0);

            var count = Math.max(settings.minNodes, Math.min(settings.maxNodes, Math.round(width * height / settings.density)));
            if (nodes.length > count) nodes.length = count;
            while (nodes.length < count) nodes.push(makeNode());
        }

        function draw(time) {
            context.clearRect(0, 0, width, height);
            for (var i = 0; i < nodes.length; i++) {
                for (var j = i + 1; j < nodes.length; j++) {
                    var first = nodes[i];
                    var second = nodes[j];
                    var distance = Math.hypot(first.x - second.x, first.y - second.y);
                    if (distance >= settings.maxDistance) continue;

                    context.beginPath();
                    context.moveTo(first.x, first.y);
                    context.lineTo(second.x, second.y);
                    context.strokeStyle = 'rgba(196, 226, 255, ' + ((1 - distance / settings.maxDistance) * 0.48) + ')';
                    context.lineWidth = 1.2;
                    context.stroke();
                }
            }

            nodes.forEach(function (node) {
                var pulse = 0.82 + Math.sin(time * 0.0015 + node.phase) * 0.18;
                context.beginPath();
                context.arc(node.x, node.y, node.radius * pulse, 0, Math.PI * 2);
                context.fillStyle = 'rgba(226, 241, 255, 0.92)';
                context.fill();
            });
        }

        function animate(time) {
            nodes.forEach(function (node) {
                node.x += node.vx;
                node.y += node.vy;
                if (node.x < -12) node.x = width + 12;
                if (node.x > width + 12) node.x = -12;
                if (node.y < -12) node.y = height + 12;
                if (node.y > height + 12) node.y = -12;
            });
            draw(time);
            frameId = window.requestAnimationFrame(animate);
        }

        function restart() {
            window.cancelAnimationFrame(frameId);
            resize();
            frameId = window.requestAnimationFrame(animate);
        }

        var resizeObserver = new ResizeObserver(restart);
        resizeObserver.observe(wall);
        canvas._sipauNetworkDispose = function () {
            window.cancelAnimationFrame(frameId);
            resizeObserver.disconnect();
        };

        restart();
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

            // The "font" shorthand comes back empty when an element sets something it cannot
            // express, such as tabular figures on the axes; the canvas then kept the title's
            // font and drew every axis label in bold 18 px.
            function fontOf(styles) {
                return styles.fontStyle + ' ' + styles.fontWeight + ' ' + styles.fontSize + ' ' + styles.fontFamily;
            }

            function drawText(element, align) {
                var elementRect = element.getBoundingClientRect();
                // Labels the chart is not showing at this width have no box to copy.
                if (elementRect.width === 0) return;
                var styles = getComputedStyle(element);
                var anchor = align === 'right' ? elementRect.right
                    : align === 'center' ? elementRect.left + elementRect.width / 2
                    : elementRect.left;
                var x = anchor - chartRect.left;
                var y = titleHeight + elementRect.top - chartRect.top + elementRect.height / 2;
                context.font = fontOf(styles);
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
                var swatchRect = swatch ? swatch.getBoundingClientRect() : { width: 14, height: 3 };
                var swatchWidth = swatchRect.width || 14;
                var swatchHeight = swatchRect.height || 3;
                if (swatch && swatch.classList.contains('dash')) {
                    // A dashed key has no background: its dashes are painted in the text colour.
                    context.fillStyle = swatchStyles.color;
                    for (var dash = 0; dash < swatchWidth; dash += 6) {
                        context.fillRect(x + dash, y - swatchHeight / 2, 3.3, swatchHeight);
                    }
                } else {
                    context.fillStyle = swatchStyles.backgroundColor;
                    context.fillRect(x, y - swatchHeight / 2, swatchWidth, swatchHeight);
                }
                context.font = fontOf(styles);
                context.fillStyle = styles.color;
                context.textAlign = 'left';
                context.textBaseline = 'middle';
                context.fillText(element.textContent.trim(), x + swatchWidth + 7, y);
            });

            if (footer) {
                var footerStyles = getComputedStyle(footer);
                var footerY = titleHeight + chartRect.height;
                var footerPadding = parseFloat(footerStyles.paddingLeft) || 0;
                var footerText = footer.textContent.trim().replace(/\s+/g, ' ');
                context.fillStyle = footerStyles.borderTopColor;
                context.fillRect(0, footerY, width, 1);
                context.font = fontOf(footerStyles);
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

    window.sipauScrollTo = function (id) {
        document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    };

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

    // The reconnection notice (App.razor). Blazor sets its state as a class and reports it here.
    var reconnect = document.getElementById('components-reconnect-modal');
    if (reconnect) {
        reconnect.addEventListener('components-reconnect-state-changed', function (e) {
            switch (e.detail.state) {
                case 'show':
                case 'paused':
                    if (!reconnect.open) { reconnect.showModal(); }
                    break;
                case 'hide':
                    if (reconnect.open) { reconnect.close(); }
                    break;
                case 'rejected':
                    // The server no longer has this screen: the only way back is a fresh page.
                    location.reload();
                    break;
            }
        });
        document.getElementById('sipau-reconnect-retry').addEventListener('click', function () {
            Blazor.reconnect().then(function (ok) { if (!ok) { location.reload(); } });
        });
        document.getElementById('sipau-reconnect-resume').addEventListener('click', function () {
            Blazor.resumeCircuit().then(function (ok) { if (!ok) { location.reload(); } });
        });
        document.getElementById('sipau-reconnect-reload').addEventListener('click', function () { location.reload(); });
    }

    // Signing out ends the session for the whole browser, and each open tab keeps its own live
    // connection, which would otherwise go on showing data. They are told, and reload into /login.
    if ('BroadcastChannel' in window) {
        var session = new BroadcastChannel('sipau-session');
        session.onmessage = function (e) {
            if (e.data === 'signed-out' && location.pathname !== '/login') { location.assign('/login'); }
        };
        document.addEventListener('submit', function (e) {
            var action = e.target && e.target.getAttribute ? e.target.getAttribute('action') : null;
            if (action && action.toLowerCase() === '/account/logout') { session.postMessage('signed-out'); }
        });
    }

    window.addEventListener('scroll', onScroll, { passive: true, capture: true });
    window.addEventListener('resize', onScroll, { passive: true });
    window.addEventListener('keydown', reveal, { passive: true });
    document.addEventListener('DOMContentLoaded', sync);
    sync();
})();
