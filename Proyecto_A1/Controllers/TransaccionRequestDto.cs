namespace Proyecto_A1.Controllers
{
    public class TransaccionRequestDto
    {
        public string? Identificacion { get; internal set; }
        public string? TipoMovimiento { get; internal set; }
        public int Monto { get; internal set; }
        public string? Descripcion { get; internal set; }
        public string? ReferenciaExterna { get; internal set; }
    }
}