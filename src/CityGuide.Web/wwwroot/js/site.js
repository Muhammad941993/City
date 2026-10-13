// Small progressive enhancements. Every page works without this file.
(function () {
  'use strict';

  // Share: native share sheet on phones, otherwise copy the link.
  document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-share]');
    if (!btn) return;
    var data = { title: btn.dataset.title || document.title, url: location.href.split('?')[0] };
    if (navigator.share) {
      navigator.share(data).catch(function () {});
    } else if (navigator.clipboard) {
      navigator.clipboard.writeText(data.url).then(function () {
        var old = btn.innerHTML;
        btn.textContent = 'اتنسخ اللينك ✓';
        setTimeout(function () { btn.innerHTML = old; }, 2000);
      });
    } else {
      window.open('https://wa.me/?text=' + encodeURIComponent(data.title + ' ' + data.url), '_blank');
    }
  });

  // Bottom sheet for filters on mobile.
  document.addEventListener('click', function (e) {
    var open = e.target.closest('[data-sheet-open]');
    var close = e.target.closest('[data-sheet-close]');
    var id = (open || close) && (open ? open.dataset.sheetOpen : close.dataset.sheetClose);
    if (!id) return;
    var sheet = document.getElementById(id);
    if (!sheet) return;
    var isOpen = !!open;
    sheet.classList.toggle('open', isOpen);
    document.querySelectorAll('[data-sheet-open="' + id + '"]').forEach(function (b) { b.setAttribute('aria-expanded', String(isOpen)); });
    document.body.style.overflow = isOpen ? 'hidden' : '';
    if (isOpen) { var f = sheet.querySelector('select, input, button'); if (f) f.focus(); }
  });
  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;
    var sheet = document.querySelector('.filters.open');
    if (sheet) { sheet.classList.remove('open'); document.body.style.overflow = ''; }
  });

  // Image previews for file inputs.
  document.querySelectorAll('input[type=file][data-preview]').forEach(function (input) {
    input.addEventListener('change', function () {
      var box = document.getElementById(input.dataset.preview);
      if (!box) return;
      box.innerHTML = '';
      Array.prototype.slice.call(input.files, 0, 10).forEach(function (file) {
        if (!file.type || file.type.indexOf('image/') !== 0) return;
        var img = document.createElement('img');
        img.alt = '';
        img.src = URL.createObjectURL(file);
        img.onload = function () { URL.revokeObjectURL(img.src); };
        box.appendChild(img);
      });
    });
  });

  // Show rooms/bathrooms only for homes, floor/finishing for everything but land.
  function syncTypeFields(form) {
    var checked = form.querySelector('[data-property-type]:checked');
    var type = checked ? checked.value : '';
    form.querySelectorAll('[data-home-only]').forEach(function (el) { el.hidden = !(type === 'Apartment' || type === 'House' || type === ''); });
    form.querySelectorAll('[data-not-land]').forEach(function (el) { el.hidden = type === 'Land'; });
  }
  document.querySelectorAll('form').forEach(function (form) {
    if (!form.querySelector('[data-property-type]')) return;
    syncTypeFields(form);
    form.addEventListener('change', function (e) { if (e.target.matches('[data-property-type]')) syncTypeFields(form); });
  });

  // Multi-step form: one step at a time, validating the visible step before moving on.
  document.querySelectorAll('form[data-steps]').forEach(function (form) {
    var steps = Array.prototype.slice.call(form.querySelectorAll('.step'));
    if (steps.length < 2) return;
    var back = form.querySelector('[data-step-back]');
    var next = form.querySelector('[data-step-next]');
    var submit = form.querySelector('[data-step-submit]');
    var label = form.querySelector('[data-steps-label]');
    var bar = form.querySelector('.progress > div');
    form.querySelector('[data-steps-progress]').hidden = false;
    // Native validation can't focus fields in hidden steps, so validate per step here instead.
    form.noValidate = true;

    // After a failed server-side submit, start at the first step that has an error.
    var current = 0;
    steps.some(function (s, i) {
      if (s.querySelector('.field-validation-error, .error:not(:empty)')) { current = i; return true; }
      return false;
    });

    function show(i) {
      current = i;
      steps.forEach(function (s, j) { s.hidden = j !== i; });
      back.hidden = i === 0;
      next.hidden = i === steps.length - 1;
      submit.hidden = i !== steps.length - 1;
      label.textContent = 'الخطوة ' + (i + 1) + ' من ' + steps.length;
      bar.style.width = ((i + 1) / steps.length * 100) + '%';
    }

    // Fields hidden by the property-type toggle are skipped; the step itself may be hidden.
    function firstInvalid(i) {
      var fields = steps[i].querySelectorAll('input, select, textarea');
      for (var k = 0; k < fields.length; k++) {
        var h = fields[k].closest('[hidden]');
        if (h && h !== steps[i]) continue;
        if (!fields[k].checkValidity()) return fields[k];
      }
      return null;
    }

    function stepValid(i) {
      var bad = firstInvalid(i);
      if (bad) bad.reportValidity();
      return !bad;
    }

    next.addEventListener('click', function () {
      if (!stepValid(current)) return;
      show(current + 1);
      form.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
    back.addEventListener('click', function () { show(current - 1); });
    form.addEventListener('submit', function (e) {
      for (var i = 0; i < steps.length; i++) {
        if (firstInvalid(i)) { e.preventDefault(); show(i); stepValid(i); return; }
      }
      submit.disabled = true;
      submit.textContent = 'جاري الإرسال…';
    });
    show(current);
  });
})();
