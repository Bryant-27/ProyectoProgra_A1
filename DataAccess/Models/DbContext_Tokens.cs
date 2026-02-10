using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class DbContext_Tokens : DbContext
    {
        public DbContext_Tokens(DbContextOptions<DbContext_Tokens> options) : base(options)
        {
        }

        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<InicioSesion> InicioSesiones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Configuramos InicioSesion
            modelBuilder.Entity<InicioSesion>(entity =>
            {
                entity.ToTable("Inicio_Sesion"); // Nombre de la tabla
                entity.HasKey(e => e.IdSession);  // IdSession ERROR ORTOGRAFICO

                //  IGNORAR las propiedades de navegación que causan los errores de columnas extras
                //NO CAMBIAR KNG ESO DA ERROR NI USARLAS
                entity.Ignore(e => e.IdEstadoNavigation);
                entity.Ignore(e => e.IdUsuarioNavigation);

                // Mapear las columnas que sí existen para evitar confusiones
                entity.Property(e => e.IdUsuario).HasColumnName("ID_Usuario");
                entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");
            });

            // Configuraciones Usuarios
            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(u => u.IdUsuario);

                // Ignoramos la lista de inicios de sesión dentro de Usuarios para evitar conflictos
                entity.Ignore(u => u.InicioSesion);
            });
        }
    }
}
