using System.Text.Json.Serialization;

namespace Entities.DTOs
{
    public class BitacoraResponse
    {
        [JsonPropertyName("codigo")]
        public int Codigo { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("bitacoraId")]
        public long? BitacoraId { get; set; }

        [JsonPropertyName("fechaRegistro")]
        public DateTime? FechaRegistro { get; set; }
    }
}