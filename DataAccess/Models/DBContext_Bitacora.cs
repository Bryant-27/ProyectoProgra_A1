using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;//necesario para el parche

using System.ComponentModel.DataAnnotations.Schema;//necesario para el parche

namespace DataAccess.Models
{
    public class DBContext_Bitacora:DbContext
    {
        //context de la bictacora tal como es la tabla 

       public DBContext_Bitacora(DbContextOptions<DBContext_Bitacora>options):base(options) { }
        public DbSet<BitacoraMovimiento>Bitacora { get; set; }
    }

    public class BitacoraMovimiento 
    {
        [Key]
        public long BitacoraId { get; set; }
        public string Usuario { get; set; } = null!;
        public string Accion { get; set; } = null!;

        public string Descripcion { get; set; } = null!;

        public DateTime FechaRegistro { get; set; }

        public string Servicio { get; set; } = null!;

        public string Resultado { get; set; } = null!;


    }
}