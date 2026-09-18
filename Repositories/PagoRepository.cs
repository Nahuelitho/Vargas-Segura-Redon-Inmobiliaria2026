using System.ComponentModel.DataAnnotations;
using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class PagoRepository(IConfiguration config)
{
    private MySqlConnection CrearConexion() => new(config.GetConnectionString("DefaultConnection"));

    public async Task InicializarTabla()
    {
        var cadena = new MySqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection")!) { AllowUserVariables = true };
        await using var c = new MySqlConnection(cadena.ConnectionString);
        await c.OpenAsync();
        using var recurso = typeof(PagoRepository).Assembly.GetManifestResourceStream("Inmobiliaria.Database.002_pagos.sql")!;
        using var lector = new StreamReader(recurso);
        await using var cmd = new MySqlCommand(await lector.ReadToEndAsync(), c);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Pago>> ObtenerPorReserva(int idReserva, bool incluirAuditoria = false)
    {
        await using var c = CrearConexion();
        await c.OpenAsync();
        // La consulta de auditoría se reserva a administradores.
        var sql = "SELECT p.id,p.id_reserva,p.concepto,p.fecha_pago,p.importe,p.estado,p.es_sena";
        sql += incluirAuditoria
            ? ",p.id_usuario_creador,p.id_usuario_anulador,CONCAT(c.nombre,' ',c.apellido) creador,CONCAT(a.nombre,' ',a.apellido) anulador FROM pagos p LEFT JOIN usuarios c ON c.id=p.id_usuario_creador LEFT JOIN usuarios a ON a.id=p.id_usuario_anulador"
            : " FROM pagos p";
        sql += " WHERE p.id_reserva=@reserva ORDER BY p.fecha_pago DESC,p.id DESC";
        await using var cmd = new MySqlCommand(sql, c);
        cmd.Parameters.AddWithValue("@reserva", idReserva);
        await using var l = await cmd.ExecuteReaderAsync();
        var pagos = new List<Pago>();
        while (await l.ReadAsync())
        {
            var p = new Pago { Id = l.GetInt32("id"), IdReserva = l.GetInt32("id_reserva"), Concepto = l.GetString("concepto"), FechaPago = l.GetDateTime("fecha_pago"), Importe = l.GetDecimal("importe"), Estado = l.GetBoolean("estado"), EsSena = l.GetBoolean("es_sena") };
            if (incluirAuditoria)
            {
                p.IdUsuarioCreador = l.IsDBNull(l.GetOrdinal("id_usuario_creador")) ? null : l.GetInt32("id_usuario_creador");
                p.IdUsuarioAnulador = l.IsDBNull(l.GetOrdinal("id_usuario_anulador")) ? null : l.GetInt32("id_usuario_anulador");
                p.UsuarioCreador = l.IsDBNull(l.GetOrdinal("creador")) ? null : l.GetString("creador");
                p.UsuarioAnulador = l.IsDBNull(l.GetOrdinal("anulador")) ? null : l.GetString("anulador");
            }
            pagos.Add(p);
        }
        return pagos;
    }

    public async Task<int> Crear(int idReserva, PagoFormulario formulario, int idUsuario)
    {
        Validator.ValidateObject(formulario, new ValidationContext(formulario), true);
        if (idUsuario <= 0) throw new ArgumentException("Usuario inválido.");
        await using var c = CrearConexion();
        await c.OpenAsync();
        await using var cmd = new MySqlCommand("INSERT INTO pagos (id_reserva,concepto,fecha_pago,importe,estado,es_sena,id_usuario_creador) SELECT id,@concepto,@fecha,@importe,true,false,@usuario FROM reservas WHERE id=@reserva AND estado=true", c);
        cmd.Parameters.AddWithValue("@reserva", idReserva);
        cmd.Parameters.AddWithValue("@concepto", formulario.Concepto.Trim());
        cmd.Parameters.AddWithValue("@fecha", formulario.FechaPago!.Value.Date);
        cmd.Parameters.AddWithValue("@importe", formulario.Importe!.Value);
        cmd.Parameters.AddWithValue("@usuario", idUsuario);
        return await cmd.ExecuteNonQueryAsync() > 0 ? checked((int)cmd.LastInsertedId) : 0;
    }

    public async Task<bool> EditarConcepto(int id, int idReserva, string concepto)
    {
        var formulario = new PagoConceptoFormulario { Concepto = concepto };
        Validator.ValidateObject(formulario, new ValidationContext(formulario), true);
        await using var c = CrearConexion();
        await c.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE pagos SET concepto=@concepto WHERE id=@id AND id_reserva=@reserva AND estado=true", c);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@reserva", idReserva);
        cmd.Parameters.AddWithValue("@concepto", concepto.Trim());
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> Anular(int id, int idReserva, int idUsuario)
    {
        if (idUsuario <= 0) throw new ArgumentException("Usuario inválido.");
        await using var c = CrearConexion();
        await c.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE pagos SET estado=false,id_usuario_anulador=@usuario WHERE id=@id AND id_reserva=@reserva AND estado=true", c);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@reserva", idReserva);
        cmd.Parameters.AddWithValue("@usuario", idUsuario);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
