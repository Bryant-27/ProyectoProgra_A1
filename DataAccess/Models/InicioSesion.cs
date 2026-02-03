using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class InicioSesion
{
    public int IdSession { get; set; }

    public int IdUsuario { get; set; }

    public string? JwtToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? FechaInico { get; set; }

    public DateTime? FechaExpiracionToken { get; set; }

    public DateTime? FechaExpiracionRefresh { get; set; }

    public int? IdEstado { get; set; }

    public virtual Estados? IdEstadoNavigation { get; set; }

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}
