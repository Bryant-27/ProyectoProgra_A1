using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;//necesario para el parche

using System.ComponentModel.DataAnnotations.Schema;//necesario para el parche
namespace DataAccess.Models;

public partial class Entidades
{

    //En esta clase las llaves foraneas hacen un conflicto para poder usar el login  asi que se aplico  un parche para probar 

    [Key]//--segundo error 

    [Required(ErrorMessage = "El ID de la entidad es obligatorio")]
    public string IdEntidad { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de la institucion es obligatorio")]
    [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$",ErrorMessage = "El nombre solo puede contener letras y espacios.")]
    public string NombreInstitucion { get; set; } = null!;

    [Required(ErrorMessage ="El ID del estado es requerido")]
    public int? IdEstado { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual Estados? IdEstadoNavigation { get; set; }

    //Esto es lo que da problemas hacer identificacion de foraneas manual

    public virtual ICollection<TransaccionEnvio> TransaccionEnvioIdEntidadDestinoNavigation { get; set; } = new List<TransaccionEnvio>();

    public virtual ICollection<TransaccionEnvio> TransaccionEnvioIdEntidadOrigenNavigation { get; set; } = new List<TransaccionEnvio>();
}
