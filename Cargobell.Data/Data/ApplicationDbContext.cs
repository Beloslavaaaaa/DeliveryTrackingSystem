using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cargobell.Shared.Models;


namespace Cargobell.Data.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<DeliveryRoute> DeliveryRoutes { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<CourierRequest> CourierRequests { get; set; }
        

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Shipment>()
                .HasIndex(s => s.TrackingCode)
                .IsUnique();

            builder.Entity<Shipment>()
                .HasOne(s => s.Sender)
                .WithMany()
                .HasForeignKey(s => s.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipment>()
                .HasOne(s => s.Receiver)
                .WithMany()
                .HasForeignKey(s => s.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipment>()
                .HasOne(s => s.Courier)
                .WithMany()
                .HasForeignKey(s => s.CourierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StatusHistory>()
                .HasOne(sh => sh.Shipment)
                .WithMany(s => s.StatusHistory)
                .HasForeignKey(sh => sh.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StatusHistory>()
                .HasOne(sh => sh.Status)
                .WithMany()
                .HasForeignKey(sh => sh.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.Courier)
                .WithMany()
                .HasForeignKey(r => r.CourierId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}