using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Entities.DTOs
{
    public class ReporteDiarioRequest
    {
        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("entidadId")]
        public string? EntidadId { get; set; } = null!;
    }
}