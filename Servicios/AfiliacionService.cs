using Logica_Negocio.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Servicios.Interfaces;

namespace Servicios
{
    public class AfiliacionService : IAfiliacionService
    {
        private readonly string _connectionString;
        private readonly IBitacoraService _bitacoraService;

        public AfiliacionService(IConfiguration configuration, IBitacoraService bitacoraService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _bitacoraService = bitacoraService;
        }

        public async Task<(bool existe, string? identificacion, string? nombre, string? numeroCuenta)> ObtenerInfoPorTelefonoAsync(string telefono)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT a.Identificacion_Usuario, u.Nombre_Completo, a.Numero_Cuenta
                    FROM Afiliacion a
                    INNER JOIN Usuarios u ON a.Identificacion_Usuario = u.Identificacion
                    WHERE a.Telefono = @telefono AND a.ID_Estado = 1", connection);

                command.Parameters.AddWithValue("@telefono", telefono);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var identificacion = reader["Identificacion_Usuario"].ToString();
                    var nombre = reader["Nombre_Completo"].ToString();
                    var numeroCuenta = reader["Numero_Cuenta"].ToString();

                    await _bitacoraService.RegistrarAsync(
                        usuario: "Sistema",
                        accion: "CONSULTA_AFILIACION",
                        resultado: "EXITO",
                        descripcion: $"Teléfono {telefono} encontrado: {nombre}",
                        servicio: "AfiliacionService.ObtenerInfoPorTelefonoAsync"
                    );

                    return (true, identificacion, nombre, numeroCuenta);
                }

                await _bitacoraService.RegistrarAsync(
                    usuario: "Sistema",
                    accion: "CONSULTA_AFILIACION",
                    resultado: "NO_ENCONTRADO",
                    descripcion: $"Teléfono {telefono} no encontrado en afiliaciones",
                    servicio: "AfiliacionService.ObtenerInfoPorTelefonoAsync"
                );

                return (false, null, null, null);
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: "Sistema",
                    accion: "CONSULTA_AFILIACION",
                    resultado: "ERROR",
                    descripcion: $"Error al consultar teléfono {telefono}: {ex.Message}",
                    servicio: "AfiliacionService.ObtenerInfoPorTelefonoAsync"
                );
                throw;
            }
        }
    }
}