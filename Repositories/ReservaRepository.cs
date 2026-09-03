using Inmobiliaria.Models;
using MySqlConnector;

namespace Inmobiliaria.Repositories;

public class ReservaRepository(IConfiguration config)
{
    private readonly string _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    private MySqlConnection CrearConexion() => new(_cadenaConexion);
    public async Task<List<Reserva>> ObtenerTodos()
    {
        var reservas = new List<Reserva>(); await using var c = CrearConexion(); await c.OpenAsync();
        const string sql = "SELECT r.*, i.nombre inquilino_nombre, i.apellido inquilino_apellido, m.direccion inmueble_direccion FROM reservas r JOIN inquilinos i ON i.id=r.id_inquilino JOIN inmuebles m ON m.id=r.id_inmueble WHERE r.estado=true ORDER BY r.fecha_inicio DESC";
        await using var cmd = new MySqlCommand(sql,c); await using var l=await cmd.ExecuteReaderAsync(); while(await l.ReadAsync()) reservas.Add(Mapear(l)); return reservas;
    }
    public async Task<Reserva?> ObtenerPorId(int id)
    {
        await using var c=CrearConexion(); await c.OpenAsync(); const string sql="SELECT r.*, i.nombre inquilino_nombre, i.apellido inquilino_apellido, m.direccion inmueble_direccion FROM reservas r JOIN inquilinos i ON i.id=r.id_inquilino JOIN inmuebles m ON m.id=r.id_inmueble WHERE r.id=@id AND r.estado=true";
        await using var cmd=new MySqlCommand(sql,c); cmd.Parameters.AddWithValue("@id",id); await using var l=await cmd.ExecuteReaderAsync(); return await l.ReadAsync()?Mapear(l):null;
    }
    public async Task<int> Crear(Reserva r) { await using var c=CrearConexion(); await c.OpenAsync(); const string sql="INSERT INTO reservas (id_inquilino,id_inmueble,fecha_inicio,fecha_fin,monto_por_dia,fecha_terminacion,multa,id_reserva_origen,estado) VALUES (@inquilino,@inmueble,@inicio,@fin,@monto,@terminacion,@multa,@origen,true); SELECT LAST_INSERT_ID();"; await using var cmd=new MySqlCommand(sql,c); Cargar(cmd,r); return Convert.ToInt32(await cmd.ExecuteScalarAsync()); }
    public async Task<bool> Actualizar(Reserva r) { await using var c=CrearConexion(); await c.OpenAsync(); const string sql="UPDATE reservas SET id_inquilino=@inquilino,id_inmueble=@inmueble,fecha_inicio=@inicio,fecha_fin=@fin,monto_por_dia=@monto,fecha_terminacion=@terminacion,multa=@multa,id_reserva_origen=@origen WHERE id=@id AND estado=true"; await using var cmd=new MySqlCommand(sql,c); cmd.Parameters.AddWithValue("@id",r.Id); Cargar(cmd,r); return await cmd.ExecuteNonQueryAsync()>0; }
    public async Task<bool> Eliminar(int id) { await using var c=CrearConexion(); await c.OpenAsync(); await using var cmd=new MySqlCommand("UPDATE reservas SET estado=false WHERE id=@id",c); cmd.Parameters.AddWithValue("@id",id); return await cmd.ExecuteNonQueryAsync()>0; }
    private static void Cargar(MySqlCommand c, Reserva r) { c.Parameters.AddWithValue("@inquilino",r.IdInquilino);c.Parameters.AddWithValue("@inmueble",r.IdInmueble);c.Parameters.AddWithValue("@inicio",r.FechaInicio);c.Parameters.AddWithValue("@fin",r.FechaFin);c.Parameters.AddWithValue("@monto",r.MontoPorDia);c.Parameters.AddWithValue("@terminacion",(object?)r.FechaTerminacion??DBNull.Value);c.Parameters.AddWithValue("@multa",(object?)r.Multa??DBNull.Value);c.Parameters.AddWithValue("@origen",(object?)r.IdReservaOrigen??DBNull.Value); }
    private static Reserva Mapear(MySqlDataReader l) => new(){Id=l.GetInt32("id"),IdInquilino=l.GetInt32("id_inquilino"),IdInmueble=l.GetInt32("id_inmueble"),FechaInicio=l.GetDateTime("fecha_inicio"),FechaFin=l.GetDateTime("fecha_fin"),MontoPorDia=l.GetDecimal("monto_por_dia"),FechaTerminacion=l.IsDBNull(l.GetOrdinal("fecha_terminacion"))?null:l.GetDateTime("fecha_terminacion"),Multa=l.IsDBNull(l.GetOrdinal("multa"))?null:l.GetDecimal("multa"),IdReservaOrigen=l.IsDBNull(l.GetOrdinal("id_reserva_origen"))?null:l.GetInt32("id_reserva_origen"),Estado=l.GetBoolean("estado"),Inquilino=new Inquilino{Nombre=l.GetString("inquilino_nombre"),Apellido=l.GetString("inquilino_apellido")},Inmueble=new Inmueble{Direccion=l.GetString("inmueble_direccion")}};
}
