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

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string cleanTerm = searchTerm.Trim().ToLower();

                query = query.Where(s =>
                    s.TrackingCode.ToLower().Contains(cleanTerm) ||

                    (s.Receiver != null && (s.Receiver.FirstName.ToLower().Contains(cleanTerm) ||
                                            s.Receiver.LastName.ToLower().Contains(cleanTerm) ||
                                            s.Receiver.PhoneNumber.Contains(cleanTerm))) ||

                    (s.Sender != null && (s.Sender.FirstName.ToLower().Contains(cleanTerm) ||
                                          s.Sender.LastName.ToLower().Contains(cleanTerm) ||
                                          s.Sender.PhoneNumber.Contains(cleanTerm))) ||

                    (s.DeliveryRoute != null && (s.DeliveryRoute.StartLocation.ToLower().Contains(cleanTerm) ||
                                                 s.DeliveryRoute.EndLocation.ToLower().Contains(cleanTerm))) ||

                    (!string.IsNullOrEmpty(s.Notes) && s.Notes.ToLower().Contains(cleanTerm))
                );
            }

            var manifests = await query.ToListAsync();
            return View(manifests);
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

        [HttpGet("CreateShipment")]
        public async Task<IActionResult> CreateShipment()
        { 
            var officesList = await _context.Offices.ToListAsync();

            ViewBag.Offices = officesList;

            return View(new CreateShipmentViewModel());
        }

        [HttpPost("CreateShipment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(CreateShipmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Offices = await _context.Offices.ToListAsync();
                return View(vm);
            }

            var initialStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit");
            int dynamicStatusId = initialStatus?.StatusId ?? 2; 

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
            if (activeRoute == null)
            {
                ModelState.AddModelError("", "Critical Error: No delivery routes found in the system database.");
                ViewBag.Offices = await _context.Offices.ToListAsync();
                return View(vm);
            }

            string structuredNotes = $"GUEST_SENDER_NAME: {vm.SenderManualName} | GUEST_SENDER_PHONE: {vm.SenderPhone} | " +
                                     $"GUEST_RECEIVER_NAME: {vm.ReceiverManualName} | GUEST_RECEIVER_PHONE: {vm.ReceiverPhone} | " +
                                     $"DESTINATION_LOCATION: {finalizedDestination} | {vm.Notes}";

            var shipment = new Shipment
            {
                TrackingCode = generatedCode,
                CourierId = _userManager.GetUserId(User),
                SenderId = string.IsNullOrWhiteSpace(vm.SenderId) ? null : vm.SenderId,
                ReceiverId = string.IsNullOrWhiteSpace(vm.ReceiverId) ? null : vm.ReceiverId,

                DeliveryRouteId = activeRoute.DeliveryRouteId,
                DeliveryRoute = activeRoute,

                StatusId = 1,
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

            user.IsApproved = false;
            user.DeclarationFilePath = "REJECTED";

            await _userManager.UpdateAsync(user);
            TempData["ErrorMessage"] = $"VERIFICATION DECLARATION REJECTED FOR PROFILE: {user.FirstName.ToUpper()}.";

            return RedirectToAction("UserDirectory");
        }

        [HttpPost("ApproveRequest")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> ApproveRequest(int requestId, int deliveryRouteId)
        {
            var request = await _context.CourierRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.CourierRequestId == requestId);
            if (request == null) return NotFound();

            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync(r => r.DeliveryRouteId == deliveryRouteId)
                        ?? await _context.DeliveryRoutes.FirstOrDefaultAsync();

            if (route == null)
            {
                TempData["ErrorMessage"] = "CRITICAL: No active delivery routes found in the tracking registry.";
                return RedirectToAction("UserDirectory");
            }

            string? senderId = request.UserId;
            string? receiverId = null;

            if (!string.IsNullOrWhiteSpace(request.ReceiverPhone))
            {
                string cleanPhone = request.ReceiverPhone.Replace("+", "").Replace(" ", "").Replace("-", "").TrimStart('0');

                var matchedReceiver = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber != null &&
                                             u.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "").EndsWith(cleanPhone));

                if (matchedReceiver != null)
                {
                    receiverId = matchedReceiver.Id;
                }
            }

            string structuredNotes = $"GUEST_SENDER_NAME: {(request.User != null ? $"{request.User.FirstName} {request.User.LastName}" : "Guest Sender")} | " +
                                     $"GUEST_SENDER_PHONE: {request.User?.PhoneNumber ?? "N/A"} | " +
                                     $"GUEST_RECEIVER_NAME: {request.ReceiverName} | " +
                                     $"GUEST_RECEIVER_PHONE: {request.ReceiverPhone} | " +
                                     $"DESTINATION_LOCATION: {request.DropoffAddress ?? "Custom Local Node"} | " +
                                     $"START_LOCATION: {route?.StartLocation ?? "Main Distribution Hub"} | " +
                                     $"DESCRIPTION: {request.PackageDescription}";

            var newShipment = new Shipment
            {
                TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                SenderId = senderId,
                ReceiverId = receiverId,
                CourierId = _userManager.GetUserId(User),
                DeliveryRouteId = route.DeliveryRouteId,
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                IsCashOnDelivery = request.IsCashOnDelivery,
                CodAmount = request.CodAmount ?? 0m,
                IsFragile = request.IsFragile,
                ShippingCost = request.EstimatedPrice,
                Notes = structuredNotes
            };

            request.Status = "Approved";
            request.IsCompleted = true;

            try
            {
                _context.Shipments.Add(newShipment);
                await _context.SaveChangesAsync();

                _context.StatusHistories.Add(new StatusHistory
                {
                    ShipmentId = newShipment.ShipmentId,
                    StatusId = newShipment.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Note = "Courier dispatch request approved. Manifest initialized via system sync.",
                    Location = "Logistics Dispatch Hub"
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"DISPATCH MANIFEST {newShipment.TrackingCode} AUTHORIZED SUCCESSFULLY.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"CRITICAL TRANSACTION FAILURE: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("UserDirectory");
            }

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
        [HttpGet("UserDetails/{id}")]
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Fetch all active or history requests linked to this user profile
            var userRequests = await _context.CourierRequests
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new UserRequestViewModel
            {
                User = user,
                RequestCount = userRequests.Count(r => !r.IsCompleted)
            };

            ViewBag.UserRequests = userRequests;
            return View(viewModel);
        }

        [HttpPost("DeleteUserProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserProfile(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var linkedRequests = _context.CourierRequests.Where(r => r.UserId == id);
            _context.CourierRequests.RemoveRange(linkedRequests);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "CRITICAL: Database identity record could not be purged.";
                return RedirectToAction("UserDetails", new { id = id });
            }

            TempData["SuccessMessage"] = "NODE PURGE COMPLETE. PROFILE CORES CLEARED.";
            return RedirectToAction("UserDirectory");
        }

        [HttpGet("History")]
        public async Task<IActionResult> PackageHistory(string searchTerm)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Shipments
        .Include(s => s.Status)
        .Include(s => s.Sender)
        .Include(s => s.Receiver)
        .Include(s => s.DeliveryRoute)
        .Where(s => s.CourierId == userId && s.Status.Name == "Delivered");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string cleanTerm = searchTerm.Trim().ToLower();

                query = query.Where(s =>
                    s.TrackingCode.ToLower().Contains(cleanTerm) ||

                    (s.Receiver != null && (s.Receiver.FirstName.ToLower().Contains(cleanTerm) ||
                                            s.Receiver.LastName.ToLower().Contains(cleanTerm) ||
                                            s.Receiver.PhoneNumber.Contains(cleanTerm))) ||

                    (s.Sender != null && (s.Sender.FirstName.ToLower().Contains(cleanTerm) ||
                                          s.Sender.LastName.ToLower().Contains(cleanTerm) ||
                                          s.Sender.PhoneNumber.Contains(cleanTerm))) ||

                    (s.DeliveryRoute != null && (s.DeliveryRoute.StartLocation.ToLower().Contains(cleanTerm) ||
                                                 s.DeliveryRoute.EndLocation.ToLower().Contains(cleanTerm))) ||

                    (!string.IsNullOrEmpty(s.Notes) && s.Notes.ToLower().Contains(cleanTerm))
                );
            }

            var archivedManifests = await query
                .OrderByDescending(s => s.DeliveredDate)
                .ToListAsync();

            return View(await query.OrderByDescending(s => s.DeliveredDate ?? s.CreatedAt).ToListAsync());
        }

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

        [HttpGet("/CourierPanel/ManifestDetails/{id}")]
        public async Task<IActionResult> ManifestDetails(int id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Include(s => s.DeliveryRoute)
                .FirstOrDefaultAsync(s => s.ShipmentId == id);

            if (shipment == null)
            {
                return NotFound();
            }

            return View(shipment);
        }

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