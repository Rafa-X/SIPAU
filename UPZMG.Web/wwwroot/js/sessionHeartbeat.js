// Sends a keepalive ping only while the user has interacted with the page
// recently; idle tabs stop pinging and the cookie expires naturally.
const ACTIVITY_EVENTS = ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'];

export function start(dotNetRef, containerId, intervalMs) {
    let lastActivity = Date.now();
    const markActive = () => { lastActivity = Date.now(); };
    ACTIVITY_EVENTS.forEach(evt => document.addEventListener(evt, markActive, { passive: true }));

    const timer = setInterval(async () => {
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
                // A redirect is not a renewal. Without this, a 302 to the login page is
                // followed automatically and arrives as a 200 that looks like success.
                redirect: 'manual',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `__RequestVerificationToken=${encodeURIComponent(tokenInput.value)}`
            });

            // The endpoint answers 204 and nothing else; anything else means the session was
            // not extended, so the server-side clock must not be pushed forward.
            if (response.status === 204) {
                await dotNetRef.invokeMethodAsync('OnSessionRefreshed');
            }
        } catch {
            // Network hiccup: skip this cycle, try again on the next interval.
        }
    }, intervalMs);

    // Returned so the component can undo all of this. Disposing the module reference alone
    // left the interval running and the listeners attached to the document.
    return {
        stop: () => {
            clearInterval(timer);
            ACTIVITY_EVENTS.forEach(evt => document.removeEventListener(evt, markActive));
        }
    };
}
