using DeliveryTrackingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTrackingSystem.Data.Seed
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            context.Database.Migrate();

            // Seed Statuses
            if (!context.Statuses.Any())
            {
                var statuses = new List<Status>
                {
                    new Status { Name = "Pending", Description = "Shipment created, not yet picked up" },
                    new Status { Name = "In Transit", Description = "Shipment is on the way" },
                    new Status { Name = "Delivered", Description = "Shipment delivered to recipient" }
                };
                context.Statuses.AddRange(statuses);
                await context.SaveChangesAsync();
            }

            // Seed Users
            var adminEmail = "admin@softuni.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new IdentityUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin123!");
            }

            var courierEmail = "courier@softuni.com";
            if (await userManager.FindByEmailAsync(courierEmail) == null)
            {
                var courier = new IdentityUser
                {
                    UserName = "courier",
                    Email = courierEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(courier, "Courier123!");
            }

            if (!context.DeliveryRoutes.Any())
            {
                var routes = new List<DeliveryRoute>
                {
                    new DeliveryRoute { StartLocation = "Sofia", EndLocation = "Plovdiv", DistanceKm = 145, EstimatedTimeHours = 2.5 },
                    new DeliveryRoute { StartLocation = "Varna", EndLocation = "Burgas", DistanceKm = 130, EstimatedTimeHours = 2 },
                };
                context.DeliveryRoutes.AddRange(routes);
                await context.SaveChangesAsync();
            }

            if (!context.Shipments.Any())
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                var courierUser = await userManager.FindByEmailAsync(courierEmail);
                var route = await context.DeliveryRoutes.FirstAsync();
                var status = await context.Statuses.FirstAsync();

                var shipment = new Shipment
                {
                    TrackingCode = "TRACK123",
                    SenderId = adminUser.Id,
                    ReceiverId = courierUser.Id,
                    CourierId = courierUser.Id,
                    DeliveryRouteId = route.DeliveryRouteId,
                    StatusId = status.StatusId,
                    CreatedAt = DateTime.UtcNow,
                    EstimatedDelivery = DateTime.UtcNow.AddDays(1)
                };

                context.Shipments.Add(shipment);
                await context.SaveChangesAsync();

                var statusHistory = new StatusHistory
                {
                    ShipmentId = shipment.ShipmentId,
                    StatusId = status.StatusId,
                    Location = "Sofia Warehouse",
                    Timestamp = DateTime.UtcNow
                };
                context.StatusHistories.Add(statusHistory);

                await context.SaveChangesAsync();
            }
        }
    }
}
