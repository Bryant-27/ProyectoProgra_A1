using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class RolPorPantalla
{
    public int IdRolPorPantalla { get; set; }

    public int IdPantalla { get; set; }

    public int IdRol { get; set; }

    public string? Descripcion { get; set; }

    public virtual TablaPantallas IdPantallaNavigation { get; set; } = null!;

    public virtual Roles IdRolNavigation { get; set; } = null!;
}
