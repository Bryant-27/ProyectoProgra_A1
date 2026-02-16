using System.Text.Json.Serialization;

namespace Entities.DTOs
{
    public class BitacoraConsultaResponse
    {
        [JsonPropertyName("bitacoraId")]
        public long BitacoraId { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; }

        [JsonPropertyName("accion")]
        public string Accion { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("fechaRegistro")]
        public DateTime FechaRegistro { get; set; }

        [JsonPropertyName("servicio")]
        public string Servicio { get; set; }

        [JsonPropertyName("resultado")]
        public string Resultado { get; set; }
    }
}