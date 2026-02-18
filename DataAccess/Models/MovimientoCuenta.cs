using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    [Table("Movimiento_Cuenta")]
    public class MovimientoCuenta
    {
        [Key]
        [Column("MovimientoId")]
        public long MovimientoId { get; set; }

        [Column("Numero_Cuenta")]
        [StringLength(22)]
        public string NumeroCuenta { get; set; } = null!;

        [Column("FechaMovimiento")]
        public DateTime FechaMovimiento { get; set; }

        [Column("Monto")]
        public decimal Monto { get; set; }

        [Column("TipoMovimiento")]
        [StringLength(10)]
        public string TipoMovimiento { get; set; } = null!;

        [Column("Descripcion")]
        [StringLength(100)]
        public string? Descripcion { get; set; }

        [Column("SaldoAnterior")]
        public decimal? SaldoAnterior { get; set; }

        [Column("SaldoNuevo")]
        public decimal? SaldoNuevo { get; set; }

        [Column("ReferenciaExterna")]
        [StringLength(50)]
        public string? ReferenciaExterna { get; set; }

        // Navegación
        [ForeignKey("NumeroCuenta")]
        public virtual Cuenta? Cuenta { get; set; }
    }
}