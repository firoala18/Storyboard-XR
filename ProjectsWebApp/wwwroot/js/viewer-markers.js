// Viewer v2 — marker chips, dots, selection, search.
(function viewerScenes() {
    'use strict';
    const grid = document.querySelector('[data-role="viewer-grid"]');
    const dataNode = document.getElementById('viewer-scenes-data');
    if (!grid || !dataNode) return;

    let scenes;
    try { scenes = JSON.parse(dataNode.textContent || '[]'); }
    catch { scenes = []; }

    const byId = new Map(scenes.map(s => [s.id, s]));
    let activeSceneId = Number(grid.dataset.activeSceneId) || (scenes[0]?.id ?? 0);

    const layoutState = window.__viewerState?.get() || {};
    const saveLayout = window.__viewerState?.save || (() => {});
    layoutState.activeMarkerIdBySceneId = layoutState.activeMarkerIdBySceneId || {};

    // Expose for other IIFEs in this file.
    window.__viewerScenes = {
        all: () => scenes,
        get: id => byId.get(id),
        getActive: () => byId.get(activeSceneId),
        setActive: setActiveScene,
    };

    function setActiveScene(sceneId, options) {
        const scene = byId.get(sceneId);
        if (!scene) return;
        activeSceneId = sceneId;
        grid.dataset.activeSceneId = String(sceneId);

        // Swap the scene image.
        const img = document.querySelector('[data-role="scene-img"]');
        const placeholder = grid.dataset.placeholder;
        if (img) img.src = scene.imagePath || placeholder;

        // Swap the header.
        const num = document.querySelector('.active-scene-num');
        const name = document.querySelector('[data-role="active-scene-name"]');
        if (num) num.textContent = scene.number;
        if (name) name.textContent = scene.name || '';

        // Reflect selection in both rail states.
        document.querySelectorAll('.scene-thumb, .scene-thumb-mini').forEach(el => {
            const match = Number(el.dataset.sceneId) === sceneId;
            el.classList.toggle('is-active', match);
            el.setAttribute('aria-selected', match ? 'true' : 'false');
        });

        // Broadcast so the marker controllers can rebuild chips + dots.
        window.dispatchEvent(new CustomEvent('viewer:scene-changed', { detail: { scene } }));

        // URL bookmark (no reload).
        const url = new URL(window.location.href);
        url.searchParams.set('sceneId', String(sceneId));
        history.replaceState(null, '', url.toString());

        if (!options || options.fireMarker !== false) {
            const savedMarkerId = layoutState.activeMarkerIdBySceneId[sceneId];
            const firstMarker = scene.markers?.[0]?.id;
            const target = savedMarkerId && scene.markers?.some(m => m.id === savedMarkerId) ? savedMarkerId : firstMarker;
            if (target) window.dispatchEvent(new CustomEvent('viewer:marker-select', { detail: { markerId: target, persist: false } }));
        }
    }

    function wireRailClicks() {
        document.querySelectorAll('.scene-thumb, .scene-thumb-mini').forEach(el => {
            el.addEventListener('click', () => setActiveScene(Number(el.dataset.sceneId)));
        });
    }
    wireRailClicks();

    // Kick off initial marker selection for the already-active scene on load.
    document.addEventListener('DOMContentLoaded', () => {
        if (!activeSceneId) return;
        const scene = byId.get(activeSceneId);
        if (!scene) return;
        window.dispatchEvent(new CustomEvent('viewer:scene-initial', { detail: { scene } }));
    });
})();

(function viewerMarkerController() {
    'use strict';
    const grid = document.querySelector('[data-role="viewer-grid"]');
    if (!grid || !window.__viewerScenes) return;

    const chipList = document.querySelector('[data-role="marker-chipbar-list"]');
    const overlay = document.querySelector('[data-role="overlay"]');
    const reader = document.querySelector('[data-role="marker-reader"]');
    const empty = document.querySelector('[data-role="reader-empty"]');
    const layout = window.__viewerState?.get() || {};
    const saveLayout = window.__viewerState?.save || (() => {});

    let activeMarkerId = null;

    function contrastText(hex) {
        const s = (hex || '').replace('#', '');
        if (s.length < 6) return '#fff';
        const r = parseInt(s.slice(0,2),16), g = parseInt(s.slice(2,4),16), b = parseInt(s.slice(4,6),16);
        const lum = (0.299*r + 0.587*g + 0.114*b) / 255;
        return lum > 0.6 ? '#0f172a' : '#fff';
    }

    function renderFor(scene) {
        activeMarkerId = null;
        if (chipList) chipList.innerHTML = '';
        if (overlay) overlay.innerHTML = '';
        if (empty) empty.hidden = false;
        if (reader) reader.hidden = true;

        (scene.markers || []).forEach(m => {
            if (chipList) {
                const li = document.createElement('li');
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'marker-chip';
                btn.dataset.markerId = String(m.id);
                btn.style.background = m.colorHex || '#89ba17';
                btn.style.color = contrastText(m.colorHex);
                btn.textContent = m.number;
                btn.title = `Marker ${m.number}`;
                btn.addEventListener('click', () => selectMarker(m.id));
                li.appendChild(btn);
                chipList.appendChild(li);
            }

            if (overlay) {
                const dot = document.createElement('button');
                dot.type = 'button';
                dot.className = 'marker-dot';
                dot.dataset.markerId = String(m.id);
                dot.style.left = (m.x * 100).toFixed(3) + '%';
                dot.style.top  = (m.y * 100).toFixed(3) + '%';
                dot.style.background = m.colorHex || '#89ba17';
                dot.style.color = contrastText(m.colorHex);
                dot.textContent = m.number;
                dot.addEventListener('click', () => selectMarker(m.id));
                overlay.appendChild(dot);
            }
        });
    }

    function selectMarker(markerId, options) {
        const scene = window.__viewerScenes.getActive();
        if (!scene) return;
        const m = (scene.markers || []).find(x => x.id === markerId);
        if (!m) return;
        activeMarkerId = markerId;

        document.querySelectorAll('.marker-chip').forEach(c =>
            c.classList.toggle('is-active', Number(c.dataset.markerId) === markerId));
        document.querySelectorAll('.marker-dot').forEach(d =>
            d.classList.toggle('is-selected', Number(d.dataset.markerId) === markerId));

        renderReader(m);

        if (!options || options.persist !== false) {
            layout.activeMarkerIdBySceneId = layout.activeMarkerIdBySceneId || {};
            layout.activeMarkerIdBySceneId[scene.id] = markerId;
            saveLayout();
        }
    }

    function renderReader(m) {
        if (!reader) return;
        if (empty) empty.hidden = true;
        reader.hidden = false;

        const badge = reader.querySelector('[data-role="reader-badge"]');
        if (badge) {
            badge.textContent = m.number;
            badge.style.background = m.colorHex || '#89ba17';
            badge.style.color = contrastText(m.colorHex);
        }
        const titleEl = reader.querySelector('[data-role="reader-title"]');
        if (titleEl) titleEl.textContent = `Marker ${m.number}`;

        const taxoEl = reader.querySelector('[data-role="reader-taxo"]');
        if (taxoEl) {
            if (m.taxonomie != null) {
                taxoEl.hidden = false;
                taxoEl.textContent = 'Taxonomie ' + (m.taxonomie + 1);
            } else {
                taxoEl.hidden = true;
            }
        }

        function fill(role, html) {
            const el = reader.querySelector(`[data-role="reader-${role}"]`);
            if (!el) return;
            const section = el.closest('.marker-reader-field');
            if (!html || !String(html).trim()) { if (section) section.hidden = true; return; }
            if (section) section.hidden = false;
            if (el.classList.contains('richtext')) el.innerHTML = html;
            else el.textContent = html;
        }
        fill('description', m.description);
        fill('ziel', m.ziel);
        fill('promptIdee', m.promptIdee);
        fill('reflexion', m.reflexion);
        fill('datenablage', m.datenablage);
        fill('quellen', m.quellen);
        fill('model', m.model);

        reader.querySelectorAll('.marker-reader-group').forEach(group => {
            const hasVisible = group.querySelector('.marker-reader-field:not([hidden])');
            group.hidden = !hasVisible;
        });
    }

    function step(dir) {
        const scene = window.__viewerScenes.getActive();
        if (!scene || !scene.markers?.length) return;
        const idx = scene.markers.findIndex(m => m.id === activeMarkerId);
        const next = scene.markers[(idx + dir + scene.markers.length) % scene.markers.length];
        selectMarker(next.id);
    }

    document.querySelector('[data-action="marker-prev"]')?.addEventListener('click', () => step(-1));
    document.querySelector('[data-action="marker-next"]')?.addEventListener('click', () => step(+1));

    document.addEventListener('keydown', ev => {
        if (ev.target.matches('input, textarea, [contenteditable]')) return;
        if (ev.key === 'ArrowLeft')  { step(-1); ev.preventDefault(); }
        if (ev.key === 'ArrowRight') { step(+1); ev.preventDefault(); }
        if (ev.key === 'ArrowUp')    { stepScene(-1); ev.preventDefault(); }
        if (ev.key === 'ArrowDown')  { stepScene(+1); ev.preventDefault(); }
    });

    function stepScene(dir) {
        const all = window.__viewerScenes.all();
        if (!all.length) return;
        const cur = window.__viewerScenes.getActive();
        const idx = all.findIndex(s => s.id === cur?.id);
        const next = all[(idx + dir + all.length) % all.length];
        window.__viewerScenes.setActive(next.id);
    }

    window.addEventListener('viewer:scene-changed', ev => renderFor(ev.detail.scene));
    window.addEventListener('viewer:scene-initial', ev => {
        renderFor(ev.detail.scene);
        const saved = layout.activeMarkerIdBySceneId?.[ev.detail.scene.id];
        const first = ev.detail.scene.markers?.[0]?.id;
        const target = saved && ev.detail.scene.markers?.some(m => m.id === saved) ? saved : first;
        if (target) selectMarker(target, { persist: false });
    });
    window.addEventListener('viewer:marker-select', ev => {
        const persist = ev.detail?.persist !== false;
        selectMarker(ev.detail.markerId, { persist });
    });

    window.__viewerMarkers = { select: selectMarker };
})();
