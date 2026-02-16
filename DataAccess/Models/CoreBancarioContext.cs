using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models
{
    public class CoreBancarioContext : DbContext
    {
        public CoreBancarioContext(DbContextOptions<CoreBancarioContext> options)
            : base(options)
        {
        }

        // DbSet para Bitacora
        public DbSet<Bitacora> Bitacoras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la tabla Bitacora
            modelBuilder.Entity<Bitacora>(entity =>
            {
                entity.ToTable("Bitacora");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("BitacoraId")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Usuario)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Accion)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Descripcion)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FechaRegistro)
                    .HasColumnName("FechaRegistro")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Servicio)
                    .HasMaxLength(100);

                entity.Property(e => e.Resultado)
                    .HasMaxLength(20);
            });
        }
    }
}