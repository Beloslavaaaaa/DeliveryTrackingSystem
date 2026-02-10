using DeliveryTrackingSystem.Data;
using DeliveryTrackingSystem.Models;
using DeliveryTrackingSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DeliveryTrackingSystem.Controllers
{
    public class ShipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShipmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Action for the Guest Search bar (GET)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(string trackingCode)
        {
            if (string.IsNullOrEmpty(trackingCode))
            {
                return RedirectToAction("Index", "Home");
            }

            var shipment = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Include(s => s.StatusHistory)
                    .ThenInclude(sh => sh.Status)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                TempData["Error"] = "Shipment not found. Please verify your tracking code.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new ShipmentTrackingViewModel
            {
                TrackingCode = shipment.TrackingCode,
                Status = shipment.Status.Name,
                Route = $"{shipment.DeliveryRoute.StartLocation} → {shipment.DeliveryRoute.EndLocation}",
                StatusHistory = shipment.StatusHistory.OrderByDescending(sh => sh.Timestamp).ToList()
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);

            var shipments = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => s.SenderId == userId || s.ReceiverId == userId || s.CourierId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(shipments);
        }

        [Authorize]
        public async Task<IActionResult> RouteMap(string trackingCode)
        {
            var shipment = await _context.Shipments
                .Include(s => s.DeliveryRoute)
                .Include(s => s.Courier)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null) return NotFound();

            var viewModel = new ShipmentRouteViewModel
            {
                TrackingCode = shipment.TrackingCode,
                CourierName = shipment.Courier?.UserName ?? "Unassigned",
                StartLocation = shipment.DeliveryRoute.StartLocation,
                EndLocation = shipment.DeliveryRoute.EndLocation
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Operator")]
        public IActionResult Create()
        {
            ViewBag.Routes = _context.DeliveryRoutes.ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Operator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Shipment shipment)
        {
            if (ModelState.IsValid)
            {
                shipment.TrackingCode = "SW-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                shipment.CreatedAt = DateTime.UtcNow;
                shipment.StatusId = _context.Statuses.First(s => s.Name == "Pending").StatusId;

                _context.Shipments.Add(shipment);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { trackingCode = shipment.TrackingCode });
            }

            ViewBag.Routes = _context.DeliveryRoutes.ToList();
            return View(shipment);
        }
    }
}