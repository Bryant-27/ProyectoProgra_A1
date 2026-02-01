using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Estados
{
    public int IdEstado { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Afiliacion> Afiliacion { get; set; } = new List<Afiliacion>();

    public virtual ICollection<Entidades> Entidades { get; set; } = new List<Entidades>();

    public virtual ICollection<InicioSesion> InicioSesion { get; set; } = new List<InicioSesion>();

    public virtual ICollection<Parametros> Parametros { get; set; } = new List<Parametros>();

    public virtual ICollection<TablaPantallas> TablaPantallas { get; set; } = new List<TablaPantallas>();

    public virtual ICollection<TransaccionEnvio> TransaccionEnvio { get; set; } = new List<TransaccionEnvio>();

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
