using DeliveryTrackingSystem.Data;
using DeliveryTrackingSystem.Models;
using DeliveryTrackingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

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

        // GET: /Shipments/Details/{trackingCode}
        public async Task<IActionResult> Details(string trackingCode)
        {
            if (string.IsNullOrEmpty(trackingCode))
                return RedirectToAction("Index", "Home");

            var shipment = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Include(s => s.StatusHistory.OrderByDescending(sh => sh.Timestamp))
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                TempData["Error"] = "Shipment not found";
                return RedirectToAction("Index", "Home");
            }

            var model = new ShipmentTrackingViewModel
            {
                TrackingCode = trackingCode,
                Shipment = shipment
            };

            return View(model);
        }

        // GET: /Shipments/History
        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);

            var shipments = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => s.SenderId == userId || s.ReceiverId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(shipments);
        }
        public async Task<IActionResult> RouteMap(string trackingCode)
        {
            if (string.IsNullOrEmpty(trackingCode))
                return RedirectToAction("Index", "Home");

            var shipment = await _context.Shipments
                .Include(s => s.DeliveryRoute)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
                return RedirectToAction("Index", "Home");

            return View(shipment);
        }
    }
}
