using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models;

public partial class Usuarios
{
    [Key]
    [Column("ID_Usuario")]
    public int IdUsuario { get; set; }

    [Column("Nombre_Completo")]
    public string NombreCompleto { get; set; } = null!;

    [Column("Tipo_Identificacion")]
    public int TipoIdentificacion { get; set; }

    public string Identificacion { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string Usuario { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    [Column("ID_Estado")]
    public int IdEstado { get; set; }

    [Column("ID_Rol")]
    public int IdRol { get; set; }

    [Column("Fecha_Creacion", TypeName = "datetime")]
    public DateTime? FechaCreacion { get; set; }


    [ForeignKey("IdEstado")]
    public virtual Estados IdEstadoNavigation { get; set; } = null!;

    [ForeignKey("IdRol")]
    public virtual Roles IdRolNavigation { get; set; } = null!;

    public virtual ICollection<InicioSesion> InicioSesion { get; set; } = new List<InicioSesion>();

    [ForeignKey("TipoIdentificacion")]
    public virtual TiposIdentificacion TipoIdentificacionNavigation { get; set; } = null!;
}
