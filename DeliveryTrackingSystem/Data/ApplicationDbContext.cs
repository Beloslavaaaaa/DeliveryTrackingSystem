using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DeliveryTrackingSystem.Models;

namespace DeliveryTrackingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<DeliveryRoute> DeliveryRoutes { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Shipment>()
                .HasIndex(s => s.TrackingCode)
                .IsUnique();

            builder.Entity<Shipment>()
                .HasOne<IdentityUser>(s => s.Sender)
                .WithMany()
                .HasForeignKey(s => s.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipment>()
                .HasOne<IdentityUser>(s => s.Receiver)
                .WithMany()
                .HasForeignKey(s => s.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipment>()
                .HasOne<IdentityUser>(s => s.Courier)
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
                .WithMany(s => s.StatusHistories)
                .HasForeignKey(sh => sh.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne<IdentityUser>(r => r.Courier)
                .WithMany()
                .HasForeignKey(r => r.CourierId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
