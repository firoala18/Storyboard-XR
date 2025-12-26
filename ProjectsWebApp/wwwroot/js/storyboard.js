(function () {
    const api = {
        list: sbId =>
            fetch(`/api/markers/${sbId}`).then(r => r.json()),
        create: (sbId, x, y) =>
            fetch(`/api/markers`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ storyboardId: sbId, x, y })
            }).then(r => r.json()),
        update: (id, patch) =>
            fetch(`/api/markers/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(patch)
            }).then(r => r.json()),
        remove: id =>
            fetch(`/api/markers/${id}`, { method: 'DELETE' })
    };

    function el(tag, cls) { const e = document.createElement(tag); if (cls) e.className = cls; return e; }
    function px(n) { return `${n}px`; }

    function makeDraggable(dot, onMoveEnd) {
        let dragging = false; let start = { x: 0, y: 0 };
        dot.addEventListener('pointerdown', (e) => {
            dragging = true;
            e.stopPropagation();               // don’t add a marker when grabbing a dot
            dot.setPointerCapture(e.pointerId);
            start.x = e.clientX; start.y = e.clientY;
            dot.classList.add('selected');
        });
        dot.addEventListener('pointerup', (e) => {
            if (!dragging) return;
            dragging = false;
            dot.releasePointerCapture(e.pointerId);
            dot.classList.remove('selected');
            onMoveEnd();
        });
        dot.addEventListener('pointermove', (e) => {
            if (!dragging) return;
            const dx = e.clientX - start.x; const dy = e.clientY - start.y;
            start.x = e.clientX; start.y = e.clientY;
            const left = (parseFloat(dot.style.left) || 0) + dx;
            const top = (parseFloat(dot.style.top) || 0) + dy;
            dot.style.left = px(left); dot.style.top = px(top);
        });
    }

    function round2(n) { return Math.round(n * 100) / 100; }

    const State = { markers: [], sbId: null, els: {} };

    async function renderAll() {
        const { img, overlay, list } = State.els;
        overlay.innerHTML = ''; list.innerHTML = '';

        const w = img.clientWidth, h = img.clientHeight;

        State.markers.forEach(m => {
            // overlay dot
            const dot = el('button', 'marker-dot');
            dot.style.left = px(m.X * w);
            dot.style.top = px(m.Y * h);
            dot.title = m.Description || `Marker ${m.Id}`;
            overlay.appendChild(dot);

            makeDraggable(dot, async () => {
                // compute new pct from pixels
                const nx = (parseFloat(dot.style.left) / w);
                const ny = (parseFloat(dot.style.top) / h);
                m.X = Math.min(1, Math.max(0, nx));
                m.Y = Math.min(1, Math.max(0, ny));
                await api.update(m.Id, { x: m.X, y: m.Y });
            });

            // description item
            const li = el('li', 'desc-item');
            const ta = el('textarea'); ta.value = m.Description || '';
            const coords = el('div', 'coords');
            coords.textContent = `x: ${round2(m.X * 100)}% • y: ${round2(m.Y * 100)}%`;
            const del = el('button', 'delete'); del.textContent = 'Delete';

            ta.addEventListener('input', debounce(async () => {
                m.Description = ta.value;
                await api.update(m.Id, { description: m.Description });
                dot.title = m.Description;
            }, 300));

            del.addEventListener('click', async () => {
                let ok = true;
                if (window.SB && SB.confirm) {
                    ok = await SB.confirm('Diesen Marker wirklich löschen?', {
                        title: 'Marker löschen?',
                        icon: 'warning',
                        confirmButtonText: 'Ja, löschen',
                        cancelButtonText: 'Abbrechen'
                    });
                } else {
                    ok = window.confirm('Delete this marker?');
                }
                if (!ok) return;
                await api.remove(m.Id);
                State.markers = State.markers.filter(x => x.Id !== m.Id);
                renderAll();
            });

            li.appendChild(ta); li.appendChild(del); li.appendChild(coords);
            list.appendChild(li);

            li.addEventListener('mouseenter', () => dot.classList.add('selected'));
            li.addEventListener('mouseleave', () => dot.classList.remove('selected'));
            dot.addEventListener('mouseenter', () => li.classList.add('hover'));
            dot.addEventListener('mouseleave', () => li.classList.remove('hover'));
        });
    }

    function debounce(fn, ms) { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); } }

    async function onImageClick(e) {
        // Don’t add a marker if you clicked/dragged an existing one
        if (e.target && e.target.classList.contains('marker-dot')) return;

        const { img } = State.els;
        const rect = img.getBoundingClientRect();
        const x = (e.clientX - rect.left) / img.clientWidth;
        const y = (e.clientY - rect.top) / img.clientHeight;
        const created = await api.create(State.sbId, x, y);
        State.markers.push(created);
        renderAll();
    }

    async function init(cfg) {
        const img = document.getElementById(cfg.imgId);
        const overlay = document.getElementById(cfg.overlayId);
        const board = document.getElementById(cfg.boardId);
        const list = document.getElementById(cfg.listId);
        const sbId = parseInt(board.dataset.storyboardId);

        State.els = { img, overlay, list };
        State.sbId = sbId;

        function afterReady() {
            api.list(sbId).then(ms => { State.markers = ms; renderAll(); });
        }
        if (img.complete) afterReady(); else img.addEventListener('load', afterReady, { once: true });

        // Add marker on click -> attach to overlay (it sits on top of the image)
        overlay.addEventListener('click', onImageClick);

        // Reposition markers on resize
        window.addEventListener('resize', debounce(renderAll, 100));
    }

    window.Storyboard = { init };
})();
