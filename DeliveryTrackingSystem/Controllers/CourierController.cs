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
        public async Task<IActionResult> Create(CourierRequest courierRequest)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Твоята логика от Pricing страницата:
            decimal basePrice = courierRequest.PackageType switch
            {
                "Envelope" => 5,
                "Small" => 15,
                "Standard" => 35,
                "Heavy" => 70,
                _ => 5
            };

            decimal multiplier = courierRequest.DestinationZone switch
            {
                "Domestic" => 1,
                "EU" => 2.5m,
                "Global" => 5,
                _ => 1
            };

            courierRequest.UserId = userId;
            courierRequest.EstimatedPrice = basePrice * multiplier;
            courierRequest.Status = "Pending";

            // Премахваме навигационните свойства от проверката
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.CourierRequests.Add(courierRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(RequestIndex), new { tab = "active" });
            }

            return RedirectToAction(nameof(RequestIndex), new { tab = "create" });
        }

        [HttpPost("/Couriers/Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var request = await _context.CourierRequests
                .FirstOrDefaultAsync(r => r.CourierRequestId == id && r.UserId == userId);

            if (request != null && request.Status == "Pending")
            {
                request.Status = "Cancelled";
                request.IsCompleted = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(RequestIndex), new { tab = "finished" });
        }
    }
}