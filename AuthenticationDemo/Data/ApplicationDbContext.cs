using Microsoft.EntityFrameworkCore;
using AuthenticationDemo.Models;

namespace AuthenticationDemo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Email).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired();
                entity.HasMany(u => u.RefreshTokens)
                      .WithOne(rt => rt.User)
                      .HasForeignKey(rt => rt.UserId);
                entity.HasMany(u => u.Orders)
                      .WithOne(o => o.User)
                      .HasForeignKey(o => o.UserId);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.Expires).IsRequired();
                entity.Property(rt => rt.Created).IsRequired();
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.CreatedAt).IsRequired();
                entity.Property(o => o.Total).IsRequired();
                entity.HasMany(o => o.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(oi => oi.ProductId).IsRequired();
                entity.Property(oi => oi.Quantity).IsRequired();
                entity.Property(oi => oi.Price).IsRequired();
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.UserId).IsRequired();
                entity.Property(p => p.OrderId).IsRequired();
                entity.Property(p => p.RazorpayPaymentId).IsRequired();
                entity.Property(p => p.RazorpayOrderId).IsRequired();
                entity.Property(p => p.Amount).IsRequired();
                entity.Property(p => p.Currency).IsRequired();
                entity.Property(p => p.Status).IsRequired();
                entity.Property(p => p.CreatedAt).IsRequired();
                
                entity.HasIndex(p => p.RazorpayPaymentId).IsUnique();
                entity.HasIndex(p => p.RazorpayOrderId).IsUnique();
                
                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId);
                entity.HasOne(p => p.Order)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId);
            });
        }
    }
} 