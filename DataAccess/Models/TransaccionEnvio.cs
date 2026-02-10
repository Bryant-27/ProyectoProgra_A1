using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;//llaves primarias

namespace DataAccess.Models;

public partial class TransaccionEnvio
{
    [Key]
    public int IdTransaccion { get; set; }

    public int IdEntidadOrigen { get; set; }

    public int IdEntidadDestino { get; set; }

    public string TelefonoOrigen { get; set; } = null!;

    public string? NombreOrigen { get; set; }

    public string TelefonoDestino { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public int? CodigoRespuesta { get; set; }

    public string? MensajeRespuesta { get; set; }

    public int? IdEstado { get; set; }

    public virtual Entidades IdEntidadDestinoNavigation { get; set; } = null!;

    public virtual Entidades IdEntidadOrigenNavigation { get; set; } = null!;

    public virtual Estados? IdEstadoNavigation { get; set; }
}
