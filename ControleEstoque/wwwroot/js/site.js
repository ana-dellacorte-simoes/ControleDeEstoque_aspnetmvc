// Sem inline script (CSP: script-src 'self'). Tudo aqui é progressivo: o servidor valida de qualquer forma.
(function () {
  'use strict';
  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };

  // menu mobile
  document.addEventListener('click', function (e) {
    if (e.target.closest('[data-menu-toggle]')) document.body.classList.toggle('menu-open');
    else if (e.target.closest('[data-menu-close]')) document.body.classList.remove('menu-open');
    var close = e.target.closest('[data-alert-close]');
    if (close) close.closest('.alert').remove();
  });

  // larguras das barras (CSSOM é permitido pela CSP, atributo style inline não)
  $$('[data-pct]').forEach(function (el) {
    requestAnimationFrame(function () { el.style.width = (parseFloat(el.getAttribute('data-pct')) || 0) + '%'; });
  });

  // alerts de sucesso somem sozinhos
  $$('.alert-ok').forEach(function (a) { setTimeout(function () { if (a.isConnected) a.remove(); }, 9000); });

  // formulários: mostra o disponível e limita a quantidade quando aplicável
  var sel = $('[data-produto-select]');
  var qtd = $('[data-quantidade]');
  var hint = $('[data-disp-hint]');
  function atualizar() {
    if (!sel) return;
    var opt = sel.options[sel.selectedIndex];
    var limitar = sel.getAttribute('data-limitar') === '1';
    if (!opt || !opt.value) { if (hint) hint.textContent = 'Selecione um produto para ver o disponível.'; if (qtd) qtd.removeAttribute('max'); return; }
    var disp = parseInt(opt.getAttribute('data-disp'), 10) || 0;
    if (limitar && qtd) { qtd.max = Math.max(disp, 0); }
    if (hint) hint.textContent = 'Disponível: ' + disp.toLocaleString('pt-BR') + ' un.';
  }
  if (sel) { sel.addEventListener('change', atualizar); atualizar(); }


  // ordenação de tabelas (cliente): clique no cabeçalho alterna crescente → decrescente → ordem original
  $$('table.table').forEach(function (table) {
    var ths = $$('thead th', table);
    var body = table.tBodies[0];
    if (!body || !ths.some(function (t) { return t.hasAttribute('data-sort'); })) return;
    var rows = $$('tr', body);
    rows.forEach(function (r, i) { r._ord = i; });
    var state = { col: -1, dir: 0 };

    function valor(row, col, tipo) {
      var td = row.cells[col];
      var v = td ? (td.getAttribute('data-sort-value') || td.textContent) : '';
      v = v.trim();
      return tipo === 'num' ? (parseFloat(v.replace(/\./g, '').replace(',', '.')) || 0) : v;
    }
    function aplicar(col, dir) {
      state = { col: col, dir: dir };
      var tipo = col >= 0 ? ths[col].getAttribute('data-sort') : null;
      var ordenadas = rows.slice().sort(function (a, b) {
        if (dir === 0) return a._ord - b._ord;
        var x = valor(a, col, tipo), y = valor(b, col, tipo);
        var c = tipo === 'num' ? x - y : String(x).localeCompare(String(y), 'pt-BR', { numeric: true, sensitivity: 'base' });
        return (c || a._ord - b._ord) * dir;
      });
      ordenadas.forEach(function (r) { body.appendChild(r); });
      ths.forEach(function (t, i) {
        if (!t.hasAttribute('data-sort')) return;
        t.setAttribute('aria-sort', i === col && dir ? (dir > 0 ? 'ascending' : 'descending') : 'none');
      });
      if (select) select.value = dir && col >= 0 ? col + ':' + dir : '';
    }

    ths.forEach(function (th, i) {
      if (!th.hasAttribute('data-sort')) return;
      var b = document.createElement('button');
      b.type = 'button'; b.className = 'sort-btn';
      b.textContent = th.textContent;
      th.textContent = ''; th.appendChild(b);
      th.setAttribute('aria-sort', 'none');
      b.addEventListener('click', function () {
        var dir = state.col === i ? (state.dir === 1 ? -1 : state.dir === -1 ? 0 : 1) : 1;
        aplicar(i, dir);
      });
    });

    // celular: o cabeçalho some, então oferece um seletor equivalente
    var select = document.createElement('select');
    select.className = 'sort-select'; select.setAttribute('aria-label', 'Ordenar por');
    var o0 = document.createElement('option'); o0.value = ''; o0.textContent = 'Ordenar por…'; select.appendChild(o0);
    ths.forEach(function (th, i) {
      if (!th.hasAttribute('data-sort')) return;
      var nome = th.textContent.trim(), num = th.getAttribute('data-sort') === 'num';
      [[1, num ? ' (menor → maior)' : ' (A → Z)'], [-1, num ? ' (maior → menor)' : ' (Z → A)']].forEach(function (d) {
        var o = document.createElement('option'); o.value = i + ':' + d[0]; o.textContent = nome + d[1]; select.appendChild(o);
      });
    });
    select.addEventListener('change', function () {
      if (!select.value) return aplicar(-1, 0);
      var p = select.value.split(':'); aplicar(parseInt(p[0], 10), parseInt(p[1], 10));
    });
    var wrap = table.closest('.table-wrap');
    if (wrap) wrap.parentNode.insertBefore(select, wrap);
  });

  // evita duplo envio
  $$('form[data-submit-once]').forEach(function (f) {
    f.addEventListener('submit', function () {
      if (!f.checkValidity()) return;
      var b = f.querySelector('button[type=submit]');
      if (b) setTimeout(function () { b.disabled = true; }, 0);
    });
  });

  // diálogo de liberação
  var dlg = $('[data-dialog-liberar]');
  if (dlg) {
    document.addEventListener('click', function (e) {
      var b = e.target.closest('[data-liberar]');
      if (!b) return;
      $('[data-dlg-input]', dlg).value = b.getAttribute('data-liberar');
      $('[data-dlg-id]', dlg).textContent = '#' + b.getAttribute('data-liberar');
      $('[data-dlg-desc]', dlg).textContent = b.getAttribute('data-desc') || '';
      if (typeof dlg.showModal === 'function') dlg.showModal(); else dlg.setAttribute('open', '');
    });
    $('[data-dlg-cancel]', dlg).addEventListener('click', function () { dlg.close(); });
    dlg.addEventListener('click', function (e) { if (e.target === dlg) dlg.close(); });
    $('form', dlg).addEventListener('submit', function () {
      var b = this.querySelector('button[type=submit]'); if (b) setTimeout(function () { b.disabled = true; }, 0);
    });
  }

  // diálogo de desativação de produto
  var dlg2 = $('[data-dialog-desativar]');
  if (dlg2) {
    document.addEventListener('click', function (e) {
      var b = e.target.closest('[data-desativar]');
      if (!b) return;
      $('[data-dlg2-input]', dlg2).value = b.getAttribute('data-desativar');
      $('[data-dlg2-id]', dlg2).textContent = '#' + b.getAttribute('data-desativar');
      $('[data-dlg2-desc]', dlg2).textContent = b.getAttribute('data-desc') || '';
      if (typeof dlg2.showModal === 'function') dlg2.showModal(); else dlg2.setAttribute('open', '');
    });
    $('[data-dlg2-cancel]', dlg2).addEventListener('click', function () { dlg2.close(); });
    dlg2.addEventListener('click', function (e) { if (e.target === dlg2) dlg2.close(); });
    $('form', dlg2).addEventListener('submit', function () {
      var b = this.querySelector('button[type=submit]'); if (b) setTimeout(function () { b.disabled = true; }, 0);
    });
  }
})();
