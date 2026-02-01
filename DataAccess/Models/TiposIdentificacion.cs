using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class TiposIdentificacion
{
    public int IdIdentificacion { get; set; }

    public string TipoIdentificacion { get; set; } = null!;

    public string DetalleIdentificacion { get; set; } = null!;

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
