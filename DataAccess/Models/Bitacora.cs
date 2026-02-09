using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    [Table("Bitacora")]
    public class Bitacora
    {
        [Key]
        public int ID_Bitacora { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string Usuario { get; set; }

        [Required]
        public string Descripcion { get; set; }

        public string Detalle { get; set; }

        public string Tipo { get; set; } = "INFO"; // INFO, ERROR, WARNING

        public string Modulo { get; set; }

        public string IP_Origen { get; set; }
    }
}