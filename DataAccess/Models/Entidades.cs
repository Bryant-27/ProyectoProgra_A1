using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;//necesario para el parche

using System.ComponentModel.DataAnnotations.Schema;//necesario para el parche
namespace DataAccess.Models;

public partial class Entidades
{

    //En esta clase las llaves foraneas hacen un conflicto para poder usar el login  asi que se aplico  un parche para probar 

    [Key]//--segundo error 
    public int IdEntidad { get; set; }

    public string NombreInstitucion { get; set; } = null!;

    public int? IdEstado { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual Estados? IdEstadoNavigation { get; set; }

    //Esto es lo que da problemas hacer identificacion de foraneas manual

    [NotMapped]
    public virtual ICollection<TransaccionEnvio> TransaccionEnvioIdEntidadDestinoNavigation { get; set; } = new List<TransaccionEnvio>();

    [NotMapped]
    public virtual ICollection<TransaccionEnvio> TransaccionEnvioIdEntidadOrigenNavigation { get; set; } = new List<TransaccionEnvio>();
}
