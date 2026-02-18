using DataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class AfiliacionRepository : IAfiliacionRepository
    {
        private readonly string _connectionString;

        public AfiliacionRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<Afiliacion> GetByTelefonoAndIdentificacionAsync(string telefono, string identificacion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // SQL usa nombres de COLUMNA (con guión bajo)
            var query = @"SELECT Afiliacion_ID, Numero_Cuenta, Identificacion_Usuario, 
                                 Telefono, ID_Estado, Fecha_Afiliacion, Fecha_Actualizacion
                          FROM Afiliacion 
                          WHERE Telefono = @Telefono 
                            AND Identificacion_Usuario = @Identificacion
                            AND ID_Estado = 1";  // ← 1 = ACTIVO (es int, no string)

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Telefono", telefono);
            command.Parameters.AddWithValue("@Identificacion", identificacion);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Afiliacion
                {
                    AfiliacionId = reader.GetInt64(0),           // Afiliacion_ID → AfiliacionId
                    NumeroCuenta = reader.GetString(1),          // Numero_Cuenta → NumeroCuenta
                    IdentificacionUsuario = reader.GetString(2), // Identificacion_Usuario → IdentificacionUsuario
                    Telefono = reader.GetString(3),
                    IdEstado = reader.GetInt32(4),               // ID_Estado → IdEstado (int)
                    FechaAfiliacion = reader.IsDBNull(5) ? null : reader.GetDateTime(5),  // Fecha_Afiliacion → FechaAfiliacion
                    FechaActualizacion = reader.IsDBNull(6) ? null : reader.GetDateTime(6) // Fecha_Actualizacion → FechaActualizacion
                };
            }

            return null;
        }
    }
}
