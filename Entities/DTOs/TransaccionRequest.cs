using System.Text.Json.Serialization;

namespace Entities.DTOs
{
    public class TransaccionRequest
    {
        [JsonPropertyName("entidadOrigen")]
        public int EntidadOrigen { get; set; }

        [JsonPropertyName("entidadDestino")]
        public int EntidadDestino { get; set; }

        [JsonPropertyName("telefonoOrigen")]
        public string TelefonoOrigen { get; set; }

        [JsonPropertyName("nombreOrigen")]
        public string NombreOrigen { get; set; }

        [JsonPropertyName("telefonoDestino")]
        public string TelefonoDestino { get; set; }

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }
    }
}