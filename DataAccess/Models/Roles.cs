using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class Roles
{
    [Key]
    public int IdRol { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<RolPorPantalla> RolPorPantalla { get; set; } = new List<RolPorPantalla>();

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}

