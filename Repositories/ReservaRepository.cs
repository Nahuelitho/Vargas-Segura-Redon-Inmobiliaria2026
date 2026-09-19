using Inmobiliaria.Models;
using Inmobiliaria.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class ReservaRepository(IConfiguration config)
{
    private readonly string _cadenaConexion =
        config.GetConnectionString("DefaultConnection")!;

    private MySqlConnection CrearConexion() => new(_cadenaConexion);

    public async Task<bool> Existe(int id)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();

        const string sql = """
            SELECT EXISTS(
                SELECT 1
                FROM reservas
                WHERE id = @id
            )
            """;

        await using var cmd = new MySqlCommand(sql, c);
        cmd.Parameters.AddWithValue("@id", id);

        return Convert.ToBoolean(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> ExisteSuperposicion(
        int idInmueble,
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idReservaExcluir = null)
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string sql = """
            SELECT COUNT(*)
            FROM reservas
            WHERE id_inmueble = @idInmueble
              AND estado = true
              AND (@idReservaExcluir IS NULL OR id <> @idReservaExcluir)
              AND fecha_inicio < @fechaFin
              AND COALESCE(fecha_terminacion, fecha_fin) > @fechaInicio
            """;

        await using var comando = new MySqlCommand(sql, conexion);

        comando.Parameters.AddWithValue("@idInmueble", idInmueble);
        comando.Parameters.AddWithValue("@fechaInicio", fechaInicio.Date);
        comando.Parameters.AddWithValue("@fechaFin", fechaFin.Date);

        comando.Parameters.AddWithValue(
            "@idReservaExcluir",
            (object?)idReservaExcluir ?? DBNull.Value
        );

        return Convert.ToInt32(await comando.ExecuteScalarAsync()) > 0;
    }

    public async Task<List<SelectListItem>> ObtenerOpcionesOrigen(int idActual)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();

        const string sql = """
            SELECT
                r.id,
                r.fecha_inicio,
                r.estado,
                m.direccion
            FROM reservas r
            JOIN inmuebles m ON m.id = r.id_inmueble
            WHERE r.id <> @actual
            ORDER BY r.id DESC
            """;

        await using var cmd = new MySqlCommand(sql, c);
        cmd.Parameters.AddWithValue("@actual", idActual);

        await using var lector = await cmd.ExecuteReaderAsync();

        var opciones = new List<SelectListItem>();

        while (await lector.ReadAsync())
        {
            opciones.Add(new SelectListItem
            {
                Value = lector.GetInt32("id").ToString(),

                Text =
                    $"#{lector.GetInt32("id")} · " +
                    $"{lector.GetString("direccion")} · " +
                    $"{lector.GetDateTime("fecha_inicio"):dd/MM/yyyy}" +
                    $"{(lector.GetBoolean("estado") ? "" : " (dada de baja)")}"
            });
        }

        return opciones;
    }

    // Crea la reserva y la seña dentro de la misma transacción.
    public async Task<int> CrearConSena(
        Reserva reserva,
        decimal importeSena,
        DateTime fechaPago,
        int idUsuario)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(
            reserva,
            new System.ComponentModel.DataAnnotations.ValidationContext(reserva),
            true
        );

        if (idUsuario <= 0)
            throw new ArgumentException("Usuario inválido.");

        await using var c = CrearConexion();
        await c.OpenAsync();

        await using var tx = await c.BeginTransactionAsync();

        try
        {
            decimal porcentajeReserva;
            bool disponible;

            // Bloqueamos el inmueble mientras se valida y crea la reserva.
            await using (var cmdInmueble = new MySqlCommand("""
                SELECT porcentaje_reserva, disponible
                FROM inmuebles
                WHERE id = @id
                  AND estado = true
                FOR UPDATE
                """, c, tx))
            {
                cmdInmueble.Parameters.AddWithValue("@id", reserva.IdInmueble);

                await using var lector = await cmdInmueble.ExecuteReaderAsync();

                if (!await lector.ReadAsync())
                    throw new ArgumentException(
                        "El inmueble seleccionado no existe.");

                porcentajeReserva =
                    lector.GetDecimal("porcentaje_reserva");

                disponible =
                    lector.GetBoolean("disponible");
            }

            if (!disponible)
                throw new ArgumentException(
                    "El inmueble está suspendido y no admite nuevas reservas.");

            // Segunda comprobación de disponibilidad justo antes de guardar.
            await using (var verificar = new MySqlCommand("""
                SELECT COUNT(*)
                FROM reservas
                WHERE id_inmueble = @inmueble
                  AND estado = true
                  AND fecha_inicio < @fin
                  AND COALESCE(fecha_terminacion, fecha_fin) > @inicio
                """, c, tx))
            {
                verificar.Parameters.AddWithValue(
                    "@inmueble",
                    reserva.IdInmueble);

                verificar.Parameters.AddWithValue(
                    "@inicio",
                    reserva.FechaInicio.Date);

                verificar.Parameters.AddWithValue(
                    "@fin",
                    reserva.FechaFin.Date);

                var superpuesta =
                    Convert.ToInt32(await verificar.ExecuteScalarAsync()) > 0;

                if (superpuesta)
                    throw new ArgumentException(
                        "El inmueble ya posee una reserva durante ese período.");
            }

            var sena = SenaService.Preparar(
                reserva,
                porcentajeReserva,
                importeSena,
                fechaPago,
                idUsuario
            );

            await using var crear = new MySqlCommand("""
                INSERT INTO reservas
                (
                    id_inquilino,
                    id_inmueble,
                    fecha_inicio,
                    fecha_fin,
                    monto_por_dia,
                    fecha_terminacion,
                    multa,
                    id_reserva_origen,
                    id_usuario_creador,
                    estado
                )
                VALUES
                (
                    @inquilino,
                    @inmueble,
                    @inicio,
                    @fin,
                    @monto,
                    @terminacion,
                    @multa,
                    @origen,
                    @usuario,
                    true
                );

                SELECT LAST_INSERT_ID();
                """, c, tx);

            Cargar(crear, reserva);

            crear.Parameters.AddWithValue("@usuario", idUsuario);

            var id =
                Convert.ToInt32(await crear.ExecuteScalarAsync());

            if (sena is not null)
            {
                await using var pago = new MySqlCommand("""
                    INSERT INTO pagos
                    (
                        id_reserva,
                        concepto,
                        fecha_pago,
                        importe,
                        estado,
                        es_sena,
                        id_usuario_creador
                    )
                    VALUES
                    (
                        @reserva,
                        @concepto,
                        @fecha,
                        @importe,
                        true,
                        true,
                        @usuario
                    )
                    """, c, tx);

                pago.Parameters.AddWithValue("@reserva", id);
                pago.Parameters.AddWithValue("@concepto", sena.Concepto);
                pago.Parameters.AddWithValue("@fecha", sena.FechaPago);
                pago.Parameters.AddWithValue("@importe", sena.Importe);
                pago.Parameters.AddWithValue("@usuario", idUsuario);

                await pago.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<int> ObtenerCantidad()
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string sql = """
            SELECT COUNT(*)
            FROM reservas r
            JOIN inquilinos i ON i.id = r.id_inquilino
            JOIN inmuebles m ON m.id = r.id_inmueble
            WHERE r.estado = true
            """;

        await using var comando =
            new MySqlCommand(sql, conexion);

        return Convert.ToInt32(
            await comando.ExecuteScalarAsync());
    }

    public async Task<List<Reserva>> ObtenerTodos(
        int paginaActual = 1,
        int? limite = null)
    {
        var reservas = new List<Reserva>();

        await using var c = CrearConexion();
        await c.OpenAsync();

        var sql = """
            SELECT
                r.*,
                i.nombre AS inquilino_nombre,
                i.apellido AS inquilino_apellido,
                m.direccion AS inmueble_direccion
            FROM reservas r
            JOIN inquilinos i ON i.id = r.id_inquilino
            JOIN inmuebles m ON m.id = r.id_inmueble
            WHERE r.estado = true
            ORDER BY r.fecha_inicio DESC, r.id DESC
            """;

        if (limite.HasValue)
            sql += " LIMIT @limit OFFSET @offset";

        await using var cmd = new MySqlCommand(sql, c);

        if (limite.HasValue)
        {
            cmd.Parameters.AddWithValue(
                "@limit",
                limite.Value);

            cmd.Parameters.AddWithValue(
                "@offset",
                (long)(paginaActual - 1) * limite.Value);
        }

        await using var lector =
            await cmd.ExecuteReaderAsync();

        while (await lector.ReadAsync())
            reservas.Add(Mapear(lector));

        return reservas;
    }

    public async Task<Reserva?> ObtenerPorId(
        int id,
        bool incluirAuditoria = false)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();

        var sql = """
            SELECT
                r.*,
                i.nombre AS inquilino_nombre,
                i.apellido AS inquilino_apellido,
                m.direccion AS inmueble_direccion
            """;

        if (incluirAuditoria)
        {
            sql += "\n" + """
                ,
                CONCAT(uc.nombre, ' ', uc.apellido) AS usuario_creador,
                CONCAT(ut.nombre, ' ', ut.apellido) AS usuario_terminador
                """;
        }

        sql += "\n" + """
            FROM reservas r
            JOIN inquilinos i ON i.id = r.id_inquilino
            JOIN inmuebles m ON m.id = r.id_inmueble
            """;

        if (incluirAuditoria)
        {
            sql += "\n" + """
                LEFT JOIN usuarios uc
                    ON uc.id = r.id_usuario_creador
                LEFT JOIN usuarios ut
                    ON ut.id = r.id_usuario_terminador
                """;
        }

        sql += "\n" + """
            WHERE r.id = @id
              AND r.estado = true
            """;

        await using var cmd =
            new MySqlCommand(sql, c);

        cmd.Parameters.AddWithValue("@id", id);

        await using var lector =
            await cmd.ExecuteReaderAsync();

        if (!await lector.ReadAsync())
            return null;

        var reserva = Mapear(lector);

        if (incluirAuditoria)
        {
            reserva.UsuarioCreador =
                lector.IsDBNull(
                    lector.GetOrdinal("usuario_creador"))
                    ? null
                    : lector.GetString("usuario_creador");

            reserva.UsuarioTerminador =
                lector.IsDBNull(
                    lector.GetOrdinal("usuario_terminador"))
                    ? null
                    : lector.GetString("usuario_terminador");
        }

        return reserva;
    }

    // Se conserva por compatibilidad, pero las nuevas reservas
    // deben utilizar CrearConSena.
    public async Task<int> Crear(Reserva reserva)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();

        const string sql = """
            INSERT INTO reservas
            (
                id_inquilino,
                id_inmueble,
                fecha_inicio,
                fecha_fin,
                monto_por_dia,
                fecha_terminacion,
                multa,
                id_reserva_origen,
                estado
            )
            VALUES
            (
                @inquilino,
                @inmueble,
                @inicio,
                @fin,
                @monto,
                @terminacion,
                @multa,
                @origen,
                true
            );

            SELECT LAST_INSERT_ID();
            """;

        await using var cmd =
            new MySqlCommand(sql, c);

        Cargar(cmd, reserva);

        return Convert.ToInt32(
            await cmd.ExecuteScalarAsync());
    }

    // Fecha de terminación, multa y origen NO se editan
    // desde el formulario normal.
    public async Task<bool> Actualizar(Reserva reserva)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();

        const string sql = """
            UPDATE reservas
            SET
                id_inquilino = @inquilino,
                id_inmueble = @inmueble,
                fecha_inicio = @inicio,
                fecha_fin = @fin,
                monto_por_dia = @monto
            WHERE id = @id
              AND estado = true
            """;

        await using var cmd =
            new MySqlCommand(sql, c);

        cmd.Parameters.AddWithValue("@id", reserva.Id);
        cmd.Parameters.AddWithValue("@inquilino", reserva.IdInquilino);
        cmd.Parameters.AddWithValue("@inmueble", reserva.IdInmueble);
        cmd.Parameters.AddWithValue("@inicio", reserva.FechaInicio);
        cmd.Parameters.AddWithValue("@fin", reserva.FechaFin);

        cmd.Parameters.AddWithValue(
            "@monto",
            (object?)reserva.MontoPorDia ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> TerminarAnticipadamente(
        int idReserva,
        DateTime fechaTerminacion,
        int idUsuario)
    {
        if (idUsuario <= 0)
            throw new ArgumentException("Usuario inválido.");

        await using var c = CrearConexion();
        await c.OpenAsync();

        await using var tx =
            await c.BeginTransactionAsync();

        try
        {
            Reserva reserva;

            await using (var obtener = new MySqlCommand("""
                SELECT
                    id,
                    id_inquilino,
                    id_inmueble,
                    fecha_inicio,
                    fecha_fin,
                    monto_por_dia,
                    fecha_terminacion
                FROM reservas
                WHERE id = @id
                  AND estado = true
                FOR UPDATE
                """, c, tx))
            {
                obtener.Parameters.AddWithValue(
                    "@id",
                    idReserva);

                await using var lector =
                    await obtener.ExecuteReaderAsync();

                if (!await lector.ReadAsync())
                    return false;

                reserva = new Reserva
                {
                    Id = lector.GetInt32("id"),
                    IdInquilino =
                        lector.GetInt32("id_inquilino"),
                    IdInmueble =
                        lector.GetInt32("id_inmueble"),
                    FechaInicio =
                        lector.GetDateTime("fecha_inicio"),
                    FechaFin =
                        lector.GetDateTime("fecha_fin"),
                    MontoPorDia =
                        lector.GetDecimal("monto_por_dia"),
                    FechaTerminacion =
                        lector.IsDBNull(
                            lector.GetOrdinal("fecha_terminacion"))
                            ? null
                            : lector.GetDateTime(
                                "fecha_terminacion")
                };
            }

            if (reserva.FechaTerminacion.HasValue)
                throw new ArgumentException(
                    "La reserva ya fue terminada anticipadamente.");

            var multa =
                MultaService.Calcular(
                    reserva,
                    fechaTerminacion);

            await using (var actualizar =
                new MySqlCommand("""
                    UPDATE reservas
                    SET
                        fecha_terminacion = @fecha,
                        multa = @multa,
                        id_usuario_terminador = @usuario
                    WHERE id = @id
                      AND estado = true
                    """, c, tx))
            {
                actualizar.Parameters.AddWithValue(
                    "@fecha",
                    fechaTerminacion.Date);

                actualizar.Parameters.AddWithValue(
                    "@multa",
                    multa);

                actualizar.Parameters.AddWithValue(
                    "@usuario",
                    idUsuario);

                actualizar.Parameters.AddWithValue(
                    "@id",
                    idReserva);

                if (await actualizar.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            // Pago obligatorio de la multa.
            await using (var pago =
                new MySqlCommand("""
                    INSERT INTO pagos
                    (
                        id_reserva,
                        concepto,
                        fecha_pago,
                        importe,
                        estado,
                        es_sena,
                        id_usuario_creador
                    )
                    VALUES
                    (
                        @reserva,
                        'Multa por terminación anticipada',
                        @fecha,
                        @importe,
                        true,
                        false,
                        @usuario
                    )
                    """, c, tx))
            {
                pago.Parameters.AddWithValue(
                    "@reserva",
                    idReserva);

                pago.Parameters.AddWithValue(
                    "@fecha",
                    fechaTerminacion.Date);

                pago.Parameters.AddWithValue(
                    "@importe",
                    multa);

                pago.Parameters.AddWithValue(
                    "@usuario",
                    idUsuario);

                await pago.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Reserva>> ObtenerVigentes()
    {
        var reservas = new List<Reserva>();

        await using var c = CrearConexion();
        await c.OpenAsync();

        const string sql = """
            SELECT
                r.*,
                i.nombre AS inquilino_nombre,
                i.apellido AS inquilino_apellido,
                m.direccion AS inmueble_direccion
            FROM reservas r
            JOIN inquilinos i ON i.id = r.id_inquilino
            JOIN inmuebles m ON m.id = r.id_inmueble
            WHERE r.estado = true
              AND r.fecha_inicio <= CURDATE()
              AND COALESCE(
                    r.fecha_terminacion,
                    r.fecha_fin
                  ) > CURDATE()
            ORDER BY r.fecha_fin
            """;

        await using var cmd =
            new MySqlCommand(sql, c);

        await using var lector =
            await cmd.ExecuteReaderAsync();

        while (await lector.ReadAsync())
            reservas.Add(Mapear(lector));

        return reservas;
    }

    public async Task<List<Reserva>> ObtenerProximasATerminar(
        int dias)
    {
        if (dias < 0)
            throw new ArgumentException(
                "La cantidad de días no puede ser negativa.");

        var reservas = new List<Reserva>();

        await using var c = CrearConexion();
        await c.OpenAsync();

        const string sql = """
            SELECT
                r.*,
                i.nombre AS inquilino_nombre,
                i.apellido AS inquilino_apellido,
                m.direccion AS inmueble_direccion
            FROM reservas r
            JOIN inquilinos i ON i.id = r.id_inquilino
            JOIN inmuebles m ON m.id = r.id_inmueble
            WHERE r.estado = true
              AND r.fecha_terminacion IS NULL
              AND r.fecha_fin BETWEEN
                    CURDATE()
                    AND DATE_ADD(
                        CURDATE(),
                        INTERVAL @dias DAY
                    )
            ORDER BY r.fecha_fin
            """;

        await using var cmd =
            new MySqlCommand(sql, c);

        cmd.Parameters.AddWithValue("@dias", dias);

        await using var lector =
            await cmd.ExecuteReaderAsync();

        while (await lector.ReadAsync())
            reservas.Add(Mapear(lector));

        return reservas;
    }

    public async Task<bool> Eliminar(int id)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();

        await using var cmd =
            new MySqlCommand(
                "UPDATE reservas SET estado=false WHERE id=@id",
                c);

        cmd.Parameters.AddWithValue("@id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static void Cargar(
        MySqlCommand comando,
        Reserva reserva)
    {
        comando.Parameters.AddWithValue(
            "@inquilino",
            reserva.IdInquilino);

        comando.Parameters.AddWithValue(
            "@inmueble",
            reserva.IdInmueble);

        comando.Parameters.AddWithValue(
            "@inicio",
            reserva.FechaInicio);

        comando.Parameters.AddWithValue(
            "@fin",
            reserva.FechaFin);

        comando.Parameters.AddWithValue(
            "@monto",
            (object?)reserva.MontoPorDia ?? DBNull.Value);

        comando.Parameters.AddWithValue(
            "@terminacion",
            (object?)reserva.FechaTerminacion ?? DBNull.Value);

        comando.Parameters.AddWithValue(
            "@multa",
            (object?)reserva.Multa ?? DBNull.Value);

        comando.Parameters.AddWithValue(
            "@origen",
            (object?)reserva.IdReservaOrigen ?? DBNull.Value);
    }

    private static Reserva Mapear(
        MySqlDataReader lector)
    {
        return new Reserva
        {
            Id = lector.GetInt32("id"),

            IdInquilino =
                lector.GetInt32("id_inquilino"),

            IdInmueble =
                lector.GetInt32("id_inmueble"),

            FechaInicio =
                lector.GetDateTime("fecha_inicio"),

            FechaFin =
                lector.GetDateTime("fecha_fin"),

            MontoPorDia =
                lector.GetDecimal("monto_por_dia"),

            FechaTerminacion =
                lector.IsDBNull(
                    lector.GetOrdinal("fecha_terminacion"))
                    ? null
                    : lector.GetDateTime("fecha_terminacion"),

            Multa =
                lector.IsDBNull(
                    lector.GetOrdinal("multa"))
                    ? null
                    : lector.GetDecimal("multa"),

            IdReservaOrigen =
                lector.IsDBNull(
                    lector.GetOrdinal("id_reserva_origen"))
                    ? null
                    : lector.GetInt32("id_reserva_origen"),

            IdUsuarioCreador =
                lector.IsDBNull(
                    lector.GetOrdinal("id_usuario_creador"))
                    ? null
                    : lector.GetInt32("id_usuario_creador"),

            IdUsuarioTerminador =
                lector.IsDBNull(
                    lector.GetOrdinal("id_usuario_terminador"))
                    ? null
                    : lector.GetInt32("id_usuario_terminador"),

            Estado =
                lector.GetBoolean("estado"),

            Inquilino = new Inquilino
            {
                Nombre =
                    lector.GetString("inquilino_nombre"),

                Apellido =
                    lector.GetString("inquilino_apellido")
            },

            Inmueble = new Inmueble
            {
                Direccion =
                    lector.GetString("inmueble_direccion")
            }
        };
    }
}
