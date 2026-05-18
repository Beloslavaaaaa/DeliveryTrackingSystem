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

            ViewBag.TotalShippingRevenue = await _context.Shipments
                .Where(s => s.CourierId == userId && s.Status.Name == "Delivered")
                .SumAsync(s => s.ShippingCost);

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

        // --- RAPID IN-TAKE ---
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

            string finalizedDestination = "Unassigned Sector Location";
            if (vm.DestinationType == "Hub" && vm.EndOfficeId.HasValue)
            {
                var targetOffice = await _context.Offices.FindAsync(vm.EndOfficeId.Value);
                if (targetOffice != null) finalizedDestination = $"Office Hub: {targetOffice.Name}";
            }
            else if (!string.IsNullOrEmpty(vm.ManualDestinationAddress))
            {
                finalizedDestination = vm.ManualDestinationAddress;
            }

            var activeRoute = await _context.DeliveryRoutes.FirstOrDefaultAsync();
            int routeId = activeRoute?.DeliveryRouteId ?? 1;

            string structuredNotes = $"GUEST_SENDER_NAME: {vm.SenderManualName} | GUEST_SENDER_PHONE: {vm.SenderPhone} | " +
                                     $"GUEST_RECEIVER_NAME: {vm.ReceiverManualName} | GUEST_RECEIVER_PHONE: {vm.ReceiverPhone} | " +
                                     $"DESTINATION_LOCATION: {finalizedDestination} | {vm.Notes}";

            var shipment = new Shipment
            {
                TrackingCode = generatedCode,
                CourierId = _userManager.GetUserId(User),
                SenderId = vm.SenderId,
                ReceiverId = vm.ReceiverId,
                DeliveryRouteId = routeId,
                StatusId = initialStatus?.StatusId ?? 1,
                IsCashOnDelivery = vm.IsCashOnDelivery,
                CodAmount = vm.CodAmount,
                IsFragile = vm.IsFragile,
                CreatedAt = DateTime.UtcNow,
                ShippingCost = vm.ShippingCost,
                Notes = structuredNotes
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            _context.StatusHistories.Add(new StatusHistory
            {
                ShipmentId = shipment.ShipmentId,
                StatusId = shipment.StatusId,
                Timestamp = DateTime.UtcNow,
                Note = "Manifest registered via Rapid In-Take Desk.",
                Location = finalizedDestination
            });
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
                .Include(r => r.User)
                .Where(r => !r.IsCompleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        // --- RESTORED NODE VERIFICATION METHODS ---
        [HttpPost("ApproveUser")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsApproved = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"NODE ACCREDITATION GRANTED: {user.FirstName} {user.LastName} IS NOW VERIFIED.";
            }
            else
            {
                TempData["ErrorMessage"] = "CRITICAL error updating authorization record parameters.";
            }

            return RedirectToAction("UserDirectory");
        }

        [HttpPost("RejectUser")]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Flags signature path to REJECTED to cleanly clear the card layout status block state
            user.IsApproved = false;
            user.DeclarationFilePath = "REJECTED";

            await _userManager.UpdateAsync(user);
            TempData["ErrorMessage"] = $"VERIFICATION DECLARATION REJECTED FOR PROFILE: {user.FirstName.ToUpper()}.";

            return RedirectToAction("UserDirectory");
        }

        // --- MANIFEST DISPATCH METHODS ---
        [HttpPost("ApproveRequest")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> ApproveRequest(int requestId, int deliveryRouteId)
        {
            var request = await _context.CourierRequests.FirstOrDefaultAsync(r => r.CourierRequestId == requestId);
            if (request == null) return NotFound();

            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync(r => r.DeliveryRouteId == deliveryRouteId);
            if (route == null)
            {
                route = await _context.DeliveryRoutes.FirstOrDefaultAsync();
                if (route == null)
                {
                    TempData["ErrorMessage"] = "CRITICAL: No delivery routes found in the database.";
                    return RedirectToAction("UserDirectory");
                }
            }

            string? senderId = request.UserId;
            var matchedReceiver = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.ReceiverPhone);

            var newShipment = new Shipment
            {
                TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                SenderId = senderId,
                ReceiverId = matchedReceiver?.Id,
                CourierId = _userManager.GetUserId(User),
                DeliveryRouteId = route.DeliveryRouteId,
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                IsCashOnDelivery = request.IsCashOnDelivery,
                CodAmount = request.CodAmount ?? 0m,
                IsFragile = request.IsFragile,
                ShippingCost = request.EstimatedPrice,
                Notes = $"DROP-OFF RECIPIENT: {request.ReceiverName} ({request.ReceiverPhone}). DESCRIPTION: {request.PackageDescription}"
            };

            request.Status = "Approved";
            request.IsCompleted = true;

            _context.Shipments.Add(newShipment);
            await _context.SaveChangesAsync();

            _context.StatusHistories.Add(new StatusHistory
            {
                ShipmentId = newShipment.ShipmentId,
                StatusId = newShipment.StatusId,
                Timestamp = DateTime.UtcNow,
                Note = "Courier dispatch request approved. Manifest initialized.",
                Location = "Logistics Dispatch Hub"
            });
            await _context.SaveChangesAsync();

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

        // --- OPERATIONAL ACTIONS ---
        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(int shipmentId, int statusId, string? note)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null) return NotFound();

            var statusExists = await _context.Statuses.AnyAsync(s => s.StatusId == statusId);
            if (!statusExists)
            {
                TempData["ErrorMessage"] = "System mismatch: Selected status ID is invalid.";
                return RedirectToAction("ShipmentDetails", new { id = shipmentId });
            }

            shipment.StatusId = statusId;

            var history = new StatusHistory
            {
                ShipmentId = shipmentId,
                StatusId = statusId,
                Timestamp = DateTime.UtcNow,
                Note = note ?? "Manual status update.",
                Location = "In Transit / Courier Terminal"
            };

            _context.StatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return RedirectToAction("ShipmentDetails", new { id = shipmentId });
        }

        [HttpPost("ReportAnomaly")]
        public async Task<IActionResult> ReportAnomaly(int shipmentId, string reason)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            var delayedStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "Delayed");

            if (shipment != null && delayedStatus != null)
            {
                shipment.StatusId = delayedStatus.StatusId;

                if (string.IsNullOrEmpty(shipment.Notes))
                {
                    shipment.Notes = $"[DELAY ALERT]: {reason} ({DateTime.UtcNow:MMM dd, HH:mm} UTC)";
                }
                else
                {
                    shipment.Notes += $" | [DELAY ALERT]: {reason} ({DateTime.UtcNow:MMM dd, HH:mm} UTC)";
                }

                _context.StatusHistories.Add(new StatusHistory
                {
                    ShipmentId = shipmentId,
                    StatusId = delayedStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Note = "ANOMALY DETECTED: " + reason,
                    Location = "Reported by Courier"
                });

                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "ANOMALY LOGGED. PUBLIC MANIFEST UPDATED.";
            }
            return RedirectToAction("ShipmentDetails", new { id = shipmentId });
        }

        // --- AJAX HELPERS ---
        [HttpGet("GetUserByPhone")]
        public async Task<IActionResult> GetUserByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Json(new List<object>());

            string cleanPhone = phone.Replace("+", "").Replace(" ", "").Replace("-", "").TrimStart('0');

            if (cleanPhone.Length < 6) return Json(new List<object>());

            var matchedUsers = await _context.Users
                .Where(u => u.PhoneNumber != null && u.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "").EndsWith(cleanPhone))
                .Select(u => new {
                    id = u.Id,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    phoneNumber = u.PhoneNumber
                })
                .ToListAsync();

            return Json(matchedUsers);
        }

        [HttpGet("GetRouteDetails")]
        public async Task<IActionResult> GetRouteDetails(int startId, int endId)
        {
            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync();
            return Json(new { id = route?.DeliveryRouteId ?? 1, price = route?.Price ?? 0.00m });
        }
    }
}