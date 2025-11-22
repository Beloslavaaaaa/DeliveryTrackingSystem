using DeliveryTrackingSystem.Data;
using DeliveryTrackingSystem.Models;
using DeliveryTrackingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DeliveryTrackingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TrackShipment(string trackingCode)
        {
            if (string.IsNullOrEmpty(trackingCode))
                return RedirectToAction("Index");

            var shipment = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Include(s => s.StatusHistory)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                TempData["Error"] = "Shipment not found";
                return RedirectToAction("Index");
            }

            var model = new ShipmentTrackingViewModel
            {
                TrackingCode = trackingCode,
                Shipment = shipment
            };

            return RedirectToAction("Details", "Shipments", new { trackingCode });
        }
    }
}
