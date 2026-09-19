using Inmobiliaria.Models;

namespace Inmobiliaria.Services;

public static class MultaService
{
    public static decimal Calcular(
        Reserva reserva,
        DateTime fechaTerminacion)
    {
        fechaTerminacion = fechaTerminacion.Date;

        if (fechaTerminacion < reserva.FechaInicio.Date)
            throw new ArgumentException(
                "La terminación no puede ser anterior al inicio de la reserva.");

        if (fechaTerminacion >= reserva.FechaFin.Date)
            throw new ArgumentException(
                "La terminación anticipada debe ser anterior a la fecha de finalización original.");

        if (reserva.MontoPorDia is null or < 0)
            throw new ArgumentException(
                "La reserva no posee un monto por día válido.");

        var diasRestantes =
            (reserva.FechaFin.Date - fechaTerminacion).Days;

        var alquilerRestante =
            diasRestantes * reserva.MontoPorDia.Value;

        var porcentaje = ObtenerPorcentajeMulta(diasRestantes);

        return decimal.Round(
            alquilerRestante * porcentaje,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal ObtenerPorcentajeMulta(int diasRestantes)
    {
        // IMPORTANTE:
        // La consigna entregada menciona 50% y 25%,
        // pero no especifica cuándo corresponde cada uno.
        //
        // CRITERIO PROVISORIO:
        // más de 30 días restantes -> 50%
        // 30 días o menos -> 25%
        //
        // Cambiar solamente este método cuando el profesor
        // confirme la regla real.

        return diasRestantes > 30
            ? 0.50m
            : 0.25m;
    }
}