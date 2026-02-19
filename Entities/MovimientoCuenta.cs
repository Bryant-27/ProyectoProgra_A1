using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Entities
{
    public  class MovimientoCuenta
    {
        [Key]
        public long MovimientoId { get; set; }
        public int CuentaId { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string TipoMovimiento { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public decimal SaldoAnterior { get; set; }
        public decimal SaldoNuevo { get; set; }
        public string ReferenciaExterna { get; set; } = null!;
    }
}
