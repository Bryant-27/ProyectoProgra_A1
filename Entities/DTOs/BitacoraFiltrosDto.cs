namespace Entities.DTOs
{
    public class BitacoraFiltrosDto
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public string Servicio { get; set; }
        public string Resultado { get; set; }
    }

    public class BitacoraResponseDto
    {
        public long BitacoraId { get; set; }
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Servicio { get; set; }
        public string Resultado { get; set; }
    }

    public class BitacoraPaginadaDto
    {
        public List<BitacoraResponseDto> Bitacoras { get; set; }
        public int TotalRegistros { get; set; }
    }

    public class EstadisticasBitacoraDto
    {
        public int TotalRegistros { get; set; }
        public int TotalExitosos { get; set; }
        public int TotalErrores { get; set; }
        public Dictionary<string, int> AccionesMasFrecuentes { get; set; }
        public Dictionary<string, int> ServiciosMasUsados { get; set; }
    }
}