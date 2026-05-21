using Cargobell.Data;
using Cargobell.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data.Data;

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrackShipment(string trackingCode)
        {
            if (string.IsNullOrEmpty(trackingCode))
            {
                TempData["Error"] = "Please enter a tracking code.";
                return RedirectToAction("Index");
            }

            var shipment = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Include(s => s.StatusHistory)
                    .ThenInclude(sh => sh.Status)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                TempData["Error"] = "Shipment not found.";
                return RedirectToAction("Index");
            }

            var viewModel = new ShipmentTrackingViewModel
            {
                TrackingCode = shipment.TrackingCode,
                Status = shipment.Status.Name,
                Route = $"{shipment.DeliveryRoute.StartLocation} → {shipment.DeliveryRoute.EndLocation}",
                StatusHistory = shipment.StatusHistory
                    .OrderByDescending(sh => sh.Timestamp)
                    .ToList()
            };

            return View("~/Views/Shipments/Details.cshtml", viewModel);
        }
        [HttpGet]
        public IActionResult About() => View();

        public IActionResult Contact() => View();
        public IActionResult Privacy() => View();
        public IActionResult Pricing() => View();
    }
}