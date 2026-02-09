using System.Text.Json.Serialization;

namespace Entities.DTOs
{
    public class TransaccionResponse
    {
        [JsonPropertyName("codigo")]
        public int Codigo { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = null!;

        public TransaccionResponse() { }

        public TransaccionResponse(int codigo, string descripcion)
        {
            Codigo = codigo;
            Descripcion = descripcion;
        }
    }
}