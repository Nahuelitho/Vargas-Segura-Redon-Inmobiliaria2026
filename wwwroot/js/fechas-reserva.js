(function ($) {
    const validarFecha = $.validator.methods.date;
    $.validator.methods.date = function (value, element) {
        if (!element.hasAttribute('data-fecha-reserva')) {
            return validarFecha.call(this, value, element);
        }
        if (this.optional(element)) return true;
        const partes = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value.trim());
        if (!partes) return false;
        const dia = Number(partes[1]), mes = Number(partes[2]), anio = Number(partes[3]);
        const fecha = new Date(anio, mes - 1, dia);
        return anio >= 1000 && fecha.getFullYear() === anio && fecha.getMonth() === mes - 1 && fecha.getDate() === dia;
    };
    $('[data-fecha-reserva]').attr('data-val-date', 'Ingrese una fecha válida en formato dd/mm/aaaa.');
})(jQuery);
