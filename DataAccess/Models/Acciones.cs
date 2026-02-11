using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class Acciones
{
    [Key]
    public int IdAccion { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
}