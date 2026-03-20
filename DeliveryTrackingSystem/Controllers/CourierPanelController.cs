using Cargobell.Data;
using Cargobell.Data.Data;
using Cargobell.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var myActive = await _context.Shipments.CountAsync(s => s.CourierId == userId && s.Status.Name != "Delivered");
            var available = await _context.Shipments.CountAsync(s => s.CourierId == null && s.Status.Name == "Ready For Pickup");
            var collected = await _context.Shipments
                .Where(s => s.CourierId == userId && s.Status.Name == "Delivered" && s.IsCashOnDelivery)
                .SumAsync(s => s.CodAmount);

            ViewBag.MyActive = myActive;
            ViewBag.Available = available;
            ViewBag.TotalCollected = collected;

            return View(user);
        }

        [HttpGet("Shipments")]
        public async Task<IActionResult> ManageShipments(string filter = "active")
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .AsQueryable();

            if (filter == "available")
                query = query.Where(s => s.CourierId == null && s.Status.Name == "Ready For Pickup");
            else
                query = query.Where(s => s.CourierId == userId && s.Status.Name != "Delivered");

            return View(await query.ToListAsync());
        }

        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(int shipmentId, string statusName)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == statusName);

            if (shipment != null && status != null)
            {
                shipment.StatusId = status.StatusId;
                _context.StatusHistories.Add(new StatusHistory
                {
                    ShipmentId = shipmentId,
                    StatusId = status.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Note = "Operational update via Courier Terminal."
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageShipments));
        }

        [HttpGet("Users")]
        public async Task<IActionResult> UserDirectory(string searchTerm)
        {
            var courierUsers = await _userManager.GetUsersInRoleAsync("Courier");
            var courierIdList = courierUsers.Select(c => c.Id).ToList();

            var query = _userManager.Users.Where(u => !courierIdList.Contains(u.Id));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToUpper();
                query = query.Where(u => u.FirstName.ToUpper().Contains(searchTerm) ||
                                         u.LastName.ToUpper().Contains(searchTerm) ||
                                         u.PhoneNumber.Contains(searchTerm) ||
                                         u.Email.ToUpper().Contains(searchTerm));
            }

            return View(await query.ToListAsync());
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

        [HttpGet("CreateShipment")]
        public async Task<IActionResult> CreateShipment(string prefillId)
        {
            ViewBag.PrefillId = prefillId;
            ViewBag.Routes = await _context.DeliveryRoutes.ToListAsync();
            return View();
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

            string identityLog = "";

            if (string.IsNullOrEmpty(model.SenderId))
            {
                identityLog += $"[SENDER: {SenderManualName ?? "UNNAMED"}] ";
            }

            if (string.IsNullOrEmpty(model.ReceiverId))
            {
                identityLog += $"[RECEIVER: {ReceiverManualName ?? "UNNAMED"}] ";
            }

            model.Notes = $"{identityLog} | Internal Notes: {Notes}";

            var initialStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit");
            model.StatusId = initialStatus?.StatusId ?? 1;

            if (ModelState.IsValid)
            {
                _context.Shipments.Add(model);
                await _context.SaveChangesAsync();

                _context.StatusHistories.Add(new StatusHistory
                {
                    ShipmentId = model.ShipmentId,
                    StatusId = model.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Note = "Manifest authorized at terminal. Cargo in transit."
                });
                await _context.SaveChangesAsync();

                return RedirectToAction("ManageShipments", new { filter = "active" });
            }

            ViewBag.Routes = await _context.DeliveryRoutes.ToListAsync();
            return View(model);
        }
    }
}