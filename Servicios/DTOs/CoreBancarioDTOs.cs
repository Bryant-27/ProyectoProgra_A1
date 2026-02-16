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

    // DTO para solicitud de transacción individual
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

    // DTO para solicitud de transferencia entre cuentas
    public class TransferenciaRequestDto
    {
        [JsonPropertyName("identificacionOrigen")]
        public string IdentificacionOrigen { get; set; } = string.Empty;

        [JsonPropertyName("cuentaOrigen")]
        public string CuentaOrigen { get; set; } = string.Empty;

        [JsonPropertyName("identificacionDestino")]
        public string IdentificacionDestino { get; set; } = string.Empty;

        [JsonPropertyName("cuentaDestino")]
        public string CuentaDestino { get; set; } = string.Empty;

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("telefonoOrigen")]
        public string? TelefonoOrigen { get; set; }

        [JsonPropertyName("nombreOrigen")]
        public string? NombreOrigen { get; set; }

        [JsonPropertyName("telefonoDestino")]
        public string? TelefonoDestino { get; set; }

        [JsonPropertyName("referenciaExterna")]
        public string? ReferenciaExterna { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }
    }

    // DTO para respuesta de transferencia
    public class TransferenciaResponseDto : CoreResponseDto
    {
        [JsonPropertyName("saldoOrigenNuevo")]
        public decimal? SaldoOrigenNuevo { get; set; }

        [JsonPropertyName("saldoDestinoNuevo")]
        public decimal? SaldoDestinoNuevo { get; set; }
    }
}