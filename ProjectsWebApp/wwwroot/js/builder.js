(function () {
    'use strict';

    const main = document.querySelector('.builder-main');
    if (!main) return;

    const storyboardId = Number(main.dataset.storyboardId);
    const initialStep  = Number(main.dataset.initialStep) || 1;
    let   currentRowVersion = main.dataset.storyboardRowversion || null;

    const stepButtons = document.querySelectorAll('.builder-step[data-step]');
    const stepPanels  = document.querySelectorAll('.builder-step-panel[data-step]');

    function gotoStep(step) {
        step = Math.max(1, Math.min(3, Number(step) || 1));
        stepButtons.forEach(b => b.setAttribute('aria-current', Number(b.dataset.step) === step ? 'step' : 'false'));
        stepPanels.forEach(p => p.hidden = Number(p.dataset.step) !== step);
        const url = new URL(window.location);
        url.searchParams.set('step', step);
        history.replaceState(null, '', url);
        window.dispatchEvent(new CustomEvent('builder:step-changed', { detail: { step } }));
    }

    stepButtons.forEach(b => b.addEventListener('click', () => gotoStep(b.dataset.step)));

    const initialFromUrl = Number(new URLSearchParams(window.location.search).get('step'));
    gotoStep(initialFromUrl || initialStep);

    const chip = document.querySelector('.builder-save-chip');
    const chipText = chip?.querySelector('.builder-save-text');
    let lastSavedAt = null;

    function setChip(state, text) {
        if (!chip) return;
        chip.dataset.state = state;
        if (text && chipText) chipText.textContent = text;
    }
    function tickChip() {
        if (!lastSavedAt || !chip || chip.dataset.state !== 'saved') return;
        const s = Math.max(1, Math.round((Date.now() - lastSavedAt) / 1000));
        setChip('saved', `Gespeichert vor ${s < 60 ? s + ' Sek.' : Math.round(s/60) + ' Min.'}`);
    }
    setInterval(tickChip, 5000);

    function csrf() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    const QUEUE_KEY = `builder:queue:${storyboardId}`;
    const debounceTimers = new Map();

    function loadQueue() {
        try { return JSON.parse(localStorage.getItem(QUEUE_KEY)) || []; }
        catch { return []; }
    }
    function saveQueue(q) { localStorage.setItem(QUEUE_KEY, JSON.stringify(q)); }

    async function flushOnePatch(entry) {
        const headers = { 'Content-Type': 'application/json' };
        const t = csrf(); if (t) headers['RequestVerificationToken'] = t;

        if (entry.entity === 'storyboard' && currentRowVersion) entry.payload.rowVersion = currentRowVersion;

        const res = await fetch(entry.url, { method: 'PATCH', headers, body: JSON.stringify(entry.payload) });
        if (!res.ok) {
            if (res.status === 409) {
                const fresh = await res.json().catch(() => null);
                if (fresh?.rowVersion && entry.entity === 'storyboard') currentRowVersion = fresh.rowVersion;
                throw new Error('stale');
            }
            // Log the server's message so 400/500 responses aren't silent in the console.
            const body = await res.text().catch(() => '');
            console.warn('PATCH failed', entry.url, res.status, body, 'payload keys:', Object.keys(entry.payload));
            throw new Error(`http-${res.status}`);
        }
        const data = await res.json().catch(() => ({}));
        if (data?.rowVersion && entry.entity === 'storyboard') currentRowVersion = data.rowVersion;
        return data;
    }

    async function flushQueue() {
        const q = loadQueue();
        if (q.length === 0) { setChip('idle', 'Bereit'); return; }
        setChip('saving', 'Speichere…');
        let skippedBadEntry = false;
        while (q.length > 0) {
            try { await flushOnePatch(q[0]); q.shift(); saveQueue(q); }
            catch (err) {
                if (err.message === 'stale') {
                    setChip('error', '⚠ Konflikt – bitte Seite neu laden');
                    return;
                }
                // Non-retryable 4xx: drop the poison pill and keep going so one bad entry
                // can't block every future save (especially across page reloads).
                if (/^http-4\d\d$/.test(err.message || '')) {
                    console.warn('Dropping non-retryable queue entry', q[0]);
                    q.shift(); saveQueue(q);
                    skippedBadEntry = true;
                    continue;
                }
                setChip('error', '⚠ Verbindungsfehler – erneut versuchen');
                return;
            }
        }
        lastSavedAt = Date.now();
        setChip(skippedBadEntry ? 'error' : 'saved',
                skippedBadEntry ? '⚠ Einige Änderungen wurden abgelehnt' : 'Gespeichert vor 1 Sek.');
    }

    window.addEventListener('online', flushQueue);
    if (navigator.onLine) flushQueue();

    function patchField(entity, id, field, value, opts = {}) {
        const base = (window.apiUrl ? '' : (window.APP_PATH_BASE || ''));
        const raw =
            entity === 'storyboard' ? `/Storyboards/${id}` :
            entity === 'scene'      ? `/Scenes/${id}` :
            entity === 'marker'     ? `/Markers/${id}` : null;
        if (!raw) throw new Error('unknown entity ' + entity);
        const url = window.apiUrl ? window.apiUrl(raw) : (base + raw);

        const key = `${entity}:${id}:${field}`;
        const debounceMs = opts.immediate ? 0 : (opts.debounce ?? 400);

        if (debounceTimers.has(key)) clearTimeout(debounceTimers.get(key));

        const fire = () => {
            const q = loadQueue();
            const existing = q.find(e => e.key === key);
            const payload = { [field]: value };
            if (existing) existing.payload[field] = value;
            else q.push({ key, entity, url, payload });
            saveQueue(q);
            flushQueue();
        };

        if (debounceMs === 0) fire();
        else debounceTimers.set(key, setTimeout(fire, debounceMs));
    }

    document.querySelectorAll('[data-builder-edit]').forEach(wire);

    function wire(el) {
        const entity = el.dataset.builderEntity || 'storyboard';
        const field  = el.dataset.builderField;
        if (!field) return;

        el.setAttribute('contenteditable', el.dataset.builderMultiline === 'true' ? 'true' : 'plaintext-only');
        el.addEventListener('blur', () => {
            const value = el.innerText.trim();
            const id = Number(el.dataset.builderId || storyboardId);
            if (!Number.isFinite(id) || id <= 0) return;
            patchField(entity, id, field, value);
        });
        el.addEventListener('keydown', ev => {
            if (ev.key === 'Enter' && el.dataset.builderMultiline !== 'true') { ev.preventDefault(); el.blur(); }
            if (ev.key === 'Escape') { el.blur(); }
        });
    }

    window.Builder = { patchField, gotoStep, setChip };
})();
