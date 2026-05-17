using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data.Data;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;

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

        // --- DASHBOARD ---
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            ViewBag.ActiveCount = await _context.Shipments
                .CountAsync(s => s.CourierId == userId && s.Status.Name != "Delivered");

            ViewBag.TotalCollected = await _context.Shipments
                .Where(s => s.CourierId == userId && s.Status.Name == "Delivered" && s.IsCashOnDelivery)
                .SumAsync(s => s.CodAmount);

            return View(user);
        }

        // --- ACTIVE CARGO & SHIPMENT MANAGEMENT ---
        [HttpGet("Active")]
        public async Task<IActionResult> ActiveCargo(string searchTerm)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Include(s => s.DeliveryRoute)
                .Where(s => s.CourierId == userId && s.Status.Name != "Delivered");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.TrackingCode.Contains(searchTerm) ||
                                         s.Receiver.PhoneNumber.Contains(searchTerm) ||
                                         s.Receiver.FirstName.Contains(searchTerm));
            }

            return View(await query.OrderByDescending(s => s.CreatedAt).ToListAsync());
        }
        [HttpGet("ShipmentDetails/{id}")]
        public async Task<IActionResult> ShipmentDetails(int id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Include(s => s.DeliveryRoute)
                .FirstOrDefaultAsync(s => s.ShipmentId == id);

            if (shipment == null) return NotFound();

            // Fetch all statuses so the dropdown uses REAL database IDs
            ViewBag.StatusList = await _context.Statuses.ToListAsync();

            return View(shipment);
        }

        [HttpPost("MarkDelivered")]
        public async Task<IActionResult> MarkDelivered(int shipmentId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "Delivered");
            if (shipment != null && status != null)
            {
                shipment.StatusId = status.StatusId;
                shipment.DeliveredDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "SHIPMENT ARCHIVED TO HISTORY.";
            }
            return RedirectToAction("ActiveCargo");
        }

        // --- RAPID IN-TAKE (NEW) ---
        [HttpGet("CreateShipment")]
        public async Task<IActionResult> CreateShipment()
        {
            ViewBag.Offices = await _context.Offices.ToListAsync();
            return View(new CreateShipmentViewModel());
        }

        [HttpPost("CreateShipment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(CreateShipmentViewModel vm)
        {
            var initialStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit");
            string generatedCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            var shipment = new Shipment
            {
                TrackingCode = generatedCode,
                CourierId = _userManager.GetUserId(User),
                SenderId = vm.SenderId,
                ReceiverId = vm.ReceiverId,
                DeliveryRouteId = vm.DeliveryRouteId,
                StatusId = initialStatus?.StatusId ?? 1,
                IsCashOnDelivery = vm.IsCashOnDelivery,
                CodAmount = vm.CodAmount,
                IsFragile = vm.IsFragile,
                CreatedAt = DateTime.UtcNow,
                Notes = $"S: {vm.SenderManualName} | R: {vm.ReceiverManualName} | {vm.Notes}"
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"MANIFEST {generatedCode} AUTHORIZED.";
            return RedirectToAction("ActiveCargo");
        }

        // --- USER & COURIER REQUEST DIRECTORY ---
        [HttpGet("Users")]
        public async Task<IActionResult> UserDirectory()
        {
            var users = await _context.Users
                .Select(u => new UserRequestViewModel
                {
                    User = u,
                    RequestCount = _context.CourierRequests.Count(r => r.UserId == u.Id && !r.IsCompleted)
                }).ToListAsync();

            ViewBag.AllRequests = await _context.CourierRequests
                .Include(r => r.User) // Fixed: Ensure it includes User navigation
                .Where(r => !r.IsCompleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        [HttpPost("ApproveRequest")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            var req = await _context.CourierRequests.FindAsync(requestId);
            if (req == null) return NotFound();

            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit");
            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync();

            var newShipment = new Shipment
            {
                TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                SenderId = req.UserId,
                ReceiverId = req.UserId,
                StatusId = status.StatusId,
                DeliveryRouteId = route.DeliveryRouteId,
                IsFragile = req.IsFragile,
                IsCashOnDelivery = true,
                CodAmount = req.EstimatedPrice,
                Notes = $"[AUTO-APPROVED] FROM: {req.PickupAddress} TO: {req.DropoffAddress}.",
                CreatedAt = DateTime.UtcNow,
                CourierId = _userManager.GetUserId(User)
            };

            _context.Shipments.Add(newShipment);
            req.IsCompleted = true;
            req.Status = "Approved";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "REQUEST CONVERTED TO ACTIVE MANIFEST.";
            return RedirectToAction("ActiveCargo");
        }

        [HttpPost("RejectRequest")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var req = await _context.CourierRequests.FindAsync(requestId);
            if (req != null)
            {
                req.Status = "Rejected";
                req.IsCompleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "REQUEST REJECTED.";
            }
            return RedirectToAction("UserDirectory");
        }

        // --- HISTORY ---
        [HttpGet("History")]
        public async Task<IActionResult> PackageHistory(string searchTerm)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Shipments
                .Include(s => s.Status).Include(s => s.Sender).Include(s => s.Receiver)
                .Where(s => s.CourierId == userId && s.Status.Name == "Delivered");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.TrackingCode.Contains(searchTerm) || s.Receiver.PhoneNumber.Contains(searchTerm));
            }

            return View(await query.OrderByDescending(s => s.DeliveredDate ?? s.CreatedAt).ToListAsync());
        }
        // --- NEW OPERATIONAL ACTIONS ---

        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(int shipmentId, int statusId, string? note)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null) return NotFound();

            shipment.StatusId = statusId;

            // Log to Status History
            var history = new StatusHistory
            {
                ShipmentId = shipmentId,
                StatusId = statusId,
                Timestamp = DateTime.UtcNow,
                Note = note ?? "Manual status update by courier."
            };

            _context.StatusHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "MANIFEST STATUS UPDATED.";
            return RedirectToAction("ShipmentDetails", new { id = shipmentId });
        }

        [HttpPost("ReportAnomaly")]
        public async Task<IActionResult> ReportAnomaly(int shipmentId, string reason)
        {
            var shipment = await _context.Shipments.Include(s => s.Status).FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
            if (shipment == null) return NotFound();

            // Look for the exact name we just inserted in SQL
            var delayedStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "Delayed");

            if (delayedStatus != null)
            {
                shipment.StatusId = delayedStatus.StatusId;
            }

            shipment.Notes += $" | [{DateTime.UtcNow:HH:mm}] ANOMALY: {reason}";

            _context.StatusHistories.Add(new StatusHistory
            {
                ShipmentId = shipmentId,
                StatusId = shipment.StatusId,
                Timestamp = DateTime.UtcNow,
                Note = "Anomaly reported: " + reason
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("ShipmentDetails", new { id = shipmentId });
        }

        // --- AJAX HELPERS ---
        [HttpGet("GetUserByPhone")]
        public async Task<IActionResult> GetUserByPhone(string phone)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null) return Json(null);
            return Json(new { id = user.Id, firstName = user.FirstName, lastName = user.LastName });
        }

        [HttpGet("GetRouteDetails")]
        public async Task<IActionResult> GetRouteDetails(int startId, int endId)
        {
            // In a production app, you'd match specific Start/End Office IDs to a Route record
            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync();
            return Json(new { id = route?.DeliveryRouteId ?? 1 });
        }

    }
}