using Entities.DTOs;

namespace Proyecto_A1.Controllers
{
    internal class MovimientosResponseDto
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; }
        public List<MovimientoDto> Movimientos { get; set; }
    }
}