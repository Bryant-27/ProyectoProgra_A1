using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models;

[Table("Inicio_Sesion")]
public partial class InicioSesion
{
    [Key]
    [Column("ID_Session")]// no cambiar error ortografico 
    public int IdSession { get; set; }

    [Column("ID_Usuario")]
    public int IdUsuario { get; set; }

    [Column("JWT_Token")]
    public string? JwtToken { get; set; }

    [Column("Refresh_Token")]
    public string? RefreshToken { get; set; }

    [Column("Fecha_Inico")]// no cambiar error ortografico
    public DateTime? FechaInico { get; set; }

    [Column("Fecha_Expiracion_Token")]
    public DateTime? FechaExpiracionToken { get; set; }

    [Column("Fecha_Expiracion_Refresh")]
    public DateTime? FechaExpiracionRefresh { get; set; }

    [Column("ID_Estado")]
    public int? IdEstado { get; set; }

    [NotMapped]
    public virtual Estados? IdEstadoNavigation { get; set; }
    [NotMapped]
    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}