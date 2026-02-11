using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class Afiliacion
{
    [Key]
    public long AfiliacionId { get; set; }

    public string NumeroCuenta { get; set; } = null!;

    public string IdentificacionUsuario { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public int IdEstado { get; set; }

    public DateTime? FechaAfiliacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Estados IdEstadoNavigation { get; set; } = null!;
}