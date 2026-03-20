using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data.Data;
using Cargobell.Shared.Models;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize(Roles = "Courier")]
    [Route("CourierPanel")]
    public class CourierPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourierPanelController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            ViewBag.MyActive = await _context.Shipments.CountAsync(s => s.CourierId == userId && s.Status.Name != "Delivered");
            ViewBag.Available = await _context.Shipments.CountAsync(s => s.CourierId == null && s.Status.Name == "Ready For Pickup");
            ViewBag.TotalCollected = await _context.Shipments
                .Where(s => s.CourierId == userId && s.Status.Name == "Delivered" && s.IsCashOnDelivery)
                .SumAsync(s => s.CodAmount);

            return View(user);
        }

        [HttpGet("Shipments")]
        public async Task<IActionResult> ManageShipments(string filter)
        {
            var userId = _userManager.GetUserId(User);
            IQueryable<Shipment> query = _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Include(s => s.Receiver);

            if (filter == "active")
            {
                query = query.Where(s => s.CourierId == userId && s.Status.Name != "Delivered");
            }
            else
            {
                query = query.Where(s => s.CourierId == null);
            }

            return View("ManageShipments", await query.ToListAsync());
        }

        [HttpPost("ClaimShipment")]
        public async Task<IActionResult> ClaimShipment(int shipmentId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment != null)
            {
                shipment.CourierId = _userManager.GetUserId(User);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ASSIGNMENT CLAIMED SUCCESSFULLY.";
            }
            return RedirectToAction("ManageShipments", new { filter = "active" });
        }

        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(int shipmentId, string statusName)
        {
            var shipment = await _context.Shipments.Include(s => s.Status).FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == statusName);

            if (shipment != null && status != null)
            {
                shipment.StatusId = status.StatusId;
                _context.StatusHistories.Add(new StatusHistory
                {
                    ShipmentId = shipmentId,
                    StatusId = status.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Note = $"Status updated to {statusName}"
                });
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }

        [HttpGet("Users")]
        public async Task<IActionResult> UserDirectory(string searchTerm)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm) || u.PhoneNumber.Contains(searchTerm));
            }
            return View("UserDirectory", await query.ToListAsync());
        }

        [HttpGet("GetUserByPhone")]
        public async Task<IActionResult> GetUserByPhone(string phone)
        {
            var user = await _context.Users
                .Where(u => u.PhoneNumber == phone)
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .FirstOrDefaultAsync();
            return Json(user);
        }

        [HttpGet("GetRouteDetails")]
        public async Task<IActionResult> GetRouteDetails(int startId, int endId)
        {
            var start = await _context.Offices.FindAsync(startId);
            var end = await _context.Offices.FindAsync(endId);

            if (start == null || end == null) return NotFound();

            var route = await _context.DeliveryRoutes
                .FirstOrDefaultAsync(r => r.StartLocation == start.Name && r.EndLocation == end.Name);

            if (route == null)
            {
                route = new DeliveryRoute
                {
                    StartLocation = start.Name,
                    EndLocation = end.Name,
                    Price = startId == endId ? 5.00m : 15.00m,
                    DistanceKm = 10.0,
                    EstimatedTimeHours = 2.0
                };
                _context.DeliveryRoutes.Add(route);
                await _context.SaveChangesAsync();
            }

            return Json(new { id = route.DeliveryRouteId, display = $"{start.Name} to {end.Name}", price = route.Price });
        }

        [HttpGet("CreateShipment")]
        public async Task<IActionResult> CreateShipment(string prefillId)
        {
            ViewBag.Offices = await _context.Offices.Where(o => o.IsActive).ToListAsync();
            ViewBag.DefaultOfficeId = await _context.Offices
                .Where(o => o.CodeName == "BASE-ZERO")
                .Select(o => o.OfficeId)
                .FirstOrDefaultAsync();

            var model = new Shipment();
            if (!string.IsNullOrEmpty(prefillId))
            {
                model.SenderId = prefillId;
            }
            return View(model);
        }

        [HttpPost("CreateShipment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(Shipment model, string SenderManualName, string ReceiverManualName, bool IsFragile, string Notes)
        {
            var courier = await _userManager.GetUserAsync(User);
            model.TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            model.CreatedAt = DateTime.UtcNow;
            model.CourierId = courier.Id;
            model.IsFragile = IsFragile;
            model.Notes = $"[S: {SenderManualName}] [R: {ReceiverManualName}] | {Notes}";

            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit");
            model.StatusId = status?.StatusId ?? 1;

            if (ModelState.IsValid)
            {
                _context.Shipments.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"MANIFEST {model.TrackingCode} AUTHORIZED SUCCESSFULLY.";
                return RedirectToAction("Index");
            }

            ViewBag.Offices = await _context.Offices.Where(o => o.IsActive).ToListAsync();
            return View(model);
        }
    }
}