using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public partial class Parametros
{
    [Key]
    [Required(ErrorMessage = "El ID del parametro es obligatorio")]
    [MaxLength(10, ErrorMessage ="El ID no puede estar vacio")]
    [RegularExpression(@"^[A-Z]+$", ErrorMessage = "El ID solo permite letras en mayuscula")]
    public string IdParametro { get; set; } = null!;

    [Required(ErrorMessage = "El valor es obligatorio")]
    [MaxLength(500, ErrorMessage = "El valor no puede estar vacio")]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío ni contener solo espacios.")]
    public string Valor { get; set; } = null!;
    
    public int? IdEstado { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual Estados? IdEstadoNavigation { get; set; }
}
