// Live presence + cursor sharing for the Builder and the Viewer.
//
// Builder: full bidirectional — broadcasts the local cursor and renders peers'.
// Viewer:  receive-only — renders peers' cursors but never broadcasts.
//
// Cursors are scoped to the scene image canvas only. The pointer position is
// sent as normalized (x, y) in [0..1] relative to the active scene image, so
// peers see it on the exact same spot regardless of zoom or window size.
// Outside the image the cursor is reported as 'away' (chip stays in the
// presence bar; the live arrow disappears).

(function () {
    'use strict';

    const PRESENCE_BAR_ID = 'presence-bar';
    const CURSOR_LAYER_ID = 'cursor-layer';
    const SCENE_IMG_SELECTORS = ['.step2-canvas-inner .scene-img', '[data-role="scene-img"]'];
    const THROTTLE_MS = 50;       // ~20 Hz cap
    const MIN_DELTA = 0.002;      // skip near-duplicate positions (normalized units)
    const IDLE_FADE_MS = 3500;    // start fading after this much silence
    const IDLE_HIDE_MS = 8000;    // fully drop the live cursor after this
    const RESOLVE_RETRY_MS = 250;

    const main = document.querySelector('.builder-main');
    const viewerGrid = document.querySelector('[data-role="viewer-grid"]');
    if (!main && !viewerGrid) return;
    if (typeof window.signalR === 'undefined') return;

    const storyboardId = Number(main?.dataset.storyboardId ?? viewerGrid?.dataset.sbId);
    if (!Number.isFinite(storyboardId)) return;

    const isBuilder = !!main;

    let getConnection;
    if (isBuilder) {
        if (window.BuilderRT?.ready) {
            getConnection = () => window.BuilderRT.connection;
            boot();
        } else {
            window.addEventListener('builder-rt:ready', () => {
                getConnection = () => window.BuilderRT.connection;
                boot();
            }, { once: true });
        }
        return;
    }

    const hubUrl = (window.APP_PATH_BASE || '') + '/hubs/storyboard';
    const conn = new window.signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();
    getConnection = () => conn;
    boot();

    function boot() {
        const connection = getConnection();
        const peers = new Map();
        let myConnectionId = null;
        let myRole = 'viewer';
        let sendingEnabled = false;
        const presenceBar = ensurePresenceBar();
        const cursorLayer = ensureCursorLayer();

        connection.on('PresenceJoined', (entry) => {
            if (!entry || entry.connectionId === myConnectionId) return;
            peers.set(entry.connectionId, { entry, lastPayload: null, lastSeen: Date.now(), el: null });
            renderPresenceBar();
        });

        connection.on('PresenceLeft', (msg) => {
            const id = msg?.connectionId;
            if (!id) return;
            const peer = peers.get(id);
            if (peer?.el) peer.el.remove();
            peers.delete(id);
            renderPresenceBar();
        });

        connection.on('CursorMove', (msg) => {
            if (!msg || !msg.connectionId) return;
            const peer = peers.get(msg.connectionId);
            if (!peer) return;
            peer.lastPayload = msg;
            peer.lastSeen = Date.now();
            if (msg.mode === 'away') {
                if (peer.el) peer.el.style.opacity = '0';
                return;
            }
            renderRemoteCursor(peer);
        });

        (async function start() {
            if (!isBuilder) {
                try { await connection.start(); } catch (e) { console.warn('presence: start failed', e); return; }
            }
            try {
                const me = await connection.invoke('JoinPresence', storyboardId);
                if (!me) return;
                myConnectionId = me.connectionId;
                myRole = me.role || 'viewer';
                sendingEnabled = isBuilder && myRole === 'editor';
                const others = await connection.invoke('PresenceHere', storyboardId);
                (others || []).forEach(e => {
                    peers.set(e.connectionId, { entry: e, lastPayload: null, lastSeen: Date.now(), el: null });
                });
                renderPresenceBar();
                if (sendingEnabled) attachLocalListeners();
            } catch (err) {
                console.warn('presence: JoinPresence failed', err);
            }
        })();

        connection.onreconnected(async () => {
            try {
                const me = await connection.invoke('JoinPresence', storyboardId);
                if (!me) return;
                myConnectionId = me.connectionId;
                const others = await connection.invoke('PresenceHere', storyboardId);
                peers.clear();
                (others || []).forEach(e => {
                    peers.set(e.connectionId, { entry: e, lastPayload: null, lastSeen: Date.now(), el: null });
                });
                renderPresenceBar();
            } catch {}
        });

        // ── Local broadcast — image canvas only
        let lastSent = 0;
        let lastNx = -1, lastNy = -1;
        let lastMode = '';
        let pending = null;

        function attachLocalListeners() {
            window.addEventListener('pointermove', onPointerMove, { passive: true });
            window.addEventListener('pointerleave', () => sendCursor({ mode: 'away' }, true));
            document.addEventListener('visibilitychange', () => {
                if (document.hidden) sendCursor({ mode: 'away' }, true);
            });
            window.addEventListener('beforeunload', () => {
                try { sendCursor({ mode: 'away' }, true); } catch {}
            });
        }

        function onPointerMove(ev) {
            const sceneEl = findSceneImageAt(ev.clientX, ev.clientY);
            if (!sceneEl) {
                if (lastMode !== 'away') {
                    sendCursor({ mode: 'away' }, true);
                    lastNx = lastNy = -1;
                }
                return;
            }
            const rect = sceneEl.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) return;
            const x = clamp01((ev.clientX - rect.left) / rect.width);
            const y = clamp01((ev.clientY - rect.top) / rect.height);
            if (Math.abs(x - lastNx) < MIN_DELTA && Math.abs(y - lastNy) < MIN_DELTA) return;
            lastNx = x; lastNy = y;
            pending = { mode: 'scene', x, y, sceneId: getActiveSceneId() };
            scheduleSend();
        }

        function scheduleSend() {
            const now = performance.now();
            const since = now - lastSent;
            if (since >= THROTTLE_MS) {
                flush();
            } else if (!scheduleSend._t) {
                scheduleSend._t = setTimeout(() => { scheduleSend._t = null; flush(); }, THROTTLE_MS - since);
            }
        }

        function flush() {
            if (!pending) return;
            sendCursor(pending);
            pending = null;
            lastSent = performance.now();
        }

        function sendCursor(payload, force = false) {
            if (!sendingEnabled) return;
            if (!force && payload.mode === lastMode && payload.mode === 'away') return;
            lastMode = payload.mode;
            connection.invoke('CursorMove', storyboardId, payload).catch(() => {});
        }

        function findSceneImageAt(x, y) {
            for (const sel of SCENE_IMG_SELECTORS) {
                const el = document.querySelector(sel);
                if (!el || !isVisible(el)) continue;
                const r = el.getBoundingClientRect();
                if (x >= r.left && x <= r.right && y >= r.top && y <= r.bottom) return el;
            }
            return null;
        }

        function getActiveSceneId() {
            if (isBuilder) return window.Step2?.getActiveSceneId?.() ?? null;
            return Number(viewerGrid?.dataset.activeSceneId) || null;
        }

        // ── Remote cursor rendering
        function renderRemoteCursor(peer) {
            const p = peer.lastPayload; if (!p || p.mode !== 'scene') return;
            const sceneEl = findActiveSceneImageForId(p.sceneId);
            if (!sceneEl) {
                if (!peer._retry) {
                    peer._retry = setTimeout(() => { peer._retry = null; renderRemoteCursor(peer); }, RESOLVE_RETRY_MS);
                }
                if (peer.el) peer.el.style.opacity = '0';
                return;
            }
            const r = sceneEl.getBoundingClientRect();
            const x = r.left + p.x * r.width;
            const y = r.top + p.y * r.height;

            if (!peer.el) {
                peer.el = buildCursorElement(peer.entry);
                cursorLayer.appendChild(peer.el);
            }
            peer.el.style.opacity = '1';
            peer.el.style.transform = `translate(${x}px, ${y}px)`;
        }

        function findActiveSceneImageForId(sceneId) {
            const active = getActiveSceneId();
            if (sceneId != null && active !== sceneId) return null;
            for (const sel of SCENE_IMG_SELECTORS) {
                const el = document.querySelector(sel);
                if (el && isVisible(el)) return el;
            }
            return null;
        }

        function buildCursorElement(entry) {
            const wrap = document.createElement('div');
            wrap.className = 'presence-cursor';
            wrap.style.setProperty('--cursor-color', entry.color);
            // SVG arrow: filled with the user's color, white outline for contrast.
            // The path is drawn so the hot-spot is at (0,0) inside the wrapper —
            // that's where transform translate() positions us.
            wrap.innerHTML =
                '<svg class="presence-cursor-arrow" viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">' +
                '<path d="M5.5 3.5 L5.5 19 L9 15.5 L11.5 21 L14 20 L11.5 14.5 L17 14.5 Z" ' +
                'fill="' + entry.color + '" stroke="#fff" stroke-width="1.5" stroke-linejoin="round" />' +
                '</svg>' +
                '<span class="presence-cursor-label" style="background:' + entry.color + '">' +
                escapeHtml(entry.displayName || entry.initials || '?') + '</span>';
            return wrap;
        }

        // ── Idle sweep
        setInterval(() => {
            const now = Date.now();
            for (const peer of peers.values()) {
                if (!peer.el) continue;
                const age = now - peer.lastSeen;
                if (age >= IDLE_HIDE_MS) {
                    peer.el.remove(); peer.el = null;
                } else if (age >= IDLE_FADE_MS) {
                    peer.el.style.opacity = '0.35';
                }
            }
        }, 1000);

        window.addEventListener('scroll', repositionAll, { passive: true });
        window.addEventListener('resize', repositionAll, { passive: true });
        window.addEventListener('builder:scene-changed', repositionAll);
        window.addEventListener('viewer:scene-changed', repositionAll);

        function repositionAll() {
            for (const peer of peers.values()) {
                if (peer.lastPayload && peer.lastPayload.mode === 'scene') renderRemoteCursor(peer);
            }
        }

        // ── Presence bar — prefer an in-header placeholder, fall back to floating
        function ensurePresenceBar() {
            let bar = document.getElementById(PRESENCE_BAR_ID);
            if (!bar) {
                bar = document.createElement('div');
                bar.id = PRESENCE_BAR_ID;
                bar.className = 'presence-bar presence-bar--floating';
                document.body.appendChild(bar);
            }
            return bar;
        }

        function ensureCursorLayer() {
            let layer = document.getElementById(CURSOR_LAYER_ID);
            if (!layer) {
                layer = document.createElement('div');
                layer.id = CURSOR_LAYER_ID;
                layer.className = 'presence-cursor-layer';
                document.body.appendChild(layer);
            }
            return layer;
        }

        function renderPresenceBar() {
            presenceBar.innerHTML = '';
            const list = Array.from(peers.values());
            for (const peer of list) {
                const e = peer.entry;
                const chip = document.createElement('button');
                chip.type = 'button';
                chip.className = 'presence-chip';
                chip.style.background = e.color;
                chip.title = (e.displayName || '?') + (e.role === 'viewer' ? ' · Ansicht' : '');
                chip.textContent = e.initials || '?';
                chip.addEventListener('click', () => jumpToPeer(peer));
                presenceBar.appendChild(chip);
            }
            presenceBar.classList.toggle('is-empty', list.length === 0);
        }

        function jumpToPeer(peer) {
            const p = peer.lastPayload;
            if (!p || p.mode !== 'scene') {
                flashChip(peer.entry, (peer.entry.displayName || 'Gast') + ' ist gerade nicht aktiv');
                return;
            }
            const active = getActiveSceneId();
            if (active !== p.sceneId && isBuilder) {
                document.querySelector(`.scene-thumb[data-scene-id="${p.sceneId}"]`)?.click();
            }
            setTimeout(() => {
                const el = findActiveSceneImageForId(p.sceneId);
                el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }, 120);
        }

        function flashChip(entry, msg) {
            const toast = document.createElement('div');
            toast.className = 'presence-toast';
            toast.style.borderLeftColor = entry.color;
            toast.textContent = msg;
            document.body.appendChild(toast);
            setTimeout(() => toast.remove(), 2200);
        }
    }

    function clamp01(v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
    function escapeHtml(s) { return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }
    function isVisible(el) {
        if (!el) return false;
        if (el.offsetParent !== null) return true;
        const cs = window.getComputedStyle(el);
        return cs.display !== 'none' && cs.visibility !== 'hidden';
    }
})();
