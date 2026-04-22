(function () {
  function onIndex() {
    const stepList = document.getElementById('stepList');
    const addBtn = document.getElementById('addStepBtn');
    const form = document.getElementById('createFlowForm');
    if (!stepList || !addBtn || !form) return;

    function renumberStepFiles() {
      Array.from(stepList.children).forEach((li, idx) => {
        const file = li.querySelector('input[type="file"].step-image');
        if (file) file.name = `stepImages[${idx}]`;
      });
    }

    function makeStep(title = '', desc = '') {
      const li = document.createElement('li');
      li.className = 'list-group-item';
      li.innerHTML = `
        <div class="d-flex align-items-start gap-2">
          <span class="drag-handle mt-1"><i class="bi bi-grip-vertical"></i></span>
          <div class="flex-grow-1">
            <input class="form-control mb-2" name="stepTitles[]" placeholder="Schritt Titel" value="${title.replace(/\"/g,'&quot;')}" />
            <textarea class="form-control richtext" rows="2" name="stepDescriptions[]" placeholder="Schritt Beschreibung (optional)">${desc}</textarea>
            <input type="file" class="form-control mt-2 step-image" name="stepImages[]" accept="image/*" />
          </div>
          <button type="button" class="btn btn-outline-danger btn-sm ms-2 remove-step"><i class="bi bi-x-lg"></i></button>
        </div>`;
      return li;
    }

    function ensureOneStep() {
      if (stepList.children.length === 0) {
        stepList.appendChild(makeStep());
      }
    }

    addBtn.addEventListener('click', function () {
      stepList.appendChild(makeStep());
      initSortable();
      renumberStepFiles();
      try { const ta = stepList.lastElementChild.querySelector('textarea.richtext'); if (window.initRichText && ta) window.initRichText(ta); } catch {}
    });

    stepList.addEventListener('click', function (e) {
      const btn = e.target.closest('.remove-step');
      if (!btn) return;
      const li = btn.closest('li');
      if (li) li.remove();
      ensureOneStep();
      renumberStepFiles();
    });

    function initSortable() {
      if (typeof $ !== 'undefined' && typeof $.fn.sortable === 'function') {
        try { $(stepList).sortable('destroy'); } catch {}
        $(stepList).sortable({ handle: '.drag-handle', axis: 'y', update: renumberStepFiles });
      }
    }

    form.addEventListener('submit', function (e) {
      // sync CKEditor data back to textareas
      try { if (window.syncRichText) window.syncRichText(); } catch {}
      const titles = Array.from(form.querySelectorAll('input[name="stepTitles[]"]'))
        .map(i => i.value.trim())
        .filter(Boolean);
      if (titles.length === 0) {
        e.preventDefault();
        if (typeof toastr !== 'undefined') {
          toastr.error('Bitte mindestens einen Schritt mit Titel hinzufügen.');
        } else if (window.SB && SB.alert) {
          SB.alert('Bitte mindestens einen Schritt mit Titel hinzufügen.', 'warning', 'Titel fehlt');
        } else {
          alert('Bitte mindestens einen Schritt mit Titel hinzufügen.');
        }
      }
    });

    ensureOneStep();
    initSortable();
    renumberStepFiles();
  }

  function onDetails() {
    const list = document.getElementById('lpSteps');
    if (!list) return;
    const slug = list.getAttribute('data-slug') || 'default';
    const progressBar = document.getElementById('progressBar');
    const progressLabel = document.getElementById('progressLabel');

    function storageKey() { return 'lp_progress_' + slug; }

    function getState() {
      try { return JSON.parse(localStorage.getItem(storageKey()) || '{}'); } catch { return {}; }
    }
    function saveState(state) { localStorage.setItem(storageKey(), JSON.stringify(state)); }

    function updateProgress() {
      const checks = list.querySelectorAll('.lp-step');
      const total = checks.length;
      const done = Array.from(checks).filter(c => c.checked).length;
      const pct = total === 0 ? 0 : Math.round((done / total) * 100);
      if (progressBar) progressBar.style.width = pct + '%';
      if (progressLabel) progressLabel.textContent = pct + '%';
    }

    // Load state
    const state = getState();
    list.querySelectorAll('.lp-step').forEach(chk => {
      const id = chk.value;
      if (state[id]) chk.checked = true;
    });
    updateProgress();

    // Change handler
    list.addEventListener('change', function (e) {
      const chk = e.target.closest('.lp-step');
      if (!chk) return;
      const s = getState();
      s[chk.value] = chk.checked;
      saveState(s);
      updateProgress();
    });

    // Copy share
    const copyBtn = document.getElementById('copyShare');
    const shareUrl = document.getElementById('shareUrl');
    if (copyBtn && shareUrl) {
      copyBtn.addEventListener('click', async function () {
        try {
          await navigator.clipboard.writeText(shareUrl.value);
          if (typeof toastr !== 'undefined') toastr.success('Link kopiert!');
        } catch {
          shareUrl.select();
          document.execCommand('copy');
          if (typeof toastr !== 'undefined') toastr.success('Link kopiert!');
        }
      });
    }
  }

  function onEdit() {
    const list = document.getElementById('editStepList');
    const addBtn = document.getElementById('addEditStepBtn');
    const form = document.getElementById('editFlowForm');
    if (!list || !form) return;

    function updateOrders() {
      Array.from(list.children).forEach((li, idx) => {
        const orderInput = li.querySelector('input.step-order');
        if (orderInput) orderInput.value = idx + 1;
      });
    }

    function renumberEditStepFiles() {
      Array.from(list.children).forEach((li, idx) => {
        const file = li.querySelector('input[type="file"].step-image');
        if (file) file.name = `stepImages[${idx}]`;
      });
    }

    function makeEditStep() {
      const li = document.createElement('li');
      li.className = 'list-group-item';
      li.innerHTML = `
        <div class="d-flex align-items-start gap-2">
          <span class="drag-handle mt-1"><i class="bi bi-grip-vertical"></i></span>
          <div class="flex-grow-1">
            <input type="hidden" name="stepIds[]" value="0" />
            <input type="hidden" class="step-order" name="stepOrders[]" value="0" />
            <input class="form-control mb-2" name="stepTitles[]" placeholder="Schritt Titel" />
            <textarea class="form-control richtext" rows="2" name="stepDescriptions[]" placeholder="Schritt Beschreibung (optional)"></textarea>
            <input type="file" class="form-control mt-2 step-image" name="stepImages[]" accept="image/*" />
          </div>
          <button type="button" class="btn btn-outline-danger btn-sm ms-2 remove-step"><i class="bi bi-x-lg"></i></button>
        </div>`;
      return li;
    }

    if (addBtn) {
      addBtn.addEventListener('click', function () {
        list.appendChild(makeEditStep());
        initSortable();
        updateOrders();
        renumberEditStepFiles();
        try { const ta = list.lastElementChild.querySelector('textarea.richtext'); if (window.initRichText && ta) window.initRichText(ta); } catch {}
      });
    }

    list.addEventListener('click', function (e) {
      const btn = e.target.closest('.remove-step');
      if (!btn) return;
      const li = btn.closest('li');
      if (li) li.remove();
      updateOrders();
      renumberEditStepFiles();
    });

    function initSortable() {
      if (typeof $ !== 'undefined' && typeof $.fn.sortable === 'function') {
        try { $(list).sortable('destroy'); } catch {}
        $(list).sortable({
          handle: '.drag-handle',
          axis: 'y',
          update: function(){ updateOrders(); renumberEditStepFiles(); }
        });
      }
    }

    form.addEventListener('submit', function () {
      try { if (window.syncRichText) window.syncRichText(); } catch {}
      updateOrders();
      renumberEditStepFiles();
    });

    initSortable();
    updateOrders();
    renumberEditStepFiles();
  }

  document.addEventListener('DOMContentLoaded', function () {
    onIndex();
    onDetails();
    onEdit();
  });
})();
