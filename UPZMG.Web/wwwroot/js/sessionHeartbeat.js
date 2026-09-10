// Sends a keepalive ping only while the user has interacted with the page
// recently; idle tabs stop pinging and the cookie expires naturally.
export function start(dotNetRef, containerId, intervalMs) {
    let lastActivity = Date.now();
    const markActive = () => { lastActivity = Date.now(); };
    ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(evt =>
        document.addEventListener(evt, markActive, { passive: true }));

    setInterval(async () => {
        if (Date.now() - lastActivity > intervalMs) {
            return;
        }

        const tokenInput = document.querySelector(`#${containerId} input[name="__RequestVerificationToken"]`);
        if (!tokenInput) {
            return;
        }

        try {
            const response = await fetch('/account/keepalive', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `__RequestVerificationToken=${encodeURIComponent(tokenInput.value)}`
            });
            if (response.ok) {
                await dotNetRef.invokeMethodAsync('OnSessionRefreshed');
            }
        } catch {
            // Network hiccup: skip this cycle, try again on the next interval.
        }
    }, intervalMs);
}
