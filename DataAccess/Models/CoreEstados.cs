using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    [Table("Estados")]
    public class CoreEstados
    {
        [Key]
        [Column("ID_Estado")]
        public int IdEstado { get; set; }

        [Column("Nombre")]
        [StringLength(255)]
        public string Nombre { get; set; } = null!;

        [Column("Descripcion")]
        [StringLength(255)]
        public string? Descripcion { get; set; }

        [Column("Tipo_Entidad")]
        [StringLength(100)]
        public string? TipoEntidad { get; set; }

        // Relaciones con tablas del Core Bancario
        public virtual ICollection<ClienteBanco> ClientesBanco { get; set; } = new List<ClienteBanco>();
        public virtual ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    }
}