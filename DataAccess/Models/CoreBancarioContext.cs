using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models
{
    public class CoreBancarioContext : DbContext
    {
        public CoreBancarioContext(DbContextOptions<CoreBancarioContext> options)
            : base(options)
        {
        }

        public DbSet<CoreEstados> CoreEstados { get; set; }
        public DbSet<ClienteBanco> ClientesBanco { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<MovimientoCuenta> MovimientosCuenta { get; set; }
        public object ClienteBanco { get; set; }
        public IEnumerable<object> MovimientoCuenta { get; set; }
        public object Clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de CoreEstados
            modelBuilder.Entity<CoreEstados>(entity =>
            {
                entity.HasKey(e => e.IdEstado);
                entity.ToTable("Estados");

                entity.Property(e => e.Nombre)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(255);

                entity.Property(e => e.TipoEntidad)
                    .HasMaxLength(100);
            });

            // Configuración de ClienteBanco
            modelBuilder.Entity<ClienteBanco>(entity =>
            {
                entity.HasKey(e => e.IdCliente);
                entity.ToTable("Cliente_Banco");

                entity.Property(e => e.Identificacion)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.NombreCompleto)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasOne(e => e.Estado)
                    .WithMany(e => e.ClientesBanco)
                    .HasForeignKey(e => e.IdEstado)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Cuenta
            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.HasKey(e => e.NumeroCuenta);
                entity.ToTable("Cuenta");

                entity.Property(e => e.NumeroCuenta)
                    .HasMaxLength(22)
                    .IsRequired();

                entity.Property(e => e.Saldo)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Cuentas)
                    .HasForeignKey(e => e.IdentificacionCliente)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Estado)
                    .WithMany(e => e.Cuentas)
                    .HasForeignKey(e => e.IdEstado)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de MovimientoCuenta
            modelBuilder.Entity<MovimientoCuenta>(entity =>
            {
                entity.HasKey(e => e.MovimientoId);
                entity.ToTable("Movimiento_Cuenta");

                entity.Property(e => e.FechaMovimiento)
                    .HasDefaultValueSql("GETDATE()")
                    .IsRequired();

                entity.Property(e => e.Monto)
                    .HasPrecision(12, 2)
                    .IsRequired();

                entity.Property(e => e.TipoMovimiento)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(100);

                entity.Property(e => e.SaldoAnterior)
                    .HasPrecision(12, 2);

                entity.Property(e => e.SaldoNuevo)
                    .HasPrecision(12, 2);

                entity.Property(e => e.ReferenciaExterna)
                    .HasMaxLength(50);

                entity.HasOne(e => e.Cuenta)
                    .WithMany(c => c.Movimientos)
                    .HasForeignKey(e => e.NumeroCuenta)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}