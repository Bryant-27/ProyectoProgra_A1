using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models;

public partial class PagosMovilesContext : DbContext
{
    public PagosMovilesContext()
    {
    }

    public PagosMovilesContext(DbContextOptions<PagosMovilesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Acciones> Acciones { get; set; }

    public virtual DbSet<Afiliacion> Afiliacion { get; set; }

    public virtual DbSet<Entidades> Entidades { get; set; }

    public virtual DbSet<Estados> Estados { get; set; }

    public virtual DbSet<InicioSesion> InicioSesion { get; set; }

    public virtual DbSet<Parametros> Parametros { get; set; }

    public virtual DbSet<RolPorPantalla> RolPorPantalla { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<TablaPantallas> TablaPantallas { get; set; }

    public virtual DbSet<TiposIdentificacion> TiposIdentificacion { get; set; }

    public virtual DbSet<TransaccionEnvio> TransaccionEnvio { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Acciones>(entity =>
        {
            entity.HasKey(e => e.IdAccion).HasName("PK__Acciones__7E770C64CA8153A9");

            entity.Property(e => e.IdAccion)
                .ValueGeneratedNever()
                .HasColumnName("ID_Accion");
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Afiliacion>(entity =>
        {
            entity.HasKey(e => e.AfiliacionId).HasName("PK__Afiliaci__CF74B2A941A3A2CF");

            entity.Property(e => e.AfiliacionId).HasColumnName("Afiliacion_ID");
            entity.Property(e => e.FechaActualizacion).HasColumnName("Fecha_Actualizacion");
            entity.Property(e => e.FechaAfiliacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Fecha_Afiliacion");
            entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");
            entity.Property(e => e.IdentificacionUsuario)
                .HasMaxLength(20)
                .HasColumnName("Identificacion_Usuario");
            entity.Property(e => e.NumeroCuenta)
                .HasMaxLength(50)
                .HasColumnName("Numero_Cuenta");
            entity.Property(e => e.Telefono).HasMaxLength(20);

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Afiliacion)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Afiliacion_Estado");
        });

        modelBuilder.Entity<Entidades>(entity =>
        {
            entity.HasKey(e => e.IdEntidad).HasName("PK__Entidade__B665B8FDA5007541");

            entity.Property(e => e.IdEntidad)
                .ValueGeneratedNever()
                .HasColumnName("ID_Entidad");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Fecha_Creacion");
            entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");
            entity.Property(e => e.NombreInstitucion)
                .HasMaxLength(255)
                .HasColumnName("Nombre_Institucion");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Entidades)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_Entidades_Estado");
        });

        modelBuilder.Entity<Estados>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PK__Estados__9CF493954396892A");

            entity.Property(e => e.IdEstado)
                .ValueGeneratedNever()
                .HasColumnName("ID_Estado");
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<InicioSesion>(entity =>
        {
            entity.HasKey(e => e.IdSession).HasName("PK__Inicio_S__90FBA2D2D19CC68D");

            entity.ToTable("Inicio_Sesion");

            entity.Property(e => e.IdSession).HasColumnName("ID_Session");
            entity.Property(e => e.FechaExpiracionRefresh).HasColumnName("Fecha_Expiracion_Refresh");
            entity.Property(e => e.FechaExpiracionToken).HasColumnName("Fecha_Expiracion_Token");
            entity.Property(e => e.FechaInico)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Fecha_Inico");
            entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");
            entity.Property(e => e.IdUsuario).HasColumnName("ID_Usuario");
            entity.Property(e => e.JwtToken).HasColumnName("JWT_Token");
            entity.Property(e => e.RefreshToken).HasColumnName("Refresh_Token");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.InicioSesion)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_Sesion_Estado");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.InicioSesion)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sesion_Usuario");
        });

        modelBuilder.Entity<Parametros>(entity =>
        {
            entity.HasKey(e => e.IdParametro).HasName("PK__Parametr__DA51B58C2F6FB865");

            entity.Property(e => e.IdParametro)
                .HasMaxLength(50)
                .HasColumnName("ID_Parametro");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Fecha_Creacion");
            entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Parametros)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_Parametros_Estado");
        });

        modelBuilder.Entity<RolPorPantalla>(entity =>
        {
            entity.HasKey(e => e.IdRolPorPantalla).HasName("PK__Rol_Por___A1D7B7796F64437D");

            entity.ToTable("Rol_Por_Pantalla");

            entity.Property(e => e.IdRolPorPantalla).HasColumnName("ID_Rol_Por_Pantalla");
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.IdPantalla).HasColumnName("ID_Pantalla");
            entity.Property(e => e.IdRol).HasColumnName("ID_Rol");

            entity.HasOne(d => d.IdPantallaNavigation).WithMany(p => p.RolPorPantalla)
                .HasForeignKey(d => d.IdPantalla)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RPP_Pantalla");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.RolPorPantalla)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RPP_Rol");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK__Roles__202AD220B7D474B6");

            entity.Property(e => e.IdRol)
                .ValueGeneratedNever()
                .HasColumnName("ID_Rol");
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<TablaPantallas>(entity =>
        {
            entity.HasKey(e => e.IdPantalla).HasName("PK__Tabla_Pa__D32AF5D924DC1BEF");

            entity.ToTable("Tabla_Pantallas");

            entity.Property(e => e.IdPantalla)
                .ValueGeneratedNever()
                .HasColumnName("ID_Pantalla");
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.Ruta).HasMaxLength(500);

            entity.HasOne(d => d.EstadoNavigation).WithMany(p => p.TablaPantallas)
                .HasForeignKey(d => d.Estado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pantallas_Estado");
        });

        modelBuilder.Entity<TiposIdentificacion>(entity =>
        {
            entity.HasKey(e => e.IdIdentificacion).HasName("PK__Tipos_Id__2D8D9EE1B1A519D8");

            entity.ToTable("Tipos_Identificacion");

            entity.HasIndex(e => e.TipoIdentificacion, "UQ__Tipos_Id__2A8DB85843AA9929").IsUnique();

            entity.Property(e => e.IdIdentificacion)
                .ValueGeneratedNever()
                .HasColumnName("ID_Identificacion");
            entity.Property(e => e.DetalleIdentificacion)
                .HasMaxLength(255)
                .HasColumnName("Detalle_Identificacion");
            entity.Property(e => e.TipoIdentificacion)
                .HasMaxLength(20)
                .HasColumnName("Tipo_Identificacion");
        });

        modelBuilder.Entity<TransaccionEnvio>(entity =>
        {
            entity.HasKey(e => e.IdTransaccion).HasName("PK__Transacc__9B541C382146F0B7");

            entity.ToTable("Transaccion_Envio");

            entity.Property(e => e.IdTransaccion).HasColumnName("ID_Transaccion");
            entity.Property(e => e.CodigoRespuesta).HasColumnName("Codigo_Respuesta");
            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.FechaEnvio).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IdEntidadDestino).HasColumnName("ID_EntidadDestino");
            entity.Property(e => e.IdEntidadOrigen).HasColumnName("ID_Entidad_Origen");
            entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");
            entity.Property(e => e.MensajeRespuesta)
                .HasMaxLength(255)
                .HasColumnName("Mensaje_Respuesta");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NombreOrigen)
                .HasMaxLength(255)
                .HasColumnName("Nombre_Origen");
            entity.Property(e => e.TelefonoDestino)
                .HasMaxLength(20)
                .HasColumnName("Telefono_Destino");
            entity.Property(e => e.TelefonoOrigen)
                .HasMaxLength(20)
                .HasColumnName("Telefono_Origen");

            entity.HasOne(d => d.IdEntidadDestinoNavigation).WithMany(p => p.TransaccionEnvioIdEntidadDestinoNavigation)
                .HasForeignKey(d => d.IdEntidadDestino)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trans_Destino");

            entity.HasOne(d => d.IdEntidadOrigenNavigation).WithMany(p => p.TransaccionEnvioIdEntidadOrigenNavigation)
                .HasForeignKey(d => d.IdEntidadOrigen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trans_Origen");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.TransaccionEnvio)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("FK_Trans_Estado");
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Usuarios__DE4431C51525D0DC");

            entity.Property(e => e.IdUsuario).HasColumnName("ID_Usuario");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Fecha_Creacion");
            entity.Property(e => e.IdEstado).HasColumnName("ID_Estado");
            entity.Property(e => e.IdRol).HasColumnName("ID_Rol");
            entity.Property(e => e.Identificacion).HasMaxLength(20);
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(255)
                .HasColumnName("Nombre_Completo");
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.TipoIdentificacion)
                .HasMaxLength(20)
                .HasColumnName("Tipo_Identificacion");
            entity.Property(e => e.Usuario).HasMaxLength(100);

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Estado");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");

            entity.HasOne(d => d.TipoIdentificacionNavigation).WithMany(p => p.Usuarios)
                .HasPrincipalKey(p => p.TipoIdentificacion)
                .HasForeignKey(d => d.TipoIdentificacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_TipoIdent");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
