using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Entidades
{
    public int IdEntidad { get; set; }

    public string NombreInstitucion { get; set; } = null!;

    public int? IdEstado { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual Estados? IdEstadoNavigation { get; set; }

    public virtual ICollection<TransaccionEnvio> TransaccionEnvioIdEntidadDestinoNavigation { get; set; } = new List<TransaccionEnvio>();

    public virtual ICollection<TransaccionEnvio> TransaccionEnvioIdEntidadOrigenNavigation { get; set; } = new List<TransaccionEnvio>();
}
