(function () {
    'use strict';

    const main = document.querySelector('.builder-main');
    if (!main) return;
    if (typeof window.signalR === 'undefined') return;

    const storyboardId = Number(main.dataset.storyboardId);
    if (!Number.isFinite(storyboardId)) return;

    const hubUrl = (window.APP_PATH_BASE || '') + '/hubs/storyboard';
    const connection = new window.signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    let joinedSceneId = null;

    async function joinScene(id) {
        if (!id || id === joinedSceneId) return;
        if (joinedSceneId != null) {
            try { await connection.invoke('LeaveScene', joinedSceneId); } catch {}
        }
        joinedSceneId = id;
        try { await connection.invoke('JoinScene', id); } catch {}
    }

    // Coalesce bursts of events (echoes from our own writes + rapid remote
    // edits) into a single refetch so we don't hammer the server.
    let sceneReloadTimer = null;
    function scheduleActiveSceneReload() {
        if (sceneReloadTimer) clearTimeout(sceneReloadTimer);
        sceneReloadTimer = setTimeout(() => {
            sceneReloadTimer = null;
            window.Step2?.reloadActiveScene();
        }, 250);
    }

    let structuralReloadTimer = null;
    function scheduleStructuralReload(reason) {
        if (structuralReloadTimer) return;
        window.Builder?.setChip('saving', reason || 'Mitbearbeiter-Änderung…');
        structuralReloadTimer = setTimeout(() => { location.reload(); }, 1200);
    }

    // Skip echoes of our own writes — the server tags each broadcast with the
    // originating connection id so we don't clobber the local optimistic state.
    const isEcho = (p) => p && p.origin && p.origin === connection.connectionId;

    connection.on('MarkerCreated', (p) => {
        if (isEcho(p)) return;
        if (!p || p.sceneId !== window.Step2?.getActiveSceneId()) return;
        scheduleActiveSceneReload();
    });
    connection.on('MarkerUpdated', (p) => {
        if (isEcho(p)) return;
        if (!p || p.sceneId !== window.Step2?.getActiveSceneId()) return;
        scheduleActiveSceneReload();
    });
    connection.on('MarkerDeleted', (p) => {
        if (isEcho(p)) return;
        if (!p || p.sceneId !== window.Step2?.getActiveSceneId()) return;
        scheduleActiveSceneReload();
    });
    connection.on('MarkerPatched', (p) => {
        if (isEcho(p)) return;
        if (!p || p.sceneId !== window.Step2?.getActiveSceneId()) return;
        scheduleActiveSceneReload();
    });

    connection.on('SceneCreated', (p) => {
        if (isEcho(p)) return;
        scheduleStructuralReload('Neue Szene von Mitbearbeiter…');
    });
    connection.on('SceneDeleted', (p) => {
        if (isEcho(p)) return;
        scheduleStructuralReload('Szene entfernt von Mitbearbeiter…');
    });
    connection.on('ScenesReordered', (p) => {
        if (isEcho(p)) return;
        scheduleStructuralReload('Reihenfolge geändert von Mitbearbeiter…');
    });

    // Apply a remote field update to a contenteditable element, but only when
    // the user isn't currently editing it (we must never steal the caret).
    function setFieldIfIdle(el, value) {
        if (!el) return;
        if (document.activeElement === el) return;
        const next = value == null ? '' : String(value);
        if (el.innerText === next) return;
        el.innerText = next;
    }

    function applyFieldsToSelector(selector, fields) {
        if (!fields) return;
        for (const [key, value] of Object.entries(fields)) {
            const el = document.querySelector(`${selector}[data-builder-field="${key}"]`);
            if (el) setFieldIfIdle(el, value);
        }
    }

    connection.on('SceneUpdated', (p) => {
        if (isEcho(p) || !p || !p.fields) return;
        // Update the scene name in the rail + compact rail if present.
        if (Object.prototype.hasOwnProperty.call(p.fields, 'name')) {
            const label = (p.fields.name || '').trim() || '(unbenannt)';
            document.querySelectorAll(`.scene-thumb[data-scene-id="${p.id}"] .scene-thumb-name`)
                .forEach(el => el.textContent = label);
        }
        // If this is the active scene, reload so the editable fields + markers refresh.
        if (p.id === window.Step2?.getActiveSceneId()) scheduleActiveSceneReload();
    });

    connection.on('StoryboardUpdated', (p) => {
        if (isEcho(p) || !p || !p.fields) return;
        applyFieldsToSelector('[data-builder-entity="storyboard"]', p.fields);
    });

    window.addEventListener('builder:scene-changed', (ev) => {
        const id = ev?.detail?.scene?.id;
        if (id) joinScene(id);
    });

    connection.onreconnected(async () => {
        try { await connection.invoke('JoinStoryboard', storyboardId); } catch {}
        if (joinedSceneId != null) {
            const id = joinedSceneId;
            joinedSceneId = null;
            await joinScene(id);
        }
    });

    (async function start() {
        try {
            await connection.start();
            await connection.invoke('JoinStoryboard', storyboardId);
            const active = window.Step2?.getActiveSceneId();
            if (active) await joinScene(active);
        } catch (err) {
            console.warn('SignalR connection failed', err);
        }
    })();
})();
