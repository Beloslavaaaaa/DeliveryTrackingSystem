using Cargobell.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    }
}
