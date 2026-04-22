(function () {
    'use strict';

    // Enhances any <select data-ui-select> into a fully styled combobox.
    // The original <select> stays in the DOM (hidden) and remains the source
    // of truth — we forward change events to it, so existing code that listens
    // for `change` on the select still works unchanged.

    const OPEN = 'is-open';
    const SELECTED = 'is-selected';
    const ACTIVE = 'is-active';

    function build(select) {
        if (select.dataset.uiSelectInitialized === '1') return;
        select.dataset.uiSelectInitialized = '1';

        const wrap = document.createElement('div');
        wrap.className = 'ui-select';
        select.parentNode.insertBefore(wrap, select);
        wrap.appendChild(select);
        select.classList.add('ui-select-native');

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'ui-select-btn';
        btn.setAttribute('aria-haspopup', 'listbox');
        btn.setAttribute('aria-expanded', 'false');
        btn.innerHTML = '<span class="ui-select-value"></span><span class="ui-select-chev" aria-hidden="true"></span>';

        const list = document.createElement('ul');
        list.className = 'ui-select-list';
        list.setAttribute('role', 'listbox');
        list.hidden = true;

        wrap.appendChild(btn);
        wrap.appendChild(list);

        const valueSpan = btn.querySelector('.ui-select-value');
        let activeIdx = -1;

        function syncLabel() {
            const opt = select.options[select.selectedIndex];
            if (!opt || opt.value === '') {
                valueSpan.textContent = opt?.textContent || '— bitte wählen —';
                valueSpan.classList.add('is-placeholder');
            } else {
                valueSpan.textContent = opt.textContent;
                valueSpan.classList.remove('is-placeholder');
            }
        }

        function buildList() {
            list.innerHTML = '';
            Array.from(select.options).forEach((opt, i) => {
                const li = document.createElement('li');
                li.className = 'ui-select-option';
                li.setAttribute('role', 'option');
                li.dataset.value = opt.value;
                li.dataset.idx = String(i);
                li.innerHTML = '<span class="ui-select-option-tick" aria-hidden="true"></span><span class="ui-select-option-text"></span>';
                li.querySelector('.ui-select-option-text').textContent = opt.textContent;
                if (opt.disabled) li.classList.add('is-disabled');
                if (opt.selected) li.classList.add(SELECTED);
                if (opt.value === '') li.classList.add('is-placeholder-option');
                list.appendChild(li);
            });
            syncLabel();
        }

        function open() {
            if (select.disabled) return;
            buildList();
            list.hidden = false;
            wrap.classList.add(OPEN);
            btn.setAttribute('aria-expanded', 'true');
            // Position list into viewport: open upward if not enough space below.
            list.classList.remove('is-upward');
            const r = btn.getBoundingClientRect();
            const spaceBelow = window.innerHeight - r.bottom;
            const desired = Math.min(320, list.scrollHeight + 8);
            if (spaceBelow < desired && r.top > desired) list.classList.add('is-upward');
            // Focus the current selection for keyboard nav.
            activeIdx = select.selectedIndex;
            highlight(activeIdx);
            document.addEventListener('mousedown', onOutside, true);
            document.addEventListener('keydown', onKey);
        }

        function close() {
            list.hidden = true;
            wrap.classList.remove(OPEN);
            btn.setAttribute('aria-expanded', 'false');
            document.removeEventListener('mousedown', onOutside, true);
            document.removeEventListener('keydown', onKey);
        }

        function highlight(idx) {
            const items = list.querySelectorAll('.ui-select-option');
            items.forEach(x => x.classList.remove(ACTIVE));
            if (idx < 0 || idx >= items.length) return;
            items[idx].classList.add(ACTIVE);
            items[idx].scrollIntoView({ block: 'nearest' });
        }

        function pick(idx) {
            if (idx < 0 || idx >= select.options.length) return;
            const opt = select.options[idx];
            if (opt.disabled) return;
            if (select.selectedIndex !== idx) {
                select.selectedIndex = idx;
                select.dispatchEvent(new Event('change', { bubbles: true }));
            }
            syncLabel();
            close();
            btn.focus();
        }

        function onOutside(ev) {
            if (!wrap.contains(ev.target)) close();
        }

        function onKey(ev) {
            const items = list.querySelectorAll('.ui-select-option');
            if (ev.key === 'Escape') { ev.preventDefault(); close(); btn.focus(); return; }
            if (ev.key === 'ArrowDown') { ev.preventDefault(); activeIdx = Math.min(items.length - 1, Math.max(0, activeIdx + 1)); highlight(activeIdx); return; }
            if (ev.key === 'ArrowUp')   { ev.preventDefault(); activeIdx = Math.max(0, activeIdx - 1); highlight(activeIdx); return; }
            if (ev.key === 'Enter' || ev.key === ' ') { ev.preventDefault(); pick(activeIdx); return; }
            if (ev.key === 'Home')  { ev.preventDefault(); activeIdx = 0; highlight(0); return; }
            if (ev.key === 'End')   { ev.preventDefault(); activeIdx = items.length - 1; highlight(activeIdx); return; }
            // Type-ahead: jump to first option starting with the pressed character.
            if (ev.key.length === 1) {
                const ch = ev.key.toLowerCase();
                const start = activeIdx + 1;
                for (let i = 0; i < items.length; i++) {
                    const idx = (start + i) % items.length;
                    const txt = (items[idx].textContent || '').trim().toLowerCase();
                    if (txt.startsWith(ch)) { activeIdx = idx; highlight(idx); break; }
                }
            }
        }

        btn.addEventListener('click', () => list.hidden ? open() : close());
        btn.addEventListener('keydown', (ev) => {
            if (ev.key === 'ArrowDown' || ev.key === 'Enter' || ev.key === ' ') {
                ev.preventDefault();
                open();
            }
        });
        list.addEventListener('click', (ev) => {
            const li = ev.target.closest('.ui-select-option');
            if (!li) return;
            pick(Number(li.dataset.idx));
        });

        // Re-sync the visible label when outside code changes the native select.
        select.addEventListener('change', () => {
            syncLabel();
            if (!list.hidden) buildList();
        });

        syncLabel();
    }

    function scan(root) {
        (root || document).querySelectorAll('select[data-ui-select]:not([data-ui-select-initialized])')
            .forEach(build);
    }

    // Auto-init existing selects and watch for any added later (partials etc).
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => scan());
    } else {
        scan();
    }
    new MutationObserver((muts) => {
        for (const m of muts) {
            m.addedNodes.forEach(n => {
                if (n.nodeType !== 1) return;
                if (n.matches && n.matches('select[data-ui-select]')) build(n);
                if (n.querySelectorAll) scan(n);
            });
        }
    }).observe(document.body, { childList: true, subtree: true });

    window.UiSelect = { scan, build };
})();
