using Cargobell.Data;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;  
using Microsoft.AspNetCore.Authorization;

namespace DeliveryTrackingSystem.Controllers
{
    public class ShipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShipmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(string trackingCode)
        {
            if (string.IsNullOrEmpty(trackingCode)) return RedirectToAction("Index", "Home");

            var shipment = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Include(s => s.StatusHistory).ThenInclude(sh => sh.Status)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                TempData["Error"] = "TRACKING CODE NOT FOUND";
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
        public async Task<IActionResult> Active()
        {
            var userId = _userManager.GetUserId(User);

            var shipments = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => (s.SenderId == userId || s.ReceiverId == userId)
                            && s.Status.Name != "Delivered"
                            && s.Status.Name != "Cancelled")
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(shipments);
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
        public async Task<IActionResult> CashOnDelivery()
        {
            var userId = _userManager.GetUserId(User);
            var pendingCod = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => (s.SenderId == userId || s.ReceiverId == userId)
                            && s.IsCashOnDelivery
                            && s.Status.Name != "Delivered"
                            && s.Status.Name != "Cancelled")
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(pendingCod);
        }
    }
}