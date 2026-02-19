using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class Roles
{
    [Key]
    public int IdRol { get; set; }
    [Required(ErrorMessage ="El nombre es obligatorio")]
    [MinLength(1, ErrorMessage ="El nombre no puede estar vacio")]
    [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "El nombre solo puede contener letras, números y espacios.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(1, ErrorMessage = "El nombre no puede estar vacio")]
    [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "La descripcion solo puede contener letras, números y espacios.")]
    public string? Descripcion { get; set; }

    public virtual ICollection<RolPorPantalla> RolPorPantalla { get; set; } = new List<RolPorPantalla>();

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}

