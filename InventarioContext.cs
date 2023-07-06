using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Inventario;

public partial class InventarioContext : DbContext
{
    public InventarioContext()
    {
    }

    public InventarioContext(DbContextOptions<InventarioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Caja> Cajas { get; set; }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Detalle> Detalles { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductoSucursal> ProductoSucursals { get; set; }

    public virtual DbSet<Sucursal> Sucursals { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySQL("server = localhost; database=inventario; user = root; password =");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Caja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("caja");

            entity.HasIndex(e => e.Id, "id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10)")
                .HasColumnName("id");
            entity.Property(e => e.Numero)
                .HasColumnType("int(10)")
                .HasColumnName("numero");
        });

        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("categoria");

            entity.HasIndex(e => e.Id, "id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10)")
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Detalle>(entity =>
        {
            entity.HasKey(e => new { e.Productoid, e.Ventaid }).HasName("PRIMARY");

            entity.ToTable("detalle");

            entity.HasIndex(e => e.Ventaid, "FKDetalle545123");

            entity.Property(e => e.Productoid).HasColumnType("int(10)");
            entity.Property(e => e.Ventaid).HasColumnType("int(10)");
            entity.Property(e => e.Cantidad)
                .HasColumnType("int(11)")
                .HasColumnName("cantidad");

            entity.HasOne(d => d.Producto).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.Productoid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKDetalle787411");

            entity.HasOne(d => d.Venta).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.Ventaid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKDetalle545123");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("producto");

            entity.HasIndex(e => e.Categoriaid, "FKProducto906784");

            entity.HasIndex(e => e.Id, "id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10)")
                .HasColumnName("id");
            entity.Property(e => e.Categoriaid).HasColumnType("int(10)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasColumnType("int(10)")
                .HasColumnName("precio");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasForeignKey(d => d.Categoriaid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKProducto906784");
        });

        modelBuilder.Entity<ProductoSucursal>(entity =>
        {
            entity.HasKey(e => new { e.Productoid, e.Sucursalid }).HasName("PRIMARY");

            entity.ToTable("producto_sucursal");

            entity.HasIndex(e => e.Sucursalid, "FKProducto_S394446");

            entity.Property(e => e.Productoid).HasColumnType("int(10)");
            entity.Property(e => e.Sucursalid).HasColumnType("int(10)");
            entity.Property(e => e.Stock)
                .HasColumnType("int(10)")
                .HasColumnName("stock");

            entity.HasOne(d => d.Producto).WithMany(p => p.ProductoSucursals)
                .HasForeignKey(d => d.Productoid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKProducto_S155074");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.ProductoSucursals)
                .HasForeignKey(d => d.Sucursalid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKProducto_S394446");
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("sucursal");

            entity.HasIndex(e => e.Id, "id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10)")
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Ventum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("venta");

            entity.HasIndex(e => e.Cajaid, "FKVenta628219");

            entity.HasIndex(e => e.Sucursalid, "FKVenta813070");

            entity.HasIndex(e => e.Correlativo, "correlativo").IsUnique();

            entity.HasIndex(e => e.Id, "id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10)")
                .HasColumnName("id");
            entity.Property(e => e.Cajaid).HasColumnType("int(10)");
            entity.Property(e => e.Correlativo)
                .HasColumnType("int(10)")
                .HasColumnName("correlativo");
            entity.Property(e => e.Fecha)
                .HasColumnType("date")
                .HasColumnName("fecha");
            entity.Property(e => e.Sucursalid).HasColumnType("int(10)");
            entity.Property(e => e.Total)
                .HasColumnType("int(10)")
                .HasColumnName("total");

            entity.HasOne(d => d.Caja).WithMany(p => p.Venta)
                .HasForeignKey(d => d.Cajaid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKVenta628219");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Venta)
                .HasForeignKey(d => d.Sucursalid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FKVenta813070");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
