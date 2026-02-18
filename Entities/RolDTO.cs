using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class RolDTO
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public List<int> Pantallas { get; set; } = new();
    }
}
