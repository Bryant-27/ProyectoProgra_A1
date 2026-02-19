using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTOs
{
    public class MovimientosResponse
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; }
        public List<MovimientoDto> Movimientos { get; set; }
    }

    public class MovimientoDto
    {
        public long MovimientoId { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string TipoMovimiento { get; set; }
        public string Descripcion { get; set; }
        public decimal SaldoAnterior { get; set; }
        public decimal SaldoNuevo { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
    }
}
