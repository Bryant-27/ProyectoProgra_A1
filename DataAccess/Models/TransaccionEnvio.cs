using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;//llaves primarias

namespace DataAccess.Models;

public partial class TransaccionEnvio
{
    [Key]
    public int IdTransaccion { get; set; }

    public string IdEntidadOrigen { get; set; } = null!;

    public string IdEntidadDestino { get; set; } = null!;

    public string TelefonoOrigen { get; set; } = null!;

    public string? NombreOrigen { get; set; }

    public string TelefonoDestino { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public int? CodigoRespuesta { get; set; }

    public string? MensajeRespuesta { get; set; }

    public int? IdEstado { get; set; }

    [ForeignKey(nameof(IdEntidadDestino))]
    public virtual Entidades IdEntidadDestinoNavigation { get; set; } = null!;

    [ForeignKey(nameof(IdEntidadOrigen))]
    public virtual Entidades IdEntidadOrigenNavigation { get; set; } = null!;

    public virtual Estados? IdEstadoNavigation { get; set; }
}
