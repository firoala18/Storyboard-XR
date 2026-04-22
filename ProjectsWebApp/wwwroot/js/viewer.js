// Viewer v2 — layout state, persistence, dock menu, toolbar actions.
(function viewerLayout() {
    'use strict';
    const grid = document.querySelector('[data-role="viewer-grid"]');
    if (!grid) return;

    const STORE = 'viewer:aside';

    function load() {
        try { return JSON.parse(localStorage.getItem(STORE)) || {}; } catch { return {}; }
    }
    function save(state) { localStorage.setItem(STORE, JSON.stringify(state)); }

    const panelEl = document.querySelector('.viewer-panel');

    function clampFloatState(state) {
        // Discard obviously bad persisted coordinates so a stale value can't
        // strand the panel off-screen.
        const vw = window.innerWidth, vh = window.innerHeight;
        const isNum = n => Number.isFinite(n);
        if (!isNum(state.floatW) || state.floatW < 240 || state.floatW > vw) delete state.floatW;
        if (!isNum(state.floatH) || state.floatH < 200 || state.floatH > vh) delete state.floatH;
        const w = state.floatW || 360;
        const h = state.floatH || Math.round(vh * 0.72);
        if (!isNum(state.floatLeft) || state.floatLeft < 0 || state.floatLeft > vw - 80) delete state.floatLeft;
        if (!isNum(state.floatTop)  || state.floatTop  < 0 || state.floatTop  > vh - 80) delete state.floatTop;
        // If width/height would overflow, clamp to viewport-ish.
        if (state.floatLeft != null && state.floatLeft + w > vw) state.floatLeft = Math.max(8, vw - w - 8);
        if (state.floatTop  != null && state.floatTop  + h > vh) state.floatTop  = Math.max(8, vh - h - 8);
    }

    const VALID_MODES = ['docked', 'floating', 'below'];
    function normalizeMode(s) {
        if (VALID_MODES.includes(s.panelMode)) return s.panelMode;
        // Legacy: panelFloating boolean -> 'floating' | 'docked'
        return s.panelFloating ? 'floating' : 'docked';
    }

    function apply(state) {
        clampFloatState(state);
        if (state.railW)  grid.style.setProperty('--rail-w',  state.railW  + 'px');
        if (state.panelW) grid.style.setProperty('--panel-w', state.panelW + 'px');
        grid.dataset.railCollapsed  = state.railCollapsed  ? 'true' : 'false';
        grid.dataset.panelCollapsed = state.panelCollapsed ? 'true' : 'false';
        const mode = normalizeMode(state);
        grid.dataset.panelMode = mode;
        // Keep legacy flag in sync for any external readers.
        grid.dataset.panelFloating = mode === 'floating' ? 'true' : 'false';
        if (mode === 'floating') {
            if (state.floatTop  != null) grid.style.setProperty('--panel-float-top',  state.floatTop  + 'px');
            if (state.floatLeft != null) {
                grid.style.setProperty('--panel-float-left', state.floatLeft + 'px');
                grid.style.setProperty('--panel-float-right', 'auto');
            } else {
                // No persisted left — fall back to docking to the right edge.
                grid.style.removeProperty('--panel-float-left');
                grid.style.setProperty('--panel-float-right', '24px');
            }
            if (state.floatW != null) grid.style.setProperty('--panel-float-w', state.floatW + 'px');
            if (state.floatH != null) grid.style.setProperty('--panel-float-h', state.floatH + 'px');
        } else if (panelEl) {
            // Leaving floating: CSS `resize: both` and our drag write inline width/height/top/left
            // directly on the element; strip them so the grid can size it again.
            panelEl.style.width = '';
            panelEl.style.height = '';
            panelEl.style.top = '';
            panelEl.style.left = '';
            panelEl.style.right = '';
        }
        if (mode === 'below' && state.belowH != null) {
            grid.style.setProperty('--panel-below-h', state.belowH + 'px');
        }
        // Sync dropdown checked state
        document.querySelectorAll('[data-action="set-panel-mode"]').forEach(b => {
            b.setAttribute('aria-checked', b.dataset.mode === mode ? 'true' : 'false');
        });
    }

    const state = load();

    // Expose the state via window.__viewerState BEFORE dispatching dock-menu / rail-toggle wiring,
    // so later IIFEs (info drawer, markers controller) can share the same state object.
    window.__viewerState = { get: () => state, save: () => save(state) };

    // Emergency reset: ?reset-viewer-ui=1 wipes the persisted layout state.
    if (new URLSearchParams(location.search).get('reset-viewer-ui') === '1') {
        for (const k of Object.keys(state)) delete state[k];
        save(state);
    }

    apply(state);

    function toggle(which) {
        const key = which === 'rail' ? 'railCollapsed' : 'panelCollapsed';
        state[key] = !state[key];
        apply(state); save(state);
    }

    document.querySelectorAll('[data-action="toggle-rail"]').forEach(b =>
        b.addEventListener('click', () => toggle('rail')));
    document.querySelectorAll('[data-action="toggle-panel"]').forEach(b =>
        b.addEventListener('click', () => toggle('panel')));

    // Dock-mode dropdown: open/close + radio-style selection
    const dockMenu = document.querySelector('[data-role="dock-menu"]');
    const dockTrigger = dockMenu?.querySelector('[data-action="toggle-dock-menu"]');
    const dockDropdown = dockMenu?.querySelector('.viewer-dock-dropdown');
    function closeDockMenu() {
        if (!dockDropdown) return;
        dockDropdown.hidden = true;
        dockTrigger?.setAttribute('aria-expanded', 'false');
    }
    dockTrigger?.addEventListener('click', (ev) => {
        ev.stopPropagation();
        const open = dockDropdown.hidden;
        dockDropdown.hidden = !open;
        dockTrigger.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
    document.addEventListener('click', (ev) => {
        if (!dockDropdown || dockDropdown.hidden) return;
        if (dockMenu.contains(ev.target)) return;
        closeDockMenu();
    });
    document.addEventListener('keydown', (ev) => {
        if (ev.key === 'Escape') closeDockMenu();
    });
    document.querySelectorAll('[data-action="set-panel-mode"]').forEach(b =>
        b.addEventListener('click', () => {
            const mode = b.dataset.mode;
            if (!VALID_MODES.includes(mode)) return;
            state.panelMode = mode;
            delete state.panelFloating;
            // Non-docked modes imply the panel is visible.
            if (mode !== 'docked') state.panelCollapsed = false;
            apply(state); save(state);
            closeDockMenu();
        }));

    // Drag handle on floating panel
    const panelAside = document.querySelector('.viewer-panel');
    const dragHandle = panelAside?.querySelector('[data-role="panel-drag-handle"]');
    if (panelAside && dragHandle) {
        dragHandle.addEventListener('pointerdown', (ev) => {
            if (normalizeMode(state) !== 'floating') return;
            if (ev.target.closest('button')) return; // clicking a header button shouldn't start a drag
            if (ev.button !== 0) return;
            ev.preventDefault();
            dragHandle.setPointerCapture(ev.pointerId);
            const rect = panelAside.getBoundingClientRect();
            const startX = ev.clientX, startY = ev.clientY;
            const startLeft = rect.left, startTop = rect.top;

            function move(e) {
                const nx = Math.max(8, Math.min(window.innerWidth - rect.width - 8, startLeft + (e.clientX - startX)));
                const ny = Math.max(8, Math.min(window.innerHeight - rect.height - 8, startTop + (e.clientY - startY)));
                grid.style.setProperty('--panel-float-left', nx + 'px');
                grid.style.setProperty('--panel-float-right', 'auto');
                grid.style.setProperty('--panel-float-top', ny + 'px');
            }
            function up(e) {
                try { dragHandle.releasePointerCapture(e.pointerId); } catch {}
                const r = panelAside.getBoundingClientRect();
                state.floatLeft = Math.round(r.left);
                state.floatTop  = Math.round(r.top);
                save(state);
                dragHandle.removeEventListener('pointermove', move);
                dragHandle.removeEventListener('pointerup', up);
                dragHandle.removeEventListener('pointercancel', up);
            }
            dragHandle.addEventListener('pointermove', move);
            dragHandle.addEventListener('pointerup', up);
            dragHandle.addEventListener('pointercancel', up);
        });

        // Persist resize changes — CSS `resize: both` (floating) or `resize: vertical` (below).
        const ro = new ResizeObserver(() => {
            const mode = normalizeMode(state);
            const r = panelAside.getBoundingClientRect();
            if (mode === 'floating') {
                state.floatW = Math.round(r.width);
                state.floatH = Math.round(r.height);
                save(state);
            } else if (mode === 'below') {
                state.belowH = Math.round(r.height);
                save(state);
            }
        });
        ro.observe(panelAside);
    }

    function wireResize(handle, which) {
        if (!handle) return;
        const varName = which === 'rail' ? '--rail-w' : '--panel-w';
        const storeKey = which === 'rail' ? 'railW' : 'panelW';
        handle.addEventListener('pointerdown', (ev) => {
            if (ev.button !== 0) return;
            ev.preventDefault();
            handle.setPointerCapture(ev.pointerId);
            handle.classList.add('is-active');
            grid.classList.add('is-resizing');
            const gridRect = grid.getBoundingClientRect();
            const min = 170, max = 520;

            function move(e) {
                const next = which === 'rail'
                    ? (e.clientX - gridRect.left) - 20
                    : (gridRect.right - e.clientX) - 20;
                const clamped = Math.max(min, Math.min(max, next));
                grid.style.setProperty(varName, clamped + 'px');
            }
            function up(e) {
                try { handle.releasePointerCapture(e.pointerId); } catch {}
                handle.classList.remove('is-active');
                grid.classList.remove('is-resizing');
                const final = parseFloat(getComputedStyle(grid).getPropertyValue(varName));
                state[storeKey] = Math.round(final);
                save(state);
                handle.removeEventListener('pointermove', move);
                handle.removeEventListener('pointerup', up);
                handle.removeEventListener('pointercancel', up);
            }
            handle.addEventListener('pointermove', move);
            handle.addEventListener('pointerup', up);
            handle.addEventListener('pointercancel', up);
        });
    }
    wireResize(document.querySelector('[data-role="resize-rail"]'),  'rail');
    wireResize(document.querySelector('[data-role="resize-panel"]'), 'panel');

    // Height +/- buttons for the panel (floating or below modes).
    const HEIGHT_STEP = 40;
    document.querySelectorAll('[data-action="panel-height-inc"], [data-action="panel-height-dec"]').forEach(b => {
        b.addEventListener('click', () => {
            const mode = normalizeMode(state);
            const delta = b.dataset.action === 'panel-height-inc' ? +HEIGHT_STEP : -HEIGHT_STEP;
            if (mode === 'floating') {
                const min = 200;
                const max = window.innerHeight - 40;
                const current = state.floatH || 520;
                const next = Math.max(min, Math.min(max, Math.round(current + delta)));
                state.floatH = next;
                grid.style.setProperty('--panel-float-h', next + 'px');
                apply(state); save(state);
            } else if (mode === 'below') {
                const min = 200;
                const max = Math.round(window.innerHeight * 0.85);
                const current = state.belowH || 560;
                const next = Math.max(min, Math.min(max, Math.round(current + delta)));
                state.belowH = next;
                grid.style.setProperty('--panel-below-h', next + 'px');
                if (panelEl) panelEl.style.height = next + 'px';
                apply(state); save(state);
            }
        });
    });
})();

(function viewerInfoDrawer() {
    const drawer = document.querySelector('[data-role="info-drawer"]');
    const scrim = document.querySelector('[data-role="info-scrim"]');
    if (!drawer || !scrim) return;

    const state = window.__viewerState?.get() || {};
    const save = window.__viewerState?.save || (() => {});

    function open(on) {
        drawer.hidden = !on;
        scrim.hidden = !on;
        // Use dataset flag for the CSS transition hook.
        requestAnimationFrame(() => {
            drawer.dataset.open = on ? 'true' : 'false';
            scrim.dataset.open = on ? 'true' : 'false';
            drawer.setAttribute('aria-hidden', on ? 'false' : 'true');
        });
        state.infoDrawerOpen = !!on;
        save();
    }

    document.querySelectorAll('[data-action="toggle-info"]').forEach(b =>
        b.addEventListener('click', () => open(drawer.hidden)));
    document.querySelectorAll('[data-action="close-info"]').forEach(b =>
        b.addEventListener('click', () => open(false)));
    scrim.addEventListener('click', () => open(false));
    document.addEventListener('keydown', ev => { if (ev.key === 'Escape' && !drawer.hidden) open(false); });

    if (state.infoDrawerOpen) open(true);
})();

(function viewerZoom() {
    'use strict';
    const canvas   = document.querySelector('[data-role="canvas"]');
    const zoomWrap = document.querySelector('[data-role="zoom-wrap"]');
    const levelEl  = document.querySelector('[data-role="zoom-level"]');
    if (!canvas || !zoomWrap) return;

    const MIN = 0.5, MAX = 4, STEP = 0.15;
    let scale = 1;

    function setScale(v) {
        scale = Math.max(MIN, Math.min(MAX, v));
        zoomWrap.style.transform = `scale(${scale})`;
        if (levelEl) levelEl.textContent = Math.round(scale * 100) + '%';
    }

    function reset() {
        setScale(1);
        canvas.scrollTo({ top: 0, left: 0 });
    }

    // Button handlers
    document.querySelector('[data-action="zoom-in"]')?.addEventListener('click', () => setScale(scale + STEP));
    document.querySelector('[data-action="zoom-out"]')?.addEventListener('click', () => setScale(scale - STEP));
    document.querySelector('[data-action="zoom-reset"]')?.addEventListener('click', reset);

    // Ctrl+Wheel zoom around cursor
    canvas.addEventListener('wheel', (ev) => {
        if (!ev.ctrlKey) return;
        ev.preventDefault();
        setScale(scale * (ev.deltaY > 0 ? 1 / 1.15 : 1.15));
    }, { passive: false });

    // Shift+drag to pan
    let panState = null;
    canvas.addEventListener('pointerdown', (ev) => {
        if (!ev.shiftKey) return;
        if (ev.button !== 0) return;
        ev.preventDefault();
        canvas.setPointerCapture(ev.pointerId);
        panState = {
            pointerId: ev.pointerId,
            startX: ev.clientX,
            startY: ev.clientY,
            scrollLeft: canvas.scrollLeft,
            scrollTop: canvas.scrollTop,
        };
        canvas.style.cursor = 'grabbing';
    });
    canvas.addEventListener('pointermove', (ev) => {
        if (!panState || panState.pointerId !== ev.pointerId) return;
        ev.preventDefault();
        canvas.scrollLeft = panState.scrollLeft - (ev.clientX - panState.startX);
        canvas.scrollTop  = panState.scrollTop  - (ev.clientY - panState.startY);
    });
    function endPan(ev) {
        if (!panState || (ev && panState.pointerId !== ev.pointerId)) return;
        try { canvas.releasePointerCapture(panState.pointerId); } catch {}
        panState = null;
        canvas.style.cursor = '';
    }
    canvas.addEventListener('pointerup', endPan);
    canvas.addEventListener('pointercancel', endPan);
})();

// ── Stage 11: Search overlay ──────────────────────────────────────────────────
(function viewerSearch() {
    'use strict';
    const overlay = document.querySelector('[data-role="search-overlay"]');
    const input = overlay?.querySelector('[data-role="search-input"]');
    const list = overlay?.querySelector('[data-role="search-results"]');
    if (!overlay || !input || !list) return;

    function openOverlay(on) {
        overlay.hidden = !on;
        if (on) setTimeout(() => input.focus(), 0);
    }
    document.querySelectorAll('[data-action="open-search"]').forEach(b =>
        b.addEventListener('click', () => openOverlay(true)));
    document.querySelectorAll('[data-action="close-search"]').forEach(b =>
        b.addEventListener('click', () => openOverlay(false)));
    overlay.addEventListener('click', ev => { if (ev.target === overlay) openOverlay(false); });

    document.addEventListener('keydown', ev => {
        if (ev.target.matches('input, textarea, [contenteditable]')) return;
        if (ev.key === '/') { openOverlay(true); ev.preventDefault(); }
        if (ev.key === 'Escape' && !overlay.hidden) openOverlay(false);
    });

    function strip(html) { return String(html || '').replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim(); }
    function snippet(text, term, len = 160) {
        const i = text.toLowerCase().indexOf(term.toLowerCase());
        if (i < 0) return text.slice(0, len);
        const from = Math.max(0, i - 60);
        return (from > 0 ? '…' : '') + text.slice(from, from + len);
    }

    input.addEventListener('input', () => {
        const term = input.value.trim();
        list.innerHTML = '';
        if (term.length < 2 || !window.__viewerScenes) return;
        const scenes = window.__viewerScenes.all();
        const needle = term.toLowerCase();
        const hits = [];
        scenes.forEach(scene => {
            (scene.markers || []).forEach(m => {
                const fields = ['description','ziel','datenablage','quellen','promptIdee','reflexion','model'];
                for (const f of fields) {
                    const hay = strip(m[f]);
                    if (hay.toLowerCase().includes(needle) || String(m.number).includes(needle)) {
                        hits.push({ scene, marker: m, snippet: snippet(hay, term) });
                        break;
                    }
                }
            });
        });
        if (!hits.length) { list.innerHTML = '<li class="viewer-search-empty">Keine Treffer.</li>'; return; }
        hits.forEach(h => {
            const li = document.createElement('li');
            li.innerHTML = `
                <button type="button" class="viewer-search-result">
                  <span class="chip" style="background:${h.marker.colorHex};">${h.marker.number}</span>
                  <span class="meta">Szene ${h.scene.number}${h.scene.name ? ' · ' + h.scene.name : ''}</span>
                  <span class="sn">${h.snippet || ''}</span>
                </button>`;
            li.querySelector('button').addEventListener('click', () => {
                window.__viewerScenes.setActive(h.scene.id);
                window.__viewerMarkers?.select(h.marker.id);
                openOverlay(false);
            });
            list.appendChild(li);
        });
    });
})();

// ── Stage 12.1: PDF export ────────────────────────────────────────────────────
(function viewerPdf() {
    'use strict';
    const btns = document.querySelectorAll('[data-action="export-pdf"]');
    if (!btns.length) return;

    const grid = document.querySelector('[data-role="viewer-grid"]');
    if (!grid) return;

    const PATH_BASE = (function() {
        // Derive from the page's base path; fall back to empty string.
        const base = document.querySelector('base');
        if (base && base.href) {
            const u = new URL(base.href);
            return u.pathname.replace(/\/$/, '');
        }
        return '';
    })();

    const API = { list: sceneId => fetch(`${PATH_BASE}/api/scenes/${sceneId}/markers`).then(r => r.ok ? r.json() : []) };
    const TAXO_NAMES = ['Erinnern','Verstehen','Anwenden','Analysieren','Bewerten','Erschaffen'];

    // Build meta from embedded scenes data + grid data attributes.
    function getMeta() {
        const scenes = window.__viewerScenes ? window.__viewerScenes.all() : [];
        return {
            title: grid.dataset.title || document.querySelector('.viewer-tb-title')?.textContent || 'Storyboard',
            zg: grid.dataset.zg || '',
            lz: grid.dataset.lz || '',
            desc: grid.dataset.desc || '',
            taxo: grid.dataset.taxo || '',
            license: grid.dataset.license || '',
            authors: (grid.dataset.authors || '').split(',').map(x => x.trim()).filter(Boolean),
            lextras: grid.dataset.lextras || '',
            palette: (grid.dataset.palette || '').split(',').map(x => x.trim()).filter(Boolean)
        };
    }

    const N = m => ({
        id: m.id ?? m.Id,
        x: m.x ?? m.X,
        y: m.y ?? m.Y,
        number: m.number ?? m.Number ?? 0,
        colorHex: m.colorHex ?? m.ColorHex ?? '#4a86e8',
        description: m.description ?? m.Description ?? '',
        ziel: m.ziel ?? m.Ziel ?? '',
        datenablage: m.datenablage ?? m.Datenablage ?? '',
        quellen: m.quellen ?? m.Quellen ?? '',
        promptIdee: m.promptIdee ?? m.PromptIdee ?? '',
        reflexion: m.reflexion ?? m.Reflexion ?? '',
        model: m.model ?? m.Model ?? '',
        sceneId: m.sceneId ?? m.SceneId,
        taxonomie: m.taxonomie ?? m.Taxonomie ?? null
    });

    function hexToRgb(hex) {
        const h = hex.replace('#', '').trim();
        if (h.length === 3) { const r = h[0]+h[0], g = h[1]+h[1], b = h[2]+h[2]; return [parseInt(r,16),parseInt(g,16),parseInt(b,16)]; }
        if (h.length >= 6) return [parseInt(h.slice(0,2),16),parseInt(h.slice(2,4),16),parseInt(h.slice(4,6),16)];
        return [74,134,232];
    }
    function htmlToText(input) {
        try {
            const div = document.createElement('div');
            div.innerHTML = String(input || '');
            const txt = div.textContent || div.innerText || '';
            return txt.replace(/\s+/g, ' ').trim();
        } catch { return String(input || ''); }
    }

    function parseHtmlToElements(html) {
        const root = document.createElement('div'); root.innerHTML = String(html || '');
        const elements = [];

        function headingFontSize(tag) {
            switch (tag) {
                case 'h1': return 18;
                case 'h2': return 15;
                case 'h3': return 13;
                case 'h4': return 12;
                case 'h5': return 11;
                case 'h6': return 10;
                default: return null;
            }
        }

        function deriveStyle(node, base) {
            const st = {
                bold: !!(base && base.bold),
                italic: !!(base && base.italic),
                underline: !!(base && base.underline),
                color: (base && base.color) || null,
                fontSize: (base && base.fontSize) || null
            };
            if (!node || node.nodeType !== 1) return st;
            const tag = node.tagName.toLowerCase();
            if (tag === 'b' || tag === 'strong') st.bold = true;
            if (tag === 'i' || tag === 'em') st.italic = true;
            if (tag === 'u') st.underline = true;
            if (tag === 'a') { st.underline = true; st.color = st.color || '#1d4ed8'; }
            const hSize = headingFontSize(tag);
            if (hSize) { st.fontSize = hSize; st.bold = true; }
            const css = (node.getAttribute && node.getAttribute('style') || '').toLowerCase();
            if (css) {
                if (/font-weight\s*:\s*(bold|600|700|800|900)/.test(css)) st.bold = true;
                if (/font-style\s*:\s*italic/.test(css)) st.italic = true;
                if (/text-decoration[^;]*underline/.test(css)) st.underline = true;
                const cm = css.match(/(?:^|;|\s)color\s*:\s*(#[0-9a-f]{3,8}|rgb\([^)]+\))/);
                if (cm) st.color = cm[1];
                const fsm = css.match(/font-size\s*:\s*([0-9.]+)\s*(px|pt|em|rem|%)/);
                if (fsm) {
                    const v = parseFloat(fsm[1]); const unit = fsm[2];
                    let pt = 10;
                    if (unit === 'pt') pt = v;
                    else if (unit === 'px') pt = v * 0.75;
                    else if (unit === 'em' || unit === 'rem') pt = 10 * v;
                    else if (unit === '%') pt = 10 * (v / 100);
                    st.fontSize = Math.max(6, Math.min(36, pt));
                }
            }
            return st;
        }

        function collectRuns(node, baseStyle) {
            const runs = [];
            const nodeSt = deriveStyle(node, baseStyle);
            node.childNodes.forEach(ch => {
                if (ch.nodeType === 3) {
                    const t = ch.textContent || '';
                    if (t) runs.push({ text: t, ...nodeSt });
                } else if (ch.nodeType === 1) {
                    const tag = ch.tagName.toLowerCase();
                    if (tag === 'br') { runs.push({ text: '\n', br: true, ...nodeSt }); return; }
                    runs.push(...collectRuns(ch, nodeSt));
                }
            });
            return runs;
        }

        function addParagraph(runs) { elements.push({ type: 'p', runs }); }
        function addList(items, ordered) { elements.push({ type: ordered ? 'ol' : 'ul', items }); }

        function handle(node) {
            if (node.nodeType === 3) {
                const t = node.textContent || '';
                if (t.trim()) addParagraph([{ text: t }]);
                return;
            }
            if (node.nodeType !== 1) return;
            const tag = node.tagName.toLowerCase();
            if (tag === 'ul' || tag === 'ol') {
                const items = []; let idx = 1;
                node.querySelectorAll(':scope>li').forEach(li => {
                    items.push({ runs: collectRuns(li, {}), idx: idx++ });
                });
                addList(items, tag === 'ol');
            } else if (tag === 'br') {
                addParagraph([{ text: '\n' }]);
            } else {
                const runs = collectRuns(node, {});
                if (runs.length) addParagraph(runs);
            }
        }

        Array.from(root.childNodes).forEach(handle);
        return elements;
    }

    function measureTextWidth(pdf, text, style) {
        const s = style?.bold && style?.italic ? 'bolditalic' : (style?.bold ? 'bold' : (style?.italic ? 'italic' : 'normal'));
        pdf.setFont('helvetica', s);
        pdf.setFontSize(style?.fontSize || 10);
        return pdf.getTextWidth(text || '');
    }

    function wrapElementsToLines(pdf, elements, maxWidth) {
        const lines = [];
        const pushLine = (segments, indent = 0) => { lines.push({ segments, indent }); };
        function flushBuffer(buf, indent) { if (buf.length) pushLine(buf.splice(0, buf.length), indent || 0); }
        function wrapRuns(runs, indent) {
            let curW = indent || 0; let buf = [];
            const addWord = (word, style) => {
                const w = measureTextWidth(pdf, word, style);
                if (curW + w > maxWidth && buf.length) { flushBuffer(buf, indent); curW = indent || 0; }
                buf.push({ text: word, style, xOffset: curW }); curW += w;
            };
            const newLine = () => { flushBuffer(buf, indent); curW = indent || 0; };
            runs.forEach(r => {
                if (r.br) { newLine(); return; }
                const parts = String(r.text || '').split(/(\s+)/);
                parts.forEach(p => {
                    if (!p) return;
                    const isSpace = /^(\s+)$/.test(p);
                    if (isSpace && curW === (indent || 0)) return;
                    addWord(p, { bold: !!r.bold, italic: !!r.italic, underline: !!r.underline, color: r.color || null, fontSize: r.fontSize || null });
                });
            });
            flushBuffer(buf, indent);
        }
        function wrapRunsToLines(runs, indent) {
            let curW = indent || 0; let buf = []; const out = [];
            const addOut = () => { if (buf.length) out.push({ segments: buf.splice(0, buf.length), indent: indent || 0 }); };
            runs.forEach(r => {
                if (r.br) { addOut(); curW = indent || 0; return; }
                const parts = String(r.text || '').split(/(\s+)/);
                parts.forEach(p => {
                    if (!p) return;
                    const isSpace = /^(\s+)$/.test(p);
                    if (isSpace && curW === (indent || 0)) return;
                    const style = { bold: !!r.bold, italic: !!r.italic, underline: !!r.underline, color: r.color || null, fontSize: r.fontSize || null };
                    const w = measureTextWidth(pdf, p, style);
                    if (curW + w > maxWidth && buf.length) { addOut(); curW = indent || 0; }
                    buf.push({ text: p, style }); curW += w;
                });
            });
            addOut();
            return out;
        }
        elements.forEach(el => {
            if (el.type === 'p') {
                wrapRuns(el.runs, 0);
                lines.push({ segments: [{ text: '', style: {} }], gapAfter: true, indent: 0 });
            } else if (el.type === 'ul' || el.type === 'ol') {
                el.items.forEach((it, idx) => {
                    const bullet = el.type === 'ol' ? (String(idx + 1) + '. ') : '\u2022 ';
                    const bW = measureTextWidth(pdf, bullet, { bold: false, italic: false });
                    const out = wrapRunsToLines(it.runs, bW);
                    if (out.length === 0) {
                        lines.push({ segments: [{ text: bullet, style: {} }], indent: 0 });
                    } else {
                        out[0].segments.unshift({ text: bullet, style: {} });
                        out.forEach((ln, i) => { ln.indent = (i === 0 ? 0 : bW); });
                        out.forEach(ln => lines.push(ln));
                    }
                });
                lines.push({ segments: [{ text: '', style: {} }], gapAfter: true, indent: 0 });
            }
        });
        return lines;
    }

    function lineHeightFor(line, baseLineH) {
        const maxFs = (line.segments || []).reduce((m, s) => Math.max(m, (s.style && s.style.fontSize) || 10), 10);
        return Math.max(baseLineH, maxFs * 0.48);
    }

    function drawStyledLinesInBox(pdf, lines, boxX, boxY, boxW, bottom, padX, padY, baseLineH) {
        let curY = boxY + padY;
        let i = 0;
        while (i < lines.length) {
            const line = lines[i];
            const lh = lineHeightFor(line, baseLineH);
            if (curY + lh > bottom) break;
            const baselineY = curY + lh * 0.78;
            let curX = boxX + padX + (line.indent || 0);
            line.segments.forEach(seg => {
                const s = seg.style || {};
                const st = s.bold && s.italic ? 'bolditalic' : (s.bold ? 'bold' : (s.italic ? 'italic' : 'normal'));
                const fs = s.fontSize || 10;
                const color = (s.color && /^#/.test(s.color)) ? s.color : null;
                if (color) {
                    const [r, g, b] = (function(hex) { hex = hex.replace('#', ''); if (hex.length === 3) { hex = hex.split('').map(c => c + c).join(''); } return [parseInt(hex.slice(0,2),16),parseInt(hex.slice(2,4),16),parseInt(hex.slice(4,6),16)]; })(color);
                    pdf.setTextColor(r, g, b);
                } else { pdf.setTextColor(17, 24, 39); }
                pdf.setFont('helvetica', st); pdf.setFontSize(fs);
                const w = measureTextWidth(pdf, seg.text || '', s);
                pdf.text(seg.text || '', curX, baselineY);
                if (s.underline) {
                    if (color) {
                        const hx = color.replace('#', '');
                        const h6 = hx.length === 3 ? hx.split('').map(c => c + c).join('') : hx;
                        pdf.setDrawColor(parseInt(h6.slice(0, 2), 16), parseInt(h6.slice(2, 4), 16), parseInt(h6.slice(4, 6), 16));
                    } else { pdf.setDrawColor(17); }
                    pdf.setLineWidth(0.2);
                    pdf.line(curX, baselineY + 0.8, curX + w, baselineY + 0.8);
                }
                curX += w;
            });
            curY += lh;
            if (line.gapAfter) curY += 2;
            i++;
        }
        return { y: curY, consumed: i };
    }

    function drawLabeledHtmlBlock(pdf, ctx, label, html, x, y, maxW) {
        const { page, margin, contentW } = ctx;
        const bottom = page.h - margin - 8;
        const padX = 5, padY = 3.2, r = 1.8, labelGap = 2.5;
        const boxX = margin; const boxW = contentW; const baseLineH = 4.8;
        pdf.setFont('helvetica', 'bold'); pdf.setFontSize(13);
        const labelW = pdf.getTextWidth(label), labelH = 6.5;
        const minBlockH = labelH + labelGap + (baseLineH + padY * 2);
        if (y + minBlockH > bottom) { pdf.addPage(); y = margin + 6; }
        pdf.setTextColor(15, 23, 42);
        pdf.text(label, x, y + 5);
        pdf.setDrawColor(203, 213, 225); pdf.setLineWidth(0.3);
        pdf.line(x, y + 6.8, x + labelW, y + 6.8);
        const elements = parseHtmlToElements(html);
        let lines = wrapElementsToLines(pdf, elements, Math.max(20, boxW - padX * 2));
        let curY = y + labelH + labelGap;
        while (lines.length) {
            const avail = bottom - curY - padY * 2;
            let consumedH = 0, count = 0;
            for (const line of lines) {
                const lh = lineHeightFor(line, baseLineH);
                const gapAdd = line.gapAfter ? 2 : 0;
                if (consumedH + lh + gapAdd > avail) break;
                consumedH += lh + gapAdd;
                count++;
            }
            if (count === 0) { pdf.addPage(); curY = margin + 6; continue; }
            const use = lines.slice(0, count);
            const boxH = consumedH + padY * 2;
            pdf.setFillColor(248, 250, 252);
            pdf.roundedRect(boxX, curY, boxW, boxH, r, r, 'F');
            const res = drawStyledLinesInBox(pdf, use, boxX, curY, boxW, bottom, padX, padY, baseLineH);
            curY = res.y + 4;
            lines = lines.slice(res.consumed);
            if (lines.length) { pdf.addPage(); curY = margin + 6; }
        }
        return curY;
    }

    function pickPrimary(meta) {
        const raw = meta.palette.find(x => x) || getComputedStyle(document.documentElement).getPropertyValue('--role-primary-500') || '#4a86e8';
        return (raw.trim().startsWith('#') ? raw.trim() : ('#' + raw.trim()));
    }

    async function imageToDataUrl(imgEl) {
        if (!imgEl.complete || !imgEl.naturalWidth) {
            await new Promise(res => imgEl.addEventListener('load', res, { once: true }));
        }
        const cnv = document.createElement('canvas');
        cnv.width = imgEl.naturalWidth; cnv.height = imgEl.naturalHeight;
        const ctx = cnv.getContext('2d'); ctx.drawImage(imgEl, 0, 0);
        return { dataUrl: cnv.toDataURL('image/jpeg', 0.95), w: cnv.width, h: cnv.height };
    }

    async function svgToPngDataUrl(url, targetWpx) {
        return new Promise((resolve, reject) => {
            const im = new Image();
            im.crossOrigin = 'anonymous';
            im.onload = () => {
                try {
                    const w = targetWpx || im.width || 320;
                    const h = Math.round(w * (im.height / im.width || 0.3) || w * 0.3);
                    const cnv = document.createElement('canvas'); cnv.width = w; cnv.height = h;
                    const ctx = cnv.getContext('2d'); ctx.drawImage(im, 0, 0, w, h);
                    resolve({ dataUrl: cnv.toDataURL('image/png'), w, h });
                } catch (e) { reject(e); }
            };
            im.onerror = reject;
            im.src = url;
        });
    }

    async function imageUrlToDataUrl(url) {
        return new Promise((resolve, reject) => {
            const im = new Image();
            im.crossOrigin = 'anonymous';
            im.onload = async () => {
                try {
                    const cnv = document.createElement('canvas');
                    cnv.width = im.naturalWidth; cnv.height = im.naturalHeight;
                    const ctx = cnv.getContext('2d'); ctx.drawImage(im, 0, 0);
                    resolve({ dataUrl: cnv.toDataURL('image/jpeg', 0.95), w: cnv.width, h: cnv.height });
                } catch (e) { reject(e); }
            };
            im.onerror = reject;
            im.src = url;
        });
    }

    async function generatePdf(mode) {
        const meta = getMeta();
        try {
            if (!(window.jspdf && window.jspdf.jsPDF)) { alert('PDF Bibliothek konnte nicht geladen werden.'); return; }
            const { jsPDF } = window.jspdf;
            const pdf = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4', putOnlyUsedFonts: true });
            const page = { w: pdf.internal.pageSize.getWidth(), h: pdf.internal.pageSize.getHeight() }, margin = 12, contentW = page.w - margin * 2;
            const innerX = margin;
            const innerW = contentW;

            const primaryHex = pickPrimary(meta);
            const [pr, pg, pb] = hexToRgb(primaryHex);
            const ctx = { page, margin, contentW, pr, pg, pb };
            const lineH = 4.8;

            // Get current scene info from the viewer
            const activeScene = window.__viewerScenes?.getActive();
            const currentSceneId = activeScene?.id ?? Number(grid.dataset.activeSceneId) ?? 0;
            const img = document.querySelector('[data-role="scene-img"]');
            const markers = (activeScene?.markers || []).map(N);

            let didPageBreak = false;

            function drawLabeledBlock(label, value, x, y, maxW) {
                if (!value) return y;
                const padX = 5, padY = 3.2, r = 1.8, labelGap = 2.5;
                const bottom = page.h - margin - 8;
                const toText = (v) => String(v == null ? '' : v);
                const boxX = margin;
                const boxW = contentW;
                const lines = pdf.splitTextToSize(toText(value), Math.max(20, boxW - padX * 2));
                pdf.setFont('helvetica', 'bold'); pdf.setFontSize(13);
                const labelW = pdf.getTextWidth(label), labelH = 6.5;
                const minBlockH = labelH + labelGap + (lineH + padY * 2);
                if (y + minBlockH > bottom) { pdf.addPage(); didPageBreak = true; y = margin + 6; }
                pdf.setTextColor(15, 23, 42);
                pdf.text(label, x, y + 5);
                pdf.setDrawColor(203, 213, 225); pdf.setLineWidth(0.3);
                pdf.line(x, y + 6.8, x + labelW, y + 6.8);
                let i = 0;
                let curY = y + labelH + labelGap;
                pdf.setTextColor(17, 24, 39); pdf.setFont('helvetica', 'normal'); pdf.setFontSize(10);
                while (i < lines.length) {
                    const avail = bottom - curY;
                    const maxLinesThisPage = Math.floor((avail - padY * 2) / lineH);
                    if (maxLinesThisPage <= 0) { pdf.addPage(); didPageBreak = true; curY = margin + 6; continue; }
                    const slice = lines.slice(i, i + maxLinesThisPage);
                    const boxH = slice.length * lineH + padY * 2;
                    pdf.setFillColor(248, 250, 252);
                    pdf.roundedRect(boxX, curY, boxW, boxH, r, r, 'F');
                    pdf.setTextColor(17, 24, 39); pdf.setFont('helvetica', 'normal'); pdf.setFontSize(10);
                    let ty = curY + padY + 3.6;
                    slice.forEach(t => { pdf.text(t, boxX + padX, ty); ty += lineH; });
                    i += slice.length;
                    curY += boxH + 4;
                    if (i < lines.length) { pdf.addPage(); didPageBreak = true; curY = margin + 6; }
                }
                return curY;
            }

            // Header — always uni-green (#89ba17), independent of storyboard palette
            const headerH = 40;
            const titleOrig = meta.title;
            pdf.setFillColor(137, 186, 23);
            pdf.rect(0, 0, page.w, headerH, 'F');

            try {
                const logo = await svgToPngDataUrl(`${PATH_BASE}/images/logo_header_white.svg`, 2000);
                const ratio = logo.h / logo.w;
                const logoWmm = 70;
                const logoHmm = logoWmm * ratio;
                const lx = margin + 3; const ly = Math.max(2, (headerH - logoHmm) / 2);
                pdf.addImage(logo.dataUrl, 'PNG', lx, ly, logoWmm, logoHmm);
            } catch {}

            const sw = 6.2, palGap = 2.8, MAX_SWATCHES = 8;
            const palette = meta.palette.slice(0, MAX_SWATCHES).map(h => h.startsWith('#') ? h : '#' + h);

            const bodyTop = headerH + 12;

            let yCursor = bodyTop;
            const hasTitle = String(titleOrig || '').replace(/<[^>]*>/g, '').trim();
            if (hasTitle) { yCursor = drawLabeledHtmlBlock(pdf, ctx, 'Titel', String(titleOrig || ''), innerX, yCursor, innerW); yCursor += 6; }
            const hasDesc = String(meta.desc || '').replace(/<[^>]*>/g, '').trim();
            if (hasDesc) { yCursor = drawLabeledHtmlBlock(pdf, ctx, 'Beschreibung', String(meta.desc || ''), innerX, yCursor, innerW); yCursor += 6; }
            yCursor = drawLabeledBlock('Taxonomie', meta.taxo, innerX, yCursor, innerW); yCursor += 6;
            const hasZg = String(meta.zg || '').replace(/<[^>]*>/g, '').trim();
            if (hasZg) { yCursor = drawLabeledHtmlBlock(pdf, ctx, 'Zielgruppe', String(meta.zg || ''), innerX, yCursor, innerW); yCursor += 6; }
            const hasLz = String(meta.lz || '').replace(/<[^>]*>/g, '').trim();
            if (hasLz) { yCursor = drawLabeledHtmlBlock(pdf, ctx, 'Lernziel', String(meta.lz || ''), innerX, yCursor, innerW); }

            function drawPaletteBlock(label, hexes, x, y, maxW) {
                const items = (hexes || []).map(h => h && (h.startsWith('#') ? h : '#' + h)).filter(Boolean);
                if (!items.length) return y;
                const padX = 5, padY = 4, r = 1.8, labelGap = 2.5, gapY = 6.0, gapX = 10.0;
                pdf.setFont('helvetica', 'bold'); pdf.setFontSize(13);
                const labelW = pdf.getTextWidth(label), labelH = 6.5;
                const bottom = page.h - margin - 8;
                pdf.setFont('helvetica', 'normal'); pdf.setFontSize(9);
                const allowedContent = Math.max(20, (margin + contentW - x) - padX * 2 - 1);
                const cellW = Math.max(sw + 8, Math.max(...items.map(h => pdf.getTextWidth(h))) + 8);
                const cellH = sw + 6.0 + 5.2;
                const cols = Math.max(1, Math.floor((Math.min(maxW, allowedContent)) / (cellW + gapX)));
                const rows = Math.ceil(items.length / cols);
                const contentWpx = Math.min(Math.min(maxW, allowedContent), cols * cellW + (cols - 1) * gapX);
                const contentHpx = rows * cellH + (rows - 1) * gapY;
                const boxW = contentWpx + padX * 2;
                const boxH = contentHpx + padY * 2;
                const totalH = labelH + labelGap + boxH;
                if (y + totalH > bottom) { pdf.addPage(); didPageBreak = true; y = margin + 6; }
                pdf.setFont('helvetica', 'bold'); pdf.setFontSize(13);
                pdf.setTextColor(15, 23, 42);
                pdf.text(label, x, y + 5);
                pdf.setDrawColor(203, 213, 225); pdf.setLineWidth(0.3);
                pdf.line(x, y + 6.8, x + labelW, y + 6.8);
                const boxX = x, boxY = y + labelH + labelGap;
                pdf.setFillColor(248, 250, 252); pdf.roundedRect(boxX, boxY, boxW, boxH, r, r, 'F');
                let idx = 0;
                for (let rr = 0; rr < rows; rr++) {
                    for (let c = 0; c < cols && idx < items.length; c++, idx++) {
                        const hex = items[idx];
                        const [r1, g1, b1] = hexToRgb(hex);
                        const cx = boxX + padX + c * (cellW + gapX);
                        const cy = boxY + padY + rr * (cellH + gapY);
                        const swX = cx + (cellW - sw) / 2;
                        pdf.setFillColor(r1, g1, b1);
                        pdf.setDrawColor(226, 232, 240); pdf.setLineWidth(0.25);
                        pdf.roundedRect(swX, cy, sw, sw, 1, 1, 'FD');
                        pdf.setTextColor(51, 65, 85); pdf.setFont('helvetica', 'normal'); pdf.setFontSize(9);
                        pdf.text(hex, cx + cellW / 2, cy + sw + 4.6, { align: 'center' });
                    }
                }
                return boxY + boxH;
            }
            if (palette && palette.length) { yCursor += 4; yCursor = drawPaletteBlock('Palette', palette, innerX, yCursor, innerW); }
            yCursor += 8;

            if (didPageBreak) { pdf.addPage(); yCursor = margin + 6; didPageBreak = false; }

            try {
                const sceneLabel = activeScene?.name || (activeScene ? `Szene ${activeScene.number}` : '');
                if (sceneLabel) {
                    pdf.setTextColor(15, 23, 42);
                    pdf.setFont('helvetica', 'bold'); pdf.setFontSize(17);
                    pdf.text(sceneLabel, innerX, yCursor + 6);
                    pdf.setDrawColor(137, 186, 23); pdf.setLineWidth(0.8);
                    const slW = pdf.getTextWidth(sceneLabel);
                    pdf.line(innerX, yCursor + 8.4, innerX + slW, yCursor + 8.4);
                    yCursor += 14;
                }
            } catch {}

            const snap = img ? await imageToDataUrl(img) : null;
            if (snap) {
                const imgRatio = snap.w / snap.h;
                let imgWmm = contentW - 12;
                let imgHmm = imgWmm / imgRatio;
                const bodyBottom = page.h - 12, safePad = 8, maxImgHmmHere = Math.max(40, bodyBottom - yCursor - safePad);
                if (imgHmm > maxImgHmmHere) {
                    if (maxImgHmmHere < 60) { pdf.addPage(); yCursor = margin + 6; }
                    const bodyBottom2 = page.h - 12;
                    const maxImgHmm = Math.max(40, bodyBottom2 - yCursor - safePad);
                    imgHmm = Math.min(imgHmm, maxImgHmm);
                    imgWmm = imgHmm * imgRatio;
                    if (imgWmm > contentW - 12) { imgWmm = contentW - 12; imgHmm = imgWmm / imgRatio; }
                }
                const imgX = margin + (contentW - imgWmm) / 2, imgY = yCursor;
                pdf.addImage(snap.dataUrl, 'JPEG', imgX, imgY, imgWmm, imgHmm);

                const dotR = 2.4, badgeH = 6.2, padXNum = 2.2;
                pdf.setFont('helvetica', 'bold'); pdf.setFontSize(11.5);
                markers.slice().sort((a, b) => a.number - b.number || a.id - b.id).forEach(m => {
                    const colorHex = (m.colorHex || primaryHex);
                    const [r, g, b] = hexToRgb(colorHex);
                    const cx = imgX + (m.x || 0) * imgWmm;
                    const cy = imgY + (m.y || 0) * imgHmm;
                    pdf.setFillColor(r, g, b); pdf.setDrawColor(255, 255, 255); pdf.setLineWidth(0.6);
                    pdf.circle(cx, cy, dotR, 'FD');
                    const num = String(m.number ?? ''); const tw = pdf.getTextWidth(num);
                    const badgeW = Math.max(9, tw + padXNum * 2);
                    const rightEdge = imgX + imgWmm;
                    const placeRight = (cx + dotR + 1.8 + badgeW <= rightEdge);
                    const bx = placeRight ? (cx + dotR + 1.8) : (cx - dotR - 1.8 - badgeW);
                    const by = cy - badgeH / 2;
                    pdf.setFillColor(r, g, b); pdf.setDrawColor(255, 255, 255);
                    pdf.roundedRect(bx, by, badgeW, badgeH, 1.6, 1.6, 'FD');
                    pdf.setTextColor(255, 255, 255);
                    pdf.text(num, bx + badgeW / 2, by + badgeH / 2 + 1.6, { align: 'center' });
                });
            }

            // Marker details page
            pdf.addPage();
            let y = margin + 6;
            const sorted = markers.slice().sort((a, b) => a.number - b.number || a.id - b.id);
            const bottomD = page.h - 12;
            let firstMarker = true;
            sorted.forEach(m => {
                if (!firstMarker) { y += 10; }
                if (y + 18 > bottomD) { pdf.addPage(); y = margin + 6; }

                // Marker color chip
                const mColor = m.colorHex || primaryHex;
                const [mr, mg, mb] = hexToRgb(mColor);
                pdf.setFillColor(mr, mg, mb); pdf.setDrawColor(mr, mg, mb);
                pdf.roundedRect(innerX, y, 6, 9, 1.4, 1.4, 'F');

                // Marker heading
                pdf.setTextColor(15, 23, 42); pdf.setFont('helvetica', 'bold'); pdf.setFontSize(16);
                pdf.text(`Marker #${String(m.number ?? '')}`, innerX + 9, y + 6.8);

                // Full-width divider beneath
                pdf.setDrawColor(226, 232, 240); pdf.setLineWidth(0.4);
                pdf.line(innerX, y + 12, innerX + contentW, y + 12);
                y += 17;

                const taxo = (function(v) { const n = (typeof v === 'number' && v >= 0) ? v : null; return n == null ? '' : TAXO_NAMES.slice(0, Math.min(n + 1, TAXO_NAMES.length)).join(', '); })(m.taxonomie);
                const blocks = [
                    ['Taxonomie', taxo, 'plain'],
                    ['Ziel', String(m.ziel || ''), 'html'],
                    ['Beschreibung', String(m.description || ''), 'html'],
                    ['Datenablage', String(m.datenablage || ''), 'plain'],
                    ['Quellen', String(m.quellen || ''), 'html'],
                    ['Prompt-Idee', String(m.promptIdee || ''), 'html'],
                    ['Reflexion – Notizen', String(m.reflexion || ''), 'html'],
                    ['Model', String(m.model || ''), 'plain'],
                ];
                blocks.forEach(([label, val, kind]) => {
                    const has = (typeof val === 'string') ? val.replace(/<[^>]*>/g, '').trim() : String(val || '').trim();
                    if (!has) return;
                    if (kind === 'html') y = drawLabeledHtmlBlock(pdf, ctx, label, val, innerX, y, innerW);
                    else y = drawLabeledBlock(label, htmlToText(val), innerX, y, innerW);
                    y += 5;
                });
                firstMarker = false;
            });

            // Declaration page
            pdf.addPage();
            let yDecl = margin + 6;
            const declTitle = 'Erklärung zur Freigabe unter Open-Source-Lizenz';
            pdf.setTextColor(15, 23, 42); pdf.setFont('helvetica', 'bold'); pdf.setFontSize(15);
            pdf.text(declTitle, margin, yDecl + 5);
            pdf.setDrawColor(137, 186, 23); pdf.setLineWidth(0.8);
            const declW = pdf.getTextWidth(declTitle);
            pdf.line(margin, yDecl + 7.4, margin + declW, yDecl + 7.4);
            yDecl += 14;
            pdf.setTextColor(33); pdf.setFont('helvetica', 'normal'); pdf.setFontSize(11);
            const wrap = (t) => pdf.splitTextToSize(String(t || ''), page.w - margin * 2);
            const p1 = 'Ich / Wir, die unterzeichnende(n) Autor:in(nen), erkläre(n) hiermit, dass das Storyboard für das Lernmodul sowie das daraus erstellte Lernmodul von mir / uns erstellt wurde und frei von Rechten Dritter ist.';
            const p2 = 'Ich / Wir stelle(n) diese Werke hiermit unter die unten genannte Lizenz. Damit werden die Werke der Allgemeinheit zur freien Nutzung, Verbreitung, Bearbeitung und Veröffentlichung zugänglich gemacht. Eine Nutzung ist ohne Einschränkungen gemäß der Lizenzbestimmung erlaubt.';
            wrap(p1).forEach(line => { pdf.text(line, margin, yDecl); yDecl += 5.2; });
            yDecl += 2;
            wrap(p2).forEach(line => { pdf.text(line, margin, yDecl); yDecl += 5.2; });
            yDecl += 6;
            const licText = (function(key) {
                switch ((key || '').trim()) {
                    case 'Attribution_CC_BY': return 'Attribution (CC BY)';
                    case 'Attribution_ShareAlike_CC_BY_SA': return 'Attribution-ShareAlike (CC BY-SA)';
                    case 'Public_Domain_Dedication_CC0': return 'CC0 1.0 – Public Domain Dedication';
                    case 'Copyright': return 'Copyright';
                    case 'MIT': return 'MIT';
                    default: return '';
                }
            })(meta.license);
            const kv = (label, value) => {
                const full = (label + ' ' + (value || '')).trim();
                const lines = wrap(full);
                lines.forEach(ln => { pdf.text(ln, margin, yDecl); yDecl += 5.2; });
            };
            const plainTitleForDecl = htmlToText(meta.title);
            kv('Titel des Werks:', plainTitleForDecl);
            kv('Lizenzbestimmung:', licText);
            if ((meta.lextras || '').trim()) { kv('Zusatz zur Lizenz:', meta.lextras); }
            yDecl += 6;
            pdf.setFont('helvetica', 'bold');
            pdf.text('Autor:in / Autor:innen:', margin, yDecl);
            yDecl += 6;
            pdf.setFont('helvetica', 'normal');
            const authors = Array.isArray(meta.authors) ? meta.authors : [];
            if (authors.length > 0) {
                authors.forEach(name => {
                    const nm = String(name || '').trim();
                    if (nm) { pdf.text(nm, margin, yDecl); yDecl += 19; }
                    pdf.text('Ort, Datum, Unterschrift', margin, yDecl); yDecl += 12;
                });
            } else {
                for (let i = 0; i < 3; i++) {
                    pdf.text('Name', margin, yDecl); yDecl += 19;
                    pdf.text('Ort, Datum, Unterschrift', margin, yDecl); yDecl += 12;
                }
            }

            const total = pdf.getNumberOfPages();
            const footerTitle = htmlToText(meta.title) || 'Storyboard';
            const footerTitleTrim = footerTitle.length > 80 ? footerTitle.slice(0, 77) + '…' : footerTitle;
            for (let i = 1; i <= total; i++) {
                pdf.setPage(i);
                pdf.setDrawColor(226, 232, 240); pdf.setLineWidth(0.3);
                pdf.line(margin, page.h - 10, page.w - margin, page.h - 10);
                pdf.setFont('helvetica', 'normal'); pdf.setFontSize(8.5); pdf.setTextColor(148, 163, 184);
                pdf.text(footerTitleTrim, margin, page.h - 5);
                pdf.text(`Seite ${i} / ${total}`, page.w - margin, page.h - 5, { align: 'right' });
            }

            const fileTitle = (htmlToText(meta.title) || 'Storyboard').replace(/[\\/:*?"<>|]+/g, '_');
            pdf.save(`${fileTitle}.pdf`);
        } catch (err) {
            console.error(err); alert('Export fehlgeschlagen. Details in der Konsole.');
        }
    }

    btns.forEach(btn => btn.addEventListener('click', () => generatePdf('current')));
})();

// ── Stage 12.2: Fullscreen ────────────────────────────────────────────────────
(function viewerFullscreen() {
    const btns = document.querySelectorAll('[data-action="toggle-fullscreen"]');
    const target = document.querySelector('[data-role="viewer-grid"]');
    if (!target || !btns.length) return;
    btns.forEach(b => b.addEventListener('click', async () => {
        try {
            if (document.fullscreenElement) await document.exitFullscreen();
            else await target.requestFullscreen();
        } catch { /* ignore (e.g. user gesture missing) */ }
    }));
    document.addEventListener('fullscreenchange', () => {
        const on = !!document.fullscreenElement;
        btns.forEach(b => b.setAttribute('aria-pressed', on ? 'true' : 'false'));
    });
})();

// ── Stage 12.3: Share link ────────────────────────────────────────────────────
(function viewerShare() {
    document.querySelectorAll('[data-action="copy-share"]').forEach(b =>
        b.addEventListener('click', async () => {
            const url = b.dataset.shareUrl;
            if (!url) return;
            try { await navigator.clipboard.writeText(url); flash(b, 'Kopiert ✓'); }
            catch { prompt('Link kopieren:', url); }
        }));
    function flash(el, text) {
        const orig = el.getAttribute('title') || '';
        el.setAttribute('title', text);
        el.classList.add('is-flashing');
        setTimeout(() => { el.setAttribute('title', orig); el.classList.remove('is-flashing'); }, 1200);
    }
})();

// ── Stage 12.4: Palette role mapping ─────────────────────────────────────────
(function applyRoleColors() {
    'use strict';
    const container = document.querySelector('[data-role="viewer-grid"]');
    if (!container) return;

    const raw = (container.dataset.palette || '').split(',').map(s => s.trim()).filter(Boolean);
    if (!raw.length) return; // keep defaults

    const primary = raw[0].startsWith('#') ? raw[0] : ('#' + raw[0]);

    function hexToRgb(h) {
        const x = h.replace('#', '');
        const v = x.length === 3 ? x.split('').map(c => c + c).join('') : x;
        const r = parseInt(v.slice(0,2),16), g = parseInt(v.slice(2,4),16), b = parseInt(v.slice(4,6),16);
        return [r, g, b];
    }
    function rgbToHsl(r, g, b) {
        r /= 255; g /= 255; b /= 255;
        const max = Math.max(r,g,b), min = Math.min(r,g,b);
        let h, s, l = (max + min) / 2;
        if (max === min) { h = s = 0; } else {
            const d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            switch (max) {
                case r: h = (g - b) / d + (g < b ? 6 : 0); break;
                case g: h = (b - r) / d + 2; break;
                case b: h = (r - g) / d + 4; break;
            }
            h /= 6;
        }
        return [h, s, l];
    }
    function hslToHex(h, s, l) {
        function f(n) {
            const k = (n + h * 12) % 12, a = s * Math.min(l, 1 - l);
            const c = l - a * Math.max(-1, Math.min(k - 3, Math.min(9 - k, 1)));
            return Math.round(255 * c).toString(16).padStart(2, '0');
        }
        return '#' + f(0) + f(8) + f(4);
    }
    function withL(hx, lMul) {
        const [r, g, b] = hexToRgb(hx); const [h, s, l] = rgbToHsl(r, g, b);
        const l2 = Math.max(0, Math.min(1, l * lMul));
        return hslToHex(h, s, l2);
    }

    const c500 = primary;
    const c400 = withL(c500, 1.10);
    const c600 = withL(c500, 0.90);
    const c700 = withL(c500, 0.78);

    const root = document.documentElement.style;
    root.setProperty('--role-primary-400', c400);
    root.setProperty('--role-primary-500', c500);
    root.setProperty('--role-primary-600', c600);
    root.setProperty('--role-primary-700', c700);

    const chipBg = 'color-mix(in srgb, var(--role-primary-500) 12%, #ffffff)';
    const chipBd = 'color-mix(in srgb, var(--role-primary-500) 25%, #ffffff)';
    const chipMutedBg = 'color-mix(in srgb, var(--role-primary-500) 8%, #ffffff)';
    const chipMutedBd = 'color-mix(in srgb, var(--role-primary-500) 20%, #ffffff)';
    root.setProperty('--role-chip-bg', chipBg);
    root.setProperty('--role-chip-border', chipBd);
    root.setProperty('--role-chip-muted-bg', chipMutedBg);
    root.setProperty('--role-chip-muted-border', chipMutedBd);
})();
