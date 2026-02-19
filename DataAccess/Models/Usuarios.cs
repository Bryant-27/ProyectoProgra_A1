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

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Solo letras y espacios")]
    [Column("Nombre_Completo")]
    public string NombreCompleto { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [Column("Tipo_Identificacion")]
    public string TipoIdentificacion { get; set; } = null!;

    [Required(ErrorMessage = "La identificacion del usuario es obligatorio")]
    public string Identificacion { get; set; } = null!;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string Telefono { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string Usuario { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string Contraseña { get; set; } = null!;

    [Column("ID_Estado")]
    public int IdEstado { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [Column("ID_Rol")]
    public int IdRol { get; set; }

    [Column("Fecha_Creacion", TypeName = "datetime")]
    public DateTime? FechaCreacion { get; set; }

    // ===== PROPIEDADES DE NAVEGACIÓN - TODAS CON [NOTMAPPED] =====
    [NotMapped]
    [ForeignKey("IdEstado")]
    public virtual Estados? IdEstadoNavigation { get; set; }

    [NotMapped]
    [ForeignKey("IdRol")]
    public virtual Roles? IdRolNavigation { get; set; }

    [NotMapped]
    public virtual ICollection<InicioSesion> InicioSesion { get; set; } = new List<InicioSesion>();

    [NotMapped]
    [ForeignKey("TipoIdentificacion")]
    public virtual TiposIdentificacion? TipoIdentificacionNavigation { get; set; }
}