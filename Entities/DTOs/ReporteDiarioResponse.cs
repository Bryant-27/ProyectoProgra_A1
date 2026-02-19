using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Entities.DTOs
{
    public class ReporteDiarioResponse
    {
        [JsonPropertyName("codigo")]
        public int Codigo { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("fecha")]
        public DateTime? Fecha { get; set; }

        [JsonPropertyName("totalTransacciones")]
        public int TotalTransacciones { get; set; }

        [JsonPropertyName("montoTotal")]
        public decimal MontoTotal { get; set; }

        [JsonPropertyName("transacciones")]
        public List<TransaccionReporteDto> Transacciones { get; set; }
    }

    public class TransaccionReporteDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("telefonoOrigen")]
        public string TelefonoOrigen { get; set; }

        [JsonPropertyName("nombreOrigen")]
        public string NombreOrigen { get; set; }

        [JsonPropertyName("telefonoDestino")]
        public string TelefonoDestino { get; set; }

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; }
    }
}