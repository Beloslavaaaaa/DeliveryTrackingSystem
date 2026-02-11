using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DeliveryTrackingSystem.Data;
using DeliveryTrackingSystem.Models;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize]
    public class CouriersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CouriersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("/Couriers/Request")]
        public async Task<IActionResult> RequestIndex(string tab = "active")
        {
            var userId = _userManager.GetUserId(User);
            var requests = await _context.CourierRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveTab = tab; 
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CourierRequest request)
        {
            request.UserId = _userManager.GetUserId(User);
            request.CreatedAt = DateTime.UtcNow;

            _context.CourierRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(RequestIndex));
        }
    }
}