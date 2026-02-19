using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    [Table("Cuenta")]
    public class Cuenta
    {
        [Key]
        [Column("ID_Cuenta")]
        public int IdCuenta { get; set; }

        [Column("Numero_Cuenta")]
        [StringLength(22)]
        public string NumeroCuenta { get; set; } = null!;

        [Column("Identificacion_Cliente")]
        public int IdentificacionCliente { get; set; }

        [Column("Saldo")]
        public decimal Saldo { get; set; }

        [Column("ID_Estado")]
        public int? IdEstado { get; set; }

        // Navegaciones
        [ForeignKey("IdentificacionCliente")]
        public virtual ClienteBanco? Cliente { get; set; }

        // Usa CoreEstados
        [ForeignKey("IdEstado")]
        public virtual CoreEstados? Estado { get; set; }

        // Navegación a movimientos
        public virtual ICollection<MovimientoCuenta> Movimientos { get; set; } = new List<MovimientoCuenta>();
    }
}