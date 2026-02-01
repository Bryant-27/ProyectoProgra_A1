using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Acciones
{
    public int IdAccion { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
}
