using DeliveryTrackingSystem.Data;
using DeliveryTrackingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);

            var shipments = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => s.SenderId == userId || s.ReceiverId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var model = new ViewModels.UserProfileViewModel
            {
                User = await _userManager.GetUserAsync(User),
                Shipments = shipments
            };

            return View(model);
        }
    }
}
