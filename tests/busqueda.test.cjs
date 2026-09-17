const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../wwwroot/js/site.js'), 'utf8');

function setup(selected = null) {
    let handler, timer;
    class Option {
        constructor(text, value) { this.text = text; this.value = String(value); }
    }
    const select = {
        value: selected ? String(selected.id) : '',
        options: selected ? [new Option(selected.texto, selected.id)] : [],
        get selectedOptions() { return this.options.filter(x => x.value === this.value); },
        replaceChildren(...options) { this.options = options; this.value = options[0]?.value ?? ''; },
        add(option) { this.options.push(option); }
    };
    const status = {};
    const input = {
        value: '',
        dataset: { busquedaUrl: '/Propietario/Buscar', busquedaSelect: 'select' },
        getAttribute: () => 'status',
        addEventListener: (_, callback) => { handler = callback; }
    };
    const calls = [];
    vm.runInNewContext(source, {
        document: { querySelectorAll: () => [input], getElementById: id => id === 'select' ? select : status },
        window: { location: { href: 'https://example.test/Inmueble/Crear' } },
        Option, URL, AbortController,
        setTimeout: callback => { timer = callback; return 1; },
        clearTimeout: () => { timer = null; },
        fetch: (url, options) => new Promise(resolve => calls.push({ url, options, resolve }))
    });
    return {
        select, status, calls,
        type(value) { input.value = value; handler(); },
        runTimer() { const callback = timer; timer = null; return callback?.(); },
        reply(index, resultados, hayMas = false) {
            calls[index].resolve({ ok: true, json: async () => ({ resultados, hayMas }) });
        }
    };
}

test('no consulta antes de dos caracteres y agrupa la escritura rapida', async () => {
    const ui = setup();
    ui.type('a');
    await ui.runTimer();
    assert.equal(ui.calls.length, 0);
    ui.type('an');
    ui.type('ana');
    const pending = ui.runTimer();
    assert.equal(ui.calls.length, 1);
    assert.equal(ui.calls[0].url.searchParams.get('termino'), 'ana');
    ui.reply(0, []);
    await pending;
    assert.match(ui.status.textContent, /No se encontraron/);
});

test('mantiene la seleccion de edicion sin duplicarla y avisa si hay mas resultados', async () => {
    const ui = setup({ id: 42, texto: 'Seleccion actual' });
    ui.type('ana');
    const pending = ui.runTimer();
    ui.reply(0, [{ id: 42, texto: 'Seleccion actual' }, { id: 43, texto: 'Otra persona' }], true);
    await pending;
    assert.equal(ui.select.value, '42');
    assert.equal(ui.select.options.filter(x => x.value === '42').length, 1);
    assert.match(ui.status.textContent, /Afin/);
    ui.type('');
    assert.equal(ui.select.value, '42');
    assert.equal(ui.select.options.length, 2);
});

test('ignora respuestas atrasadas aunque el servidor complete una consulta cancelada', async () => {
    const ui = setup();
    ui.type('ana');
    const first = ui.runTimer();
    ui.type('pedro');
    const second = ui.runTimer();
    assert.equal(ui.calls[0].options.signal.aborted, true);
    ui.reply(1, [{ id: 2, texto: 'Pedro' }]);
    await second;
    ui.reply(0, [{ id: 1, texto: 'Ana' }]);
    await first;
    assert.equal(ui.select.options[1].text, 'Pedro');
    assert.equal(ui.select.value, '');
});

test('muestra un error recuperable y conserva el valor elegido', async () => {
    const ui = setup({ id: 42, texto: 'Seleccion actual' });
    ui.type('ana');
    const pending = ui.runTimer();
    ui.calls[0].resolve({ ok: false });
    await pending;
    assert.match(ui.status.textContent, /reintentar/);
    assert.equal(ui.select.value, '42');
});
