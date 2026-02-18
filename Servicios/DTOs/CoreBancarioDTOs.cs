using System.Text.Json.Serialization;

namespace Servicios.DTOs
{
    // DTO para respuestas estándar
    public class CoreResponseDto
    {
        [JsonPropertyName("codigo")]
        public int Codigo { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;
    }

    // DTO para respuesta de saldo
    public class SaldoResponseDto : CoreResponseDto
    {
        [JsonPropertyName("saldo")]
        public decimal? Saldo { get; set; }
    }

    // DTO para respuesta de movimiento
    public class MovimientoDto
    {
        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("saldoAnterior")]
        public decimal? SaldoAnterior { get; set; }

        [JsonPropertyName("saldoNuevo")]
        public decimal? SaldoNuevo { get; set; }
    }

    // DTO para respuesta de movimientos
    public class MovimientosResponseDto : CoreResponseDto
    {
        [JsonPropertyName("movimientos")]
        public List<MovimientoDto> Movimientos { get; set; } = new();
    }

    // DTO para solicitud de transacción (SRV14)
    public class TransaccionRequestDto
    {
        [JsonPropertyName("identificacion")]
        public string Identificacion { get; set; } = string.Empty;

        [JsonPropertyName("tipoMovimiento")]
        public string TipoMovimiento { get; set; } = string.Empty;

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("referenciaExterna")]
        public string? ReferenciaExterna { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }
    }

    // DTO para consulta de saldo por teléfono (SRV13)
    public class ConsultaSaldoTelefonoRequestDto
    {
        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [JsonPropertyName("identificacion")]
        public string Identificacion { get; set; } = string.Empty;
    }
}