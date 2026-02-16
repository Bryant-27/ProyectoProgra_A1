using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    [Table("Bitacora")]
    public class Bitacora
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("BitacoraId")]
        public long Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Usuario { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Accion { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = null!;

        [Column("FechaRegistro")]
        public DateTime FechaRegistro { get; set; }

        [StringLength(100)]
        public string? Servicio { get; set; }

        [StringLength(20)]
        public string? Resultado { get; set; }
    }
}