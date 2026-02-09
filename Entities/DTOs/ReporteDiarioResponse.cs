using System;
using System.Collections.Generic;

namespace Entities.DTOs
{
    public class ReporteDiarioResponse
    {
        public DateTime Fecha { get; set; }
        public int TotalTransacciones { get; set; }
        public decimal TotalMonto { get; set; }
        public int TransaccionesExitosas { get; set; }
        public int TransaccionesFallidas { get; set; }
        public decimal MontoPromedio { get; set; }
        public List<TransaccionDetalle> Detalles { get; set; } = new List<TransaccionDetalle>();
    }

    public class TransaccionDetalle
    {
        public int TransaccionId { get; set; }
        public string TelefonoOrigen { get; set; } = null!;
        public string TelefonoDestino { get; set; } = null!;
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public string EntidadOrigen { get; set; } = null!;
        public string EntidadDestino { get; set; } = null!;
    }
}