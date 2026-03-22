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

        // GET: /Couriers/Request
        [HttpGet("/Couriers/Request")]
        public async Task<IActionResult> RequestIndex(string tab = "active")
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var query = _context.CourierRequests
                .Where(r => r.UserId == userId);

            // Logic to separate tabs if you decide to use them in the view
            if (tab == "completed")
            {
                query = query.Where(r => r.IsCompleted);
            }
            else
            {
                query = query.Where(r => !r.IsCompleted);
            }

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveTab = tab;
            return View(requests);
        }

        // POST: /Couriers/Create
        [HttpPost("/Couriers/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourierRequest request)
        {
            // Set system-controlled properties manually to prevent over-posting
            request.UserId = _userManager.GetUserId(User);
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "Pending";
            request.IsCompleted = false;

            if (ModelState.IsValid)
            {
                _context.CourierRequests.Add(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "YOUR PICKUP REQUEST HAS BEEN BROADCAST TO THE TERMINAL.";
                return RedirectToAction(nameof(RequestIndex));
            }

            // If we got here, something failed; return to the index with the model errors
            var userId = _userManager.GetUserId(User);
            var requests = await _context.CourierRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("RequestIndex", requests);
        }

        // POST: /Couriers/Cancel/5
        [HttpPost("/Couriers/Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Critical: Ensure the request belongs to the logged-in user before modifying
            var request = await _context.CourierRequests
                .FirstOrDefaultAsync(r => r.CourierRequestId == id && r.UserId == userId);

            if (request == null)
            {
                return NotFound();
            }

            // Only allow cancellation of Pending requests
            if (request.Status == "Pending")
            {
                request.Status = "Cancelled";
                request.IsCompleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "REQUEST WITHDRAWN SUCCESSFULLY.";
            }
            else
            {
                TempData["ErrorMessage"] = "CANNOT CANCEL A REQUEST THAT IS ALREADY IN PROGRESS.";
            }

            return RedirectToAction(nameof(RequestIndex));
        }
    }
}