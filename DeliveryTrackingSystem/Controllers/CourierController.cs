using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;
using Cargobell.Data.Data;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize]
    public class CouriersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CouriersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var request = await _context.CourierRequests
                .FirstOrDefaultAsync(r => r.CourierRequestId == id && r.UserId == userId);

            if (request == null) return NotFound();

            if (request.Status == "Pending")
            {
                request.Status = "Cancelled";
                request.IsCompleted = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(RequestIndex));
        }
    }
}