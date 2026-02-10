using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataAccess.Models;

public partial class Estados
{
    [Key]
    public int IdEstado { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<Afiliacion> Afiliacion { get; set; } = new List<Afiliacion>();
    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<Entidades> Entidades { get; set; } = new List<Entidades>();
    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<InicioSesion> InicioSesion { get; set; } = new List<InicioSesion>();
    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<Parametros> Parametros { get; set; } = new List<Parametros>();
    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<TablaPantallas> TablaPantallas { get; set; } = new List<TablaPantallas>();
    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<TransaccionEnvio> TransaccionEnvio { get; set; } = new List<TransaccionEnvio>();
    [JsonIgnore]
    [NotMapped]
    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
