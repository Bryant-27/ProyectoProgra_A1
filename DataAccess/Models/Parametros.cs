using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class Parametros
{
    [Key]
    public string IdParametro { get; set; } = null!;

    public string Valor { get; set; } = null!;

    public int? IdEstado { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual Estados? IdEstadoNavigation { get; set; }
}
