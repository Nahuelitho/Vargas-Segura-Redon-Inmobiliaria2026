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

        if (fechaTerminacion > DateTime.Today)
            throw new ArgumentException(
                "La fecha efectiva de terminación no puede ser futura.");

        if (reserva.MontoPorDia is null or < 0)
            throw new ArgumentException(
                "La reserva no posee un monto por día válido.");

        var diasRestantes =
            (reserva.FechaFin.Date - fechaTerminacion).Days;

        var alquilerRestante =
            diasRestantes * reserva.MontoPorDia.Value;

        var diasTotales =
            (reserva.FechaFin.Date - reserva.FechaInicio.Date).Days;

        var diasTranscurridos =
            (fechaTerminacion - reserva.FechaInicio.Date).Days;

        var porcentaje =
            diasTranscurridos * 2 < diasTotales
                ? 0.50m
                : 0.25m;

        return decimal.Round(
            alquilerRestante * porcentaje,
            2,
            MidpointRounding.AwayFromZero);
    }
}
