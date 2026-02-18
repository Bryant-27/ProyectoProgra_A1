using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class TablaPantallas
{
    [Key]
    public int IdPantalla { get; set; }

    [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "El nombre solo puede contener letras, números y espacios.")]
    public string Nombre { get; set; } = null!;

    [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "La descripción solo puede contener letras, números y espacios.")]
    public string Descripcion { get; set; } = null!;

    public string Ruta { get; set; } = null!;

    public int Estado { get; set; }

    public virtual Estados EstadoNavigation { get; set; } = null!;

    public virtual ICollection<RolPorPantalla> RolPorPantalla { get; set; } = new List<RolPorPantalla>();
}

