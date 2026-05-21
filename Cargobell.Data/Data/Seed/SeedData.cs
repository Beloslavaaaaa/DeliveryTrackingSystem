using Cargobell.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cargobell.Data.Data.Seed
{
    public static class SeedData
    {
        public static async Task SeedOffices(ApplicationDbContext context)
        {
            if (context.Offices.Any()) return;

            var offices = new List<Office>
    {
        new Office { Name = "Central Command", CodeName = "BASE-ZERO", Address = "101 Tech Plaza", City = "Metropolis" },
        new Office { Name = "North Distribution", CodeName = "NORTH-GATE", Address = "404 Industrial Way", City = "Metropolis" },
        new Office { Name = "Port Authority Hub", CodeName = "DOCK-SIDE", Address = "Pier 17", City = "Metropolis" },
        new Office { Name = "Suburban Relay", CodeName = "OUTPOST-B", Address = "88 Pine Road", City = "Metropolis" },
        new Office { Name = "Express Sort Facility", CodeName = "RAPID-X", Address = "12 Skyway Blvd", City = "Metropolis" }
    };

            await context.Offices.AddRangeAsync(offices);
            await context.SaveChangesAsync();
        }
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string roleName = "Courier";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            var adminSettings = configuration.GetSection("AdminAccount");
            string email = adminSettings["Email"] ?? "courier@cargobell.com";
            string password = adminSettings["Password"] ?? "Courier123!";  

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var newCourier = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = "Bella",
                    LastName = "Mrnv",
                    DateOfBirth = new DateTime(2007, 2, 14),
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(newCourier, password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newCourier, roleName);
                }
            }
        
        }
    }
}
