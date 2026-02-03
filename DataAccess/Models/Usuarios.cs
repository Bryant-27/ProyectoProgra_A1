using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Usuarios
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string TipoIdentificacion { get; set; } = null!;

    public string Identificacion { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string Usuario { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public int IdEstado { get; set; }

    public int IdRol { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual Estados IdEstadoNavigation { get; set; } = null!;

    public virtual Roles IdRolNavigation { get; set; } = null!;

    public virtual ICollection<InicioSesion> InicioSesion { get; set; } = new List<InicioSesion>();

    public virtual TiposIdentificacion TipoIdentificacionNavigation { get; set; } = null!;
}
