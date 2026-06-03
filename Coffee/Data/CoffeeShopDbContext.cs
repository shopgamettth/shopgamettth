using System;
using System.Collections.Generic;
using Coffee.Models;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Data;

public partial class CoffeeShopDbContext : DbContext
{
    public CoffeeShopDbContext()
    {
    }

    public CoffeeShopDbContext(DbContextOptions<CoffeeShopDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }
    
    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<CardCharge> CardCharges { get; set; }

    public virtual DbSet<Game> Games { get; set; }
    public virtual DbSet<GameItemPackage> GameItemPackages { get; set; }
    public virtual DbSet<GameItemOrder> GameItemOrders { get; set; }

 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isSqlServer = providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        var isNpgsql = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD7B78CE3E0C9");

            entity.ToTable("Cart");

            entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Cart__ProductId__45F365D3");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Cart__UserId__44FF419A");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BFCDC690C");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImagePublicId).HasMaxLength(200);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFD629F690");

            var orderDateProperty = entity.Property(e => e.OrderDate);
            if (isSqlServer)
            {
                orderDateProperty
                    .HasColumnType("datetimeoffset")
                    .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            }
            else if (isNpgsql)
            {
                orderDateProperty
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }

            entity.Property(e => e.ReceiverName).HasMaxLength(100);
            entity.Property(e => e.ReceiverPhone).HasMaxLength(20);
            entity.Property(e => e.ShippingAddress).HasMaxLength(300);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Orders__UserId__49C3F6B7");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D36C4A8289BE");

            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__4CA06362");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderDeta__Produ__4D94879B");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38C457EBCC");

            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(200);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payments__OrderI__5070F446");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD96392F1C");

            entity.Property(e => e.ImagePublicId).HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.GameUsername).HasMaxLength(100);
            entity.Property(e => e.GamePassword).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__4222D4EF");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AE6AE9196");

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C30C383DF");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ_Users_UserName").IsUnique();

            entity.HasIndex(e => e.TransferCode, "UQ_Users_TransferCode").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.TransferCode).HasMaxLength(50);
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)").HasDefaultValue(0m);
            var createdAtProperty = entity.Property(e => e.CreatedAt);
            if (isSqlServer)
            {
                createdAtProperty
                    .HasColumnType("datetimeoffset")
                    .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            }
            else if (isNpgsql)
            {
                createdAtProperty
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.LockReason).HasMaxLength(200);
            entity.Property(e => e.Password).HasMaxLength(255);
            if (isSqlServer)
            {
                entity.Property(e => e.PasswordResetCodeExpiresAt).HasColumnType("datetimeoffset");
            }
            else if (isNpgsql)
            {
                entity.Property(e => e.PasswordResetCodeExpiresAt).HasColumnType("timestamp with time zone");
            }
            entity.Property(e => e.PasswordResetCodeHash).HasMaxLength(128);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UserName).HasMaxLength(15);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleId__3D5E1FD2");
        });
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImagePublicId).HasMaxLength(200);
        });

        modelBuilder.Entity<GameItemPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PackageName).HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImagePublicId).HasMaxLength(200);

            entity.HasOne(d => d.Game).WithMany(p => p.GameItemPackages)
                .HasForeignKey(d => d.GameId);
        });

        modelBuilder.Entity<GameItemOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlayerId).HasMaxLength(200);
            
            var createdAtProperty = entity.Property(e => e.CreatedAt);
            if (isSqlServer)
            {
                createdAtProperty.HasColumnType("datetimeoffset").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetimeoffset");
            }
            else if (isNpgsql)
            {
                createdAtProperty.HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone");
            }

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.GameItemPackage).WithMany(p => p.GameItemOrders)
                .HasForeignKey(d => d.GameItemPackageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
