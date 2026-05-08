using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data.Data;
using Cargobell.Shared.Models;

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
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var requests = await _context.CourierRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveTab = tab;
            return View(requests);
        }

        [HttpPost("/Couriers/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourierRequest request, string TimeStart)
        {
            var userId = _userManager.GetUserId(User);
            request.UserId = userId;
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "Pending";

            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Status");
            // Remove custom fields from validation if not being used
            if (!request.IsCustomSender)
            {
                ModelState.Remove("CustomSenderName");
                ModelState.Remove("CustomSenderPhone");
            }

            // Pricing logic
            decimal.TryParse(request.PackageType, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal baseP);
            decimal.TryParse(request.DestinationZone, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal mult);
            request.EstimatedPrice = (baseP * mult) + (request.IsFragile ? 5.00m : 0);

            if (!string.IsNullOrEmpty(TimeStart) && TimeSpan.TryParse(TimeStart, out var ts))
            {
                request.PreferredPickupTime = request.PreferredPickupTime.Date.Add(ts);
            }

            if (ModelState.IsValid)
            {
                _context.CourierRequests.Add(request);
                await _context.SaveChangesAsync();
                return RedirectToAction("RequestIndex", new { tab = "active" });
            }

            // If invalid, we MUST reload the lists so the other tabs aren't empty
            ViewBag.ActiveTab = "create";
            var allRequests = await _context.CourierRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("RequestIndex", allRequests);
        }

        [HttpPost("/Couriers/Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var request = await _context.CourierRequests
                .FirstOrDefaultAsync(r => r.CourierRequestId == id && r.UserId == userId);

            if (request != null && (request.Status == "Pending" || request.Status == "Active"))
            {
                request.Status = "Cancelled";
                request.IsCompleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("RequestIndex", new { tab = "finished" });
        }
    }
}