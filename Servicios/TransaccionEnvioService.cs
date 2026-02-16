using Logica_Negocio.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Servicios.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios
{
    public class TransaccionEnvioService : ITransaccionEnvioService
    {
        private readonly string _connectionString;
        private readonly IBitacoraService _bitacoraService;

        public TransaccionEnvioService(IConfiguration configuration, IBitacoraService bitacoraService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _bitacoraService = bitacoraService;
        }

        public async Task RegistrarTransferenciaAsync(
            string telefonoOrigen,
            string nombreOrigen,
            string telefonoDestino,
            decimal monto,
            string descripcion,
            int codigoRespuesta,
            string mensajeRespuesta)
        {
            try
            {
                // Nota: ID_Entidad_Origen y ID_EntidadDestino son requeridos
                // Como es transferencia interna, usamos la misma entidad (ej. ID 1 = Banco Central)
                // Tus compañeros deben tener definidas las entidades en su tabla
                int idEntidad = 1; // Asumimos que 1 es "Banco Central" o tu entidad

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO Transaccion_Envio 
                    (ID_Entidad_Origen, ID_EntidadDestino, Telefono_Origen, Nombre_Origen, 
                     Telefono_Destino, Monto, Descripcion, FechaEnvio, Codigo_Respuesta, 
                     Mensaje_Respuesta, ID_Estado)
                    VALUES 
                    (@entidad, @entidad, @telOrigen, @nombreOrigen, 
                     @telDestino, @monto, @descripcion, GETDATE(), @codigo, 
                     @mensaje, 4)", connection); // Estado 4 = Completado

                command.Parameters.AddWithValue("@entidad", idEntidad);
                command.Parameters.AddWithValue("@telOrigen", telefonoOrigen);
                command.Parameters.AddWithValue("@nombreOrigen", nombreOrigen ?? "Sistema");
                command.Parameters.AddWithValue("@telDestino", telefonoDestino);
                command.Parameters.AddWithValue("@monto", monto);
                command.Parameters.AddWithValue("@descripcion", descripcion ?? "Transferencia");
                command.Parameters.AddWithValue("@codigo", codigoRespuesta);
                command.Parameters.AddWithValue("@mensaje", mensajeRespuesta);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "REGISTRO_TRANSACCION_ENVIO",
                    resultado: "EXITO",
                    descripcion: $"Transferencia registrada: {telefonoOrigen} -> {telefonoDestino}",
                    servicio: "TransaccionEnvioService.RegistrarTransferenciaAsync"
                );
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAccionBitacora(
                    usuario: "Sistema",
                    accion: "REGISTRO_TRANSACCION_ENVIO",
                    resultado: "ERROR",
                    descripcion: $"Error al registrar transferencia: {ex.Message}",
                    servicio: "TransaccionEnvioService.RegistrarTransferenciaAsync"
                );
                // No relanzamos la excepción para no interrumpir el flujo principal
            }
        }
    }
}