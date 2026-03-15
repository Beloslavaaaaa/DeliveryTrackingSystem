using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Cargobell.Data.Data;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userShipments = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => s.SenderId == user.Id || s.ReceiverId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                UserName = user.Email.Split('@')[0].ToUpper(),
                TotalSpent = await _context.Shipments
                    .Where(s => s.SenderId == user.Id)
                    .SumAsync(s => s.DeliveryRoute != null ? s.DeliveryRoute.Price : 0),
                TotalEarned = await _context.Shipments
                    .Where(s => s.ReceiverId == user.Id)
                    .SumAsync(s => s.DeliveryRoute != null ? s.DeliveryRoute.Price : 0),
                RecentActions = userShipments.Select(s => new RecentAction
                {
                    Title = $"Cargo #{s.TrackingCode}",
                    Type = s.SenderId == user.Id ? "Delivery From You" : "Delivery To You",
                    Amount = s.DeliveryRoute != null ? s.DeliveryRoute.Price.ToString("C") : "$0.00",
                    Date = s.CreatedAt.ToString("MMM dd"),
                    Status = s.Status != null ? s.Status.Name : "Pending"
                }).ToList()
            };

            return View(model);
        }
    }
}