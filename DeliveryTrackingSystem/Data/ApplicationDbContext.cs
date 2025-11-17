using Microsoft.EntityFrameworkCore;
using DeliveryTrackingSystem.Models;
using System.Reflection.Emit;

namespace DeliveryTrackingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Models.Route> Routes { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .HasMany(u => u.SentShipments)
                .WithOne(s => s.Sender)
                .HasForeignKey(s => s.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>()
                .HasMany(u => u.ReceivedShipments)
                .WithOne(s => s.Receiver)
                .HasForeignKey(s => s.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>()
                .HasMany(u => u.AssignedShipments)
                .WithOne(s => s.Courier)
                .HasForeignKey(s => s.CourierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipment>()
                .HasIndex(s => s.TrackingCode)
            .IsUnique();

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
        }
    }
}
