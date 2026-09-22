using System.Globalization;
using Inmobiliaria.Models;

namespace Inmobiliaria.Services;

public static class SenaService
{
    // La fecha de salida no se cobra; una reserva en el mismo día cobra un día.
    public static decimal CalcularTotal(Reserva reserva)
    {
        if (reserva.FechaFin.Date < reserva.FechaInicio.Date)
            throw new ArgumentException("La fecha de fin no puede ser anterior al inicio.");
        if (reserva.MontoPorDia is null or < 0)
            throw new ArgumentException("La reserva debe tener un monto por día válido.");
        return decimal.Round(Math.Max(1, (reserva.FechaFin.Date - reserva.FechaInicio.Date).Days) * reserva.MontoPorDia.Value, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal Calcular(Reserva reserva, decimal? porcentaje)
    {
        if (porcentaje is null or < 0 or > 100)
            throw new ArgumentException("El inmueble debe tener un porcentaje de reserva entre 0 y 100.");
        return decimal.Round(CalcularTotal(reserva) * porcentaje.Value / 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static Pago? Preparar(Reserva reserva, decimal? porcentaje, decimal importe, DateTime fecha, int idUsuario)
    {
        var esperado = Calcular(reserva, porcentaje);
        if (importe != esperado)
            throw new ArgumentException(
                $"La seña ingresada no coincide. Importe requerido: {esperado.ToString("0.00", CultureInfo.InvariantCulture)}.");
        if (idUsuario <= 0 || fecha.Year < 1000 || importe > 99999999.99m)
            throw new ArgumentException("Los datos de la seña no son válidos.");
        // No se genera un pago de importe cero cuando no se exige seña.
        return esperado == 0 ? null : new Pago { IdReserva = reserva.Id, Concepto = "Seña inicial", FechaPago = fecha.Date, Importe = esperado, EsSena = true, IdUsuarioCreador = idUsuario };
    }
}
