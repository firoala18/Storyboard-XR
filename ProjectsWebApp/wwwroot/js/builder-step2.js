(function () {
    'use strict';

    const main = document.querySelector('.builder-main');
    if (!main) return;
    const storyboardId = Number(main.dataset.storyboardId);

    const rail        = document.querySelector('.scene-rail');
    const canvas      = document.querySelector('[data-role="marker-canvas"]');
    const sceneName   = document.querySelector('.step2-scene-name');
    const activeNumEl = document.querySelector('.active-scene-num');
    const panel       = document.querySelector('.step2-marker-panel');
    const hint        = document.querySelector('.step2-canvas-hint');

    if (!canvas) return;

    let activeScene = null;
    let activeMarkerId = null;

    function setSceneNameEnabled(enabled) {
        if (!sceneName) return;
        if (enabled) {
            sceneName.classList.remove('is-disabled');
            const multi = sceneName.dataset.builderMultiline === 'true';
            sceneName.setAttribute('contenteditable', multi ? 'true' : 'plaintext-only');
        } else {
            sceneName.classList.add('is-disabled');
            sceneName.setAttribute('contenteditable', 'false');
            sceneName.textContent = '';
            sceneName.dataset.builderId = '';
        }
    }
    setSceneNameEnabled(false);

    // Mirror the scene name edits into the rail thumbnail name in real time.
    if (sceneName) {
        sceneName.addEventListener('input', () => {
            if (!activeScene) return;
            const id = activeScene.id;
            const text = sceneName.innerText.trim();
            const label = text || '(unbenannt)';
            document.querySelectorAll(`.scene-thumb[data-scene-id="${id}"] .scene-thumb-name`)
                .forEach(el => el.textContent = label);
            document.querySelectorAll(`.scene-thumb-mini[data-scene-id="${id}"]`)
                .forEach(el => el.title = `Szene ${activeScene.number} ${text}`);
            activeScene.name = text;
        });
    }

    function csrfToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function selectSceneFromThumb(li) {
        if (!li) return;
        const id = Number(li.dataset.sceneId);
        document.querySelectorAll('.scene-thumb').forEach(x => x.setAttribute('aria-selected', String(Number(x.dataset.sceneId) === id)));
        document.querySelectorAll('.scene-thumb-mini').forEach(x => x.setAttribute('aria-selected', String(Number(x.dataset.sceneId) === id)));
        loadScene(id);
    }

    async function loadScene(sceneId) {
        try {
            const res = await fetch(window.apiUrl(`/Scenes/${sceneId}?format=json`));
            if (!res.ok) { console.warn('loadScene failed', sceneId, res.status); return; }
            activeScene = await res.json();
            render();
        } catch (err) {
            console.error('loadScene error', err);
        }
    }

    // Persistent inner wrapper whose size exactly matches the image's displayed rect,
    // so marker percentage coords stay anchored to the image regardless of canvas size.
    function getInner() {
        let inner = canvas.querySelector('.step2-canvas-inner');
        if (!inner) {
            inner = document.createElement('div');
            inner.className = 'step2-canvas-inner';
            canvas.appendChild(inner);
        }
        return inner;
    }

    function render() {
        if (!activeScene) { setSceneNameEnabled(false); return; }
        setSceneNameEnabled(true);
        if (activeNumEl) activeNumEl.textContent = activeScene.number;
        if (sceneName) {
            sceneName.textContent = activeScene.name ?? '';
            sceneName.dataset.builderId = String(activeScene.id);
        }

        const inner = getInner();
        inner.querySelectorAll('.scene-img, .marker-dot').forEach(n => n.remove());
        const imgBtn = document.querySelector('[data-role="scene-image-btn"]');

        if (activeScene.imagePath) {
            const img = document.createElement('img');
            img.className = 'scene-img';
            img.src = activeScene.imagePath;
            img.alt = activeScene.name ?? '';
            inner.hidden = false;
            inner.prepend(img);
            if (hint) hint.style.display = 'none';
            if (imgBtn) imgBtn.textContent = '↑ Bild ersetzen';
        } else {
            inner.hidden = true;
            if (hint) {
                hint.style.display = '';
                hint.textContent = 'Noch kein Bild · klicke auf „Bild hochladen" oben rechts';
            }
            if (imgBtn) imgBtn.textContent = '↑ Bild hochladen';
        }
        (activeScene.markers || []).forEach(renderMarker);
        panel?.classList.remove('has-marker');
        activeMarkerId = null;
        renderMarkerMiniList();
        window.dispatchEvent(new CustomEvent('builder:scene-changed', { detail: { scene: activeScene } }));
        const first = (activeScene.markers || [])[0];
        if (first) selectMarker(first.id);
    }

    function renderMarkerMiniList() {
        const list = document.querySelector('[data-role="marker-mini-list"]');
        if (!list) return;
        list.innerHTML = '';
        const markers = activeScene?.markers || [];
        if (markers.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'marker-rail-empty';
            empty.textContent = 'Keine Marker';
            list.appendChild(empty);
            return;
        }
        markers.forEach(m => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'marker-mini';
            btn.textContent = m.number;
            btn.style.background = m.colorHex || '#ef4444';
            btn.dataset.markerId = String(m.id);
            btn.title = `Marker ${m.number}`;
            if (m.id === activeMarkerId) btn.setAttribute('aria-selected', 'true');
            btn.addEventListener('click', () => {
                selectMarker(m.id);
                document.querySelectorAll('.marker-mini').forEach(x =>
                    x.setAttribute('aria-selected', x === btn ? 'true' : 'false'));
            });
            list.appendChild(btn);
        });
    }

    let suppressCanvasClick = false;

    function renderMarker(m) {
        const dot = document.createElement('div');
        dot.className = 'marker-dot';
        dot.style.left = (m.x * 100) + '%';
        dot.style.top  = (m.y * 100) + '%';
        dot.style.background = m.colorHex || '#ef4444';
        dot.textContent = m.number;
        dot.dataset.markerId = String(m.id);
        attachDrag(dot, m);
        getInner().appendChild(dot);
    }

    function attachDrag(dot, m) {
        let dragging = false;
        let moved = false;
        let startX = 0, startY = 0;
        const DRAG_THRESHOLD = 3; // px before we treat it as a drag

        dot.addEventListener('pointerdown', (ev) => {
            if (ev.button !== 0) return;
            ev.preventDefault();
            ev.stopPropagation();
            dragging = true;
            moved = false;
            startX = ev.clientX;
            startY = ev.clientY;
            dot.setPointerCapture(ev.pointerId);
            dot.classList.add('is-dragging');
        });

        dot.addEventListener('pointermove', (ev) => {
            if (!dragging) return;
            if (!moved) {
                if (Math.abs(ev.clientX - startX) < DRAG_THRESHOLD &&
                    Math.abs(ev.clientY - startY) < DRAG_THRESHOLD) return;
                moved = true;
            }
            const rect = getInner().getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) return;
            const x = Math.min(1, Math.max(0, (ev.clientX - rect.left) / rect.width));
            const y = Math.min(1, Math.max(0, (ev.clientY - rect.top)  / rect.height));
            dot.style.left = (x * 100) + '%';
            dot.style.top  = (y * 100) + '%';
            m.x = x; m.y = y;
        });

        function finish(ev) {
            if (!dragging) return;
            dragging = false;
            dot.classList.remove('is-dragging');
            try { dot.releasePointerCapture(ev.pointerId); } catch {}
            if (moved) {
                suppressCanvasClick = true;
                setTimeout(() => { suppressCanvasClick = false; }, 0);
                window.Builder?.patchField('marker', m.id, 'x', m.x, { immediate: true });
                window.Builder?.patchField('marker', m.id, 'y', m.y, { immediate: true });
            } else {
                // plain click: select
                selectMarker(m.id);
            }
        }
        dot.addEventListener('pointerup', finish);
        dot.addEventListener('pointercancel', finish);
    }

    canvas.addEventListener('click', async (ev) => {
        if (!activeScene) return;
        if (suppressCanvasClick) return;
        if (ev.target.closest('.marker-dot')) return;
        if (!activeScene.imagePath) return;

        const inner = getInner();
        const rect = inner.getBoundingClientRect();
        // Only create if the click landed inside the image rect (not in the letterbox)
        if (ev.clientX < rect.left || ev.clientX > rect.right ||
            ev.clientY < rect.top  || ev.clientY > rect.bottom) return;
        const x = (ev.clientX - rect.left) / rect.width;
        const y = (ev.clientY - rect.top)  / rect.height;

        try {
            const res = await fetch(window.apiUrl(`/api/scenes/${activeScene.id}/markers`), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ x, y })
            });
            if (!res.ok) { console.warn('marker create failed', res.status); return; }
            const created = await res.json();
            const marker = {
                id: created.id ?? created.Id,
                x: created.x ?? created.X,
                y: created.y ?? created.Y,
                number: created.number ?? created.Number,
                colorHex: created.colorHex ?? created.ColorHex,
                description: created.description ?? created.Description ?? '',
                ziel: created.ziel ?? created.Ziel ?? '',
                datenablage: created.datenablage ?? created.Datenablage ?? '',
                quellen: created.quellen ?? created.Quellen ?? '',
                promptIdee: created.promptIdee ?? created.PromptIdee ?? '',
                reflexion: created.reflexion ?? created.Reflexion ?? '',
                model: created.model ?? created.Model ?? '',
                taxonomie: created.taxonomie ?? created.Taxonomie ?? null
            };
            activeScene.markers = activeScene.markers || [];
            activeScene.markers.push(marker);
            renderMarker(marker);
            renderMarkerMiniList();
            selectMarker(marker.id);
        } catch (err) {
            console.error('marker create error', err);
        }
    });

    function selectMarker(id) {
        activeMarkerId = id;
        document.querySelectorAll('.marker-dot').forEach(d =>
            d.setAttribute('aria-selected', String(Number(d.dataset.markerId) === id)));
        window.dispatchEvent(new CustomEvent('builder:marker-selected', { detail: { scene: activeScene, markerId: id } }));
        panel?.classList.add('has-marker');
    }

    window.addEventListener('builder:markers-renumbered', (ev) => {
        const { sceneId, order } = ev.detail || {};
        if (!activeScene || activeScene.id !== sceneId) return;
        const numberById = new Map((order || []).map(o => [o.id, o.number]));
        activeScene.markers = (activeScene.markers || [])
            .filter(m => numberById.has(m.id))
            .map(m => ({ ...m, number: numberById.get(m.id) }))
            .sort((a, b) => a.number - b.number);
        document.querySelectorAll('.marker-dot').forEach(dot => {
            const id = Number(dot.dataset.markerId);
            if (numberById.has(id)) dot.textContent = String(numberById.get(id));
        });
        renderMarkerMiniList();
    });

    document.querySelector('[data-action="add-scene"]')?.addEventListener('click', async () => {
        const fd = new FormData();
        fd.append('storyboardId', String(storyboardId));
        window.Builder?.setChip('saving', 'Szene wird erstellt…');
        const res = await fetch(window.apiUrl('/Scenes/AddEmpty'), { method: 'POST', body: fd });
        if (!res.ok) { window.Builder?.setChip('error', '⚠ Szene konnte nicht erstellt werden'); return; }
        const sc = await res.json();
        appendSceneToRail(sc);
        activeScene = sc;
        render();
        window.Builder?.setChip('saved', 'Gespeichert vor 1 Sek.');
    });

    function appendSceneToRail(sc) {
        const li = document.createElement('li');
        li.className = 'scene-thumb';
        li.dataset.sceneId = String(sc.id);
        li.dataset.sceneNumber = String(sc.number);
        li.innerHTML = `
            <span class="scene-num">${sc.number}</span>
            <div class="scene-thumb-img">${sc.imagePath ? `<img src="${sc.imagePath}" alt="">` : ''}</div>
            <span class="scene-thumb-name">${sc.name || '(unbenannt)'}</span>`;
        li.addEventListener('click', () => selectSceneFromThumb(li));
        rail.appendChild(li);

        // Mirror into the compact rail
        const compact = document.querySelector('.scene-rail-compact');
        if (compact) {
            const mini = document.createElement('li');
            mini.className = 'scene-thumb-mini';
            mini.dataset.sceneId = String(sc.id);
            mini.dataset.sceneNumber = String(sc.number);
            mini.title = `Szene ${sc.number}${sc.name ? ' · ' + sc.name : ''}`;
            mini.innerHTML = `
                <span class="scene-thumb-mini-num">${sc.number}</span>
                ${sc.imagePath ? `<img src="${sc.imagePath}" alt="">` : ''}`;
            mini.addEventListener('click', () => {
                selectSceneFromThumb(li);
            });
            compact.appendChild(mini);
        }

        document.querySelectorAll('.scene-thumb').forEach(x => x.setAttribute('aria-selected', 'false'));
        document.querySelectorAll('.scene-thumb-mini').forEach(x => x.setAttribute('aria-selected', 'false'));
        li.setAttribute('aria-selected', 'true');
        const miniMatch = document.querySelector(`.scene-thumb-mini[data-scene-id="${sc.id}"]`);
        if (miniMatch) miniMatch.setAttribute('aria-selected', 'true');
        const count = document.querySelector('.scene-count');
        if (count) count.textContent = String(document.querySelectorAll('.scene-thumb').length);
    }

    document.querySelector('[data-action="replace-scene-image"]')?.addEventListener('click', () => {
        if (!activeScene) return;
        document.querySelector('[data-input="replace-scene-image"]').click();
    });

    document.querySelector('[data-input="replace-scene-image"]')?.addEventListener('change', async (ev) => {
        const file = ev.target.files?.[0]; if (!file || !activeScene) return;
        const fd = new FormData();
        fd.append('image', file);
        window.Builder?.setChip('saving', 'Bild wird hochgeladen…');
        const res = await fetch(window.apiUrl(`/Scenes/UploadImage/${activeScene.id}`), { method: 'POST', body: fd });
        ev.target.value = '';
        if (!res.ok) { window.Builder?.setChip('error', '⚠ Bild-Upload fehlgeschlagen'); return; }
        const data = await res.json();
        activeScene.imagePath = data.imagePath;
        render();
        const thumb = document.querySelector(`.scene-thumb[data-scene-id="${activeScene.id}"] .scene-thumb-img`);
        if (thumb) thumb.innerHTML = `<img src="${data.imagePath}" alt="">`;
        window.Builder?.setChip('saved', 'Gespeichert vor 1 Sek.');
    });

    document.querySelector('[data-action="delete-scene"]')?.addEventListener('click', async () => {
        if (!activeScene) return;
        if (!confirm('Diese Szene wirklich löschen?')) return;
        const fd = new URLSearchParams();
        fd.append('id', String(activeScene.id));
        fd.append('storyboardId', String(storyboardId));
        fd.append('__RequestVerificationToken', csrfToken());
        const res = await fetch(window.apiUrl('/User/Scenes/Delete'), {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: fd
        });
        if (res.ok) location.reload();
    });

    document.querySelector('[data-action="generate-ai-scene"]')?.addEventListener('click', () => {
        const p = prompt('Bitte gib eine Szenenbeschreibung ein:');
        if (!p) return;
        const fd = new URLSearchParams();
        fd.append('storyboardId', String(storyboardId));
        fd.append('prompt', p);
        fd.append('aspect', '1:1');
        fd.append('quality', 'low');
        fd.append('__RequestVerificationToken', csrfToken());
        window.Builder?.setChip('saving', 'Generiere Szene…');
        fetch(window.apiUrl('/User/Scenes/GenerateAiScene'), {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: fd
        }).then(r => {
            if (r.ok) location.reload();
            else window.Builder?.setChip('error', '⚠ AI-Generierung fehlgeschlagen');
        });
    });

    document.querySelectorAll('.scene-thumb').forEach(li =>
        li.addEventListener('click', () => selectSceneFromThumb(li)));

    // Mini-thumb clicks select the same scene (and auto-expand if collapsed).
    function wireSceneMini(li) {
        li.addEventListener('click', () => {
            const id = Number(li.dataset.sceneId);
            const full = document.querySelector(`.scene-thumb[data-scene-id="${id}"]`);
            if (full) selectSceneFromThumb(full);
            document.querySelectorAll('.scene-thumb-mini').forEach(m =>
                m.setAttribute('aria-selected', m === li ? 'true' : 'false'));
        });
    }
    document.querySelectorAll('.scene-thumb-mini').forEach(wireSceneMini);

    window.addEventListener('builder:step-changed', (ev) => {
        if (ev.detail.step !== 2) return;
        if (activeScene) return;
        const first = document.querySelector('.scene-thumb');
        if (first) selectSceneFromThumb(first);
    });

    // ── Zoom & pan ───────────────────────────────────────────────────
    (function wireZoomPan() {
        if (!canvas) return;
        const ZOOM_MIN = 0.25, ZOOM_MAX = 6, ZOOM_STEP = 1.2;
        const levelEl = document.querySelector('[data-role="zoom-level"]');
        let zoom = 1, panX = 0, panY = 0;

        function apply() {
            canvas.style.setProperty('--zoom', zoom);
            canvas.style.setProperty('--pan-x', panX + 'px');
            canvas.style.setProperty('--pan-y', panY + 'px');
            if (levelEl) levelEl.textContent = Math.round(zoom * 100) + '%';
        }
        function reset() { zoom = 1; panX = 0; panY = 0; apply(); }

        function setZoom(next, centerClient) {
            next = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, next));
            const innerEl = canvas.querySelector('.step2-canvas-inner');
            if (!innerEl || next === zoom) { zoom = next; apply(); return; }
            // Keep the point under the cursor / canvas-center stationary while zooming.
            const rect = canvas.getBoundingClientRect();
            const cx = (centerClient?.x ?? (rect.left + rect.width / 2)) - rect.left;
            const cy = (centerClient?.y ?? (rect.top  + rect.height / 2)) - rect.top;
            // Current content coordinate under the focal point.
            const contentX = (cx - panX) / zoom;
            const contentY = (cy - panY) / zoom;
            zoom = next;
            panX = cx - contentX * zoom;
            panY = cy - contentY * zoom;
            apply();
        }

        document.querySelector('[data-action="zoom-in"]') ?.addEventListener('click', () => setZoom(zoom * ZOOM_STEP));
        document.querySelector('[data-action="zoom-out"]')?.addEventListener('click', () => setZoom(zoom / ZOOM_STEP));
        document.querySelector('[data-action="zoom-reset"]')?.addEventListener('click', reset);

        canvas.addEventListener('wheel', (ev) => {
            // Shift/Ctrl/meta or just plain wheel in the canvas = zoom; always preventDefault
            // so the page itself doesn't scroll.
            ev.preventDefault();
            const delta = ev.deltaY < 0 ? ZOOM_STEP : 1 / ZOOM_STEP;
            setZoom(zoom * delta, { x: ev.clientX, y: ev.clientY });
        }, { passive: false });

        // Pan: Shift+drag, middle-mouse drag, or just drag on the canvas background
        // (i.e. not on the image and not on a marker) so marker placement / dragging
        // stay unaffected.
        let panning = null;
        canvas.addEventListener('pointerdown', (ev) => {
            const wantsPan = ev.shiftKey || ev.button === 1;
            const onMarker = !!ev.target.closest('.marker-dot');
            if (!wantsPan || onMarker) return;
            ev.preventDefault();
            panning = { startX: ev.clientX, startY: ev.clientY, panX, panY, pointerId: ev.pointerId };
            canvas.setPointerCapture(ev.pointerId);
            canvas.classList.add('is-panning');
            suppressCanvasClick = true;
        });
        canvas.addEventListener('pointermove', (ev) => {
            if (!panning) return;
            panX = panning.panX + (ev.clientX - panning.startX);
            panY = panning.panY + (ev.clientY - panning.startY);
            apply();
        });
        function endPan(ev) {
            if (!panning) return;
            try { canvas.releasePointerCapture(panning.pointerId); } catch {}
            canvas.classList.remove('is-panning');
            panning = null;
            setTimeout(() => { suppressCanvasClick = false; }, 0);
        }
        canvas.addEventListener('pointerup', endPan);
        canvas.addEventListener('pointercancel', endPan);

        // Reset zoom/pan whenever the active scene changes so markers aren't
        // placed against stale transforms.
        const origSelect = selectSceneFromThumb;
        // (wired below via existing path; we hook into render indirectly by resetting
        //  on scene-thumb click via the grid listeners already in place).
        window.addEventListener('builder:step-changed', (ev) => { if (ev.detail.step === 2) reset(); });
        document.querySelectorAll('.scene-thumb').forEach(li => li.addEventListener('click', reset));
        document.querySelector('.scene-rail')?.addEventListener('click', (ev) => {
            if (ev.target.closest('.scene-thumb')) reset();
        });

        apply();
    })();

    // ── Collapsible + resizable sidebars ─────────────────────────────
    (function wireAsides() {
        const grid = document.querySelector('[data-role="step2-grid"]');
        if (!grid) return;

        const STORE = 'builder:step2:aside';

        function load() {
            try { return JSON.parse(localStorage.getItem(STORE)) || {}; } catch { return {}; }
        }
        function save(state) { localStorage.setItem(STORE, JSON.stringify(state)); }

        const panelEl = document.querySelector('.step2-marker-panel');

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

        // Emergency reset: ?reset-builder-ui=1 wipes the persisted layout state.
        if (new URLSearchParams(location.search).get('reset-builder-ui') === '1') {
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
        const dockDropdown = dockMenu?.querySelector('.step2-dock-dropdown');
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
        const panelAside = document.querySelector('.step2-marker-panel');
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

        // Height +/- buttons for the "below" marker-panel mode (iPad/mobile friendly).
        const HEIGHT_STEP = 80;
        function adjustBelowHeight(delta) {
            if (!panelEl) return;
            if (normalizeMode(state) !== 'below') return;
            const min = 200;
            const max = Math.round(window.innerHeight * 0.85);
            const current = panelEl.getBoundingClientRect().height;
            const next = Math.max(min, Math.min(max, Math.round(current + delta)));
            panelEl.style.height = next + 'px';
            grid.style.setProperty('--panel-below-h', next + 'px');
            state.belowH = next;
            save(state);
        }
        document.querySelectorAll('[data-action="panel-height-inc"]').forEach(b =>
            b.addEventListener('click', () => adjustBelowHeight(+HEIGHT_STEP)));
        document.querySelectorAll('[data-action="panel-height-dec"]').forEach(b =>
            b.addEventListener('click', () => adjustBelowHeight(-HEIGHT_STEP)));
    })();
})();
