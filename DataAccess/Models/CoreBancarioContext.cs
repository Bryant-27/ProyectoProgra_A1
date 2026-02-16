using Microsoft.EntityFrameworkCore;

namespace Models
{
    public class CoreBancarioContext : DbContext
    {
        public CoreBancarioContext(DbContextOptions<CoreBancarioContext> options)
            : base(options)
        {
        }

        public DbSet<Bitacora> Bitacoras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Bitacora>(entity =>
            {
                entity.ToTable("Bitacora");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}