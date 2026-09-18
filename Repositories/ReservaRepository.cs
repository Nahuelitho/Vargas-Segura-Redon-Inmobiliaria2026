using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class ReservaRepository(IConfiguration config)
{
    private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    private MySqlConnection CrearConexion() => new(_cadenaConexion);

    public async Task<bool> Existe(int id)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT EXISTS(SELECT 1 FROM reservas WHERE id=@id)", c);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToBoolean(await cmd.ExecuteScalarAsync());
    }

    public async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> ObtenerOpcionesOrigen(int idActual)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();
        // Incluye las bajas para conservar referencias históricas al editar.
        await using var cmd = new MySqlCommand("SELECT r.id,r.fecha_inicio,r.estado,m.direccion FROM reservas r JOIN inmuebles m ON m.id=r.id_inmueble WHERE r.id<>@actual ORDER BY r.id DESC", c);
        cmd.Parameters.AddWithValue("@actual", idActual);
        await using var lector = await cmd.ExecuteReaderAsync();
        var opciones = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        while (await lector.ReadAsync())
            opciones.Add(new() { Value = lector.GetInt32("id").ToString(), Text = $"#{lector.GetInt32("id")} · {lector.GetString("direccion")} · {lector.GetDateTime("fecha_inicio"):dd/MM/yyyy}{(lector.GetBoolean("estado") ? "" : " (dada de baja)")}" });
        return opciones;
    }

    // Guarda reserva y seña en una transacción, usando el porcentaje del inmueble.
    public async Task<int> CrearConSena(Reserva reserva, decimal importeSena, DateTime fechaPago, int idUsuario)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(reserva, new System.ComponentModel.DataAnnotations.ValidationContext(reserva), true);
        await using var c = CrearConexion();
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using var inmueble = new MySqlCommand("SELECT porcentaje_reserva FROM inmuebles WHERE id=@id AND estado=true FOR UPDATE", c, tx);
        inmueble.Parameters.AddWithValue("@id", reserva.IdInmueble);
        var porcentaje = await inmueble.ExecuteScalarAsync();
        if (porcentaje is null or DBNull) throw new ArgumentException("El inmueble no existe o no tiene porcentaje de reserva.");
        var sena = Inmobiliaria.Services.SenaService.Preparar(reserva, Convert.ToDecimal(porcentaje), importeSena, fechaPago, idUsuario);
        await using var crear = new MySqlCommand("INSERT INTO reservas (id_inquilino,id_inmueble,fecha_inicio,fecha_fin,monto_por_dia,fecha_terminacion,multa,id_reserva_origen,estado) VALUES (@inquilino,@inmueble,@inicio,@fin,@monto,@terminacion,@multa,@origen,true); SELECT LAST_INSERT_ID();", c, tx);
        Cargar(crear, reserva);
        var id = Convert.ToInt32(await crear.ExecuteScalarAsync());
        if (sena is not null)
        {
            await using var pago = new MySqlCommand("INSERT INTO pagos (id_reserva,concepto,fecha_pago,importe,estado,es_sena,id_usuario_creador) VALUES (@reserva,@concepto,@fecha,@importe,true,true,@usuario)", c, tx);
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
    public async Task<int> ObtenerCantidad()
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();
        const string sql = "SELECT COUNT(*) FROM reservas r JOIN inquilinos i ON i.id=r.id_inquilino JOIN inmuebles m ON m.id=r.id_inmueble WHERE r.estado=true";
        await using var comando = new MySqlCommand(sql, conexion);
        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    public async Task<List<Reserva>> ObtenerTodos(int paginaActual = 1, int? limite = null)
    {
        var reservas = new List<Reserva>(); await using var c = CrearConexion(); await c.OpenAsync();
        var sql = "SELECT r.*, i.nombre inquilino_nombre, i.apellido inquilino_apellido, m.direccion inmueble_direccion FROM reservas r JOIN inquilinos i ON i.id=r.id_inquilino JOIN inmuebles m ON m.id=r.id_inmueble WHERE r.estado=true ORDER BY r.fecha_inicio DESC, r.id DESC";
        // Sin limite se conserva el listado completo para los selectores de los formularios.
        if (limite.HasValue) sql += " LIMIT @limit OFFSET @offset";
        await using var cmd = new MySqlCommand(sql,c);
        if (limite.HasValue)
        {
            cmd.Parameters.AddWithValue("@limit", limite.Value);
            cmd.Parameters.AddWithValue("@offset", (long)(paginaActual - 1) * limite.Value);
        }
        await using var l=await cmd.ExecuteReaderAsync(); while(await l.ReadAsync()) reservas.Add(Mapear(l)); return reservas;
    }
    public async Task<Reserva?> ObtenerPorId(int id)
    {
        await using var c=CrearConexion(); await c.OpenAsync(); const string sql="SELECT r.*, i.nombre inquilino_nombre, i.apellido inquilino_apellido, m.direccion inmueble_direccion FROM reservas r JOIN inquilinos i ON i.id=r.id_inquilino JOIN inmuebles m ON m.id=r.id_inmueble WHERE r.id=@id AND r.estado=true";
        await using var cmd=new MySqlCommand(sql,c); cmd.Parameters.AddWithValue("@id",id); await using var l=await cmd.ExecuteReaderAsync(); return await l.ReadAsync()?Mapear(l):null;
    }
    public async Task<int> Crear(Reserva r) { await using var c=CrearConexion(); await c.OpenAsync(); const string sql="INSERT INTO reservas (id_inquilino,id_inmueble,fecha_inicio,fecha_fin,monto_por_dia,fecha_terminacion,multa,id_reserva_origen,estado) VALUES (@inquilino,@inmueble,@inicio,@fin,@monto,@terminacion,@multa,@origen,true); SELECT LAST_INSERT_ID();"; await using var cmd=new MySqlCommand(sql,c); Cargar(cmd,r); return Convert.ToInt32(await cmd.ExecuteScalarAsync()); }
    public async Task<bool> Actualizar(Reserva r) { await using var c=CrearConexion(); await c.OpenAsync(); const string sql="UPDATE reservas SET id_inquilino=@inquilino,id_inmueble=@inmueble,fecha_inicio=@inicio,fecha_fin=@fin,monto_por_dia=@monto,fecha_terminacion=@terminacion,multa=@multa,id_reserva_origen=@origen WHERE id=@id AND estado=true"; await using var cmd=new MySqlCommand(sql,c); cmd.Parameters.AddWithValue("@id",r.Id); Cargar(cmd,r); return await cmd.ExecuteNonQueryAsync()>0; }
    public async Task<bool> Eliminar(int id) { await using var c=CrearConexion(); await c.OpenAsync(); await using var cmd=new MySqlCommand("UPDATE reservas SET estado=false WHERE id=@id",c); cmd.Parameters.AddWithValue("@id",id); return await cmd.ExecuteNonQueryAsync()>0; }
    private static void Cargar(MySqlCommand c, Reserva r) { c.Parameters.AddWithValue("@inquilino",r.IdInquilino);c.Parameters.AddWithValue("@inmueble",r.IdInmueble);c.Parameters.AddWithValue("@inicio",r.FechaInicio);c.Parameters.AddWithValue("@fin",r.FechaFin);c.Parameters.AddWithValue("@monto",(object?)r.MontoPorDia??DBNull.Value);c.Parameters.AddWithValue("@terminacion",(object?)r.FechaTerminacion??DBNull.Value);c.Parameters.AddWithValue("@multa",(object?)r.Multa??DBNull.Value);c.Parameters.AddWithValue("@origen",(object?)r.IdReservaOrigen??DBNull.Value); }
    private static Reserva Mapear(MySqlDataReader l) => new(){Id=l.GetInt32("id"),IdInquilino=l.GetInt32("id_inquilino"),IdInmueble=l.GetInt32("id_inmueble"),FechaInicio=l.GetDateTime("fecha_inicio"),FechaFin=l.GetDateTime("fecha_fin"),MontoPorDia=l.GetDecimal("monto_por_dia"),FechaTerminacion=l.IsDBNull(l.GetOrdinal("fecha_terminacion"))?null:l.GetDateTime("fecha_terminacion"),Multa=l.IsDBNull(l.GetOrdinal("multa"))?null:l.GetDecimal("multa"),IdReservaOrigen=l.IsDBNull(l.GetOrdinal("id_reserva_origen"))?null:l.GetInt32("id_reserva_origen"),Estado=l.GetBoolean("estado"),Inquilino=new Inquilino{Nombre=l.GetString("inquilino_nombre"),Apellido=l.GetString("inquilino_apellido")},Inmueble=new Inmueble{Direccion=l.GetString("inmueble_direccion")}};
}
