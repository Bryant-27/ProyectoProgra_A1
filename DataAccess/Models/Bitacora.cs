using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("Bitacora")]
    public class Bitacora
    {
        [Key]
        [Column("BitacoraId")]
        public long Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario { get; set; }

        [Required]
        [StringLength(100)]
        public string Accion { get; set; }

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; }

        [Column("FechaRegistro")]
        public DateTime FechaRegistro { get; set; }

        [StringLength(100)]
        public string Servicio { get; set; }

        [StringLength(20)]
        public string Resultado { get; set; }
    }
}