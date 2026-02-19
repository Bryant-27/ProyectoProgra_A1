using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class RolDTO
    {
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MinLength(1, ErrorMessage = "El nombre no puede estar vacio")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "El nombre solo puede contener letras, números y espacios.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La descripcion es obligatorio")]
        [MinLength(1, ErrorMessage = "La descripcion no puede estar vacio")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "La descripcion solo puede contener letras, números y espacios.")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "Debe asignar al menos una pantalla.")]
        [MinLength(1, ErrorMessage = "Debe asignar al menos una pantalla.")]
        public List<int> Pantallas { get; set; } = new();
    }
}
