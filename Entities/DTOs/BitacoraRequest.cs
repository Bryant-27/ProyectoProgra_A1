using System.Text.Json.Serialization;

namespace Entities.DTOs
{
    public class BitacoraRequest
    {
        [JsonPropertyName("usuario")]
        public string Usuario { get; set; }

        [JsonPropertyName("accion")]
        public string Accion { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }
    }
}
 