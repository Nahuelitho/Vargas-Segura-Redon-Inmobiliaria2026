// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelectorAll('[data-busqueda-url]').forEach(input => {
    const select = document.getElementById(input.dataset.busquedaSelect);
    const status = document.getElementById(input.getAttribute('aria-describedby'));
    let timer;
    let request;
    let version = 0;

    function mostrarResultados(resultados = []) {
        const selected = select.value ? select.selectedOptions[0] : null;
        select.replaceChildren(new Option('Seleccione...', ''));
        if (selected) select.add(selected);
        resultados.forEach(item => {
            if (String(item.id) !== selected?.value)
                select.add(new Option(item.texto, item.id));
        });
        select.value = selected?.value ?? '';
    }

    input.addEventListener('input', () => {
        clearTimeout(timer);
        request?.abort();
        const currentVersion = ++version;
        const termino = input.value.trim();
        mostrarResultados();
        if (termino.length < 2) {
            status.textContent = 'Escribí al menos 2 caracteres.';
            return;
        }

        status.textContent = 'Buscando...';
        timer = setTimeout(async () => {
            request = new AbortController();
            try {
                const url = new URL(input.dataset.busquedaUrl, window.location.href);
                url.searchParams.set('termino', termino);
                const response = await fetch(url, { signal: request.signal });
                if (!response.ok) throw new Error('Error de búsqueda');
                const data = await response.json();
                if (currentVersion !== version) return;
                mostrarResultados(data.resultados);
                status.textContent = data.hayMas
                    ? 'Se muestran 20 resultados. Afiná la búsqueda para encontrar otros.'
                    : data.resultados.length
                        ? `${data.resultados.length} resultado(s). Seleccioná una opción.`
                        : 'No se encontraron coincidencias.';
            } catch (error) {
                if (error.name !== 'AbortError' && currentVersion === version)
                    status.textContent = 'No se pudo buscar. Modificá el texto para reintentar.';
            }
        }, 300);
    });
});
