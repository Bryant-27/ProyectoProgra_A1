using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    [Table("Cliente_Banco")]
    public class ClienteBanco
    {
        [Key]
        [Column("ID_Cliente")]
        public int IdCliente { get; set; }

        [Column("Tipo_Identificacion")]
        [StringLength(50)]
        public string? TipoIdentificacion { get; set; }

        [Column("Identificacion")]
        [StringLength(20)]
        public string Identificacion { get; set; } = null!;

        [Column("Nombre_Completo")]
        [StringLength(255)]
        public string NombreCompleto { get; set; } = null!;

        [Column("ID_Estado")]
        public int? IdEstado { get; set; }

        // Navegación - Usa CoreEstados
        [ForeignKey("IdEstado")]
        public virtual CoreEstados? Estado { get; set; }

        // Navegación a cuentas
        public virtual ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    }
}