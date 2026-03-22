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

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.TrackingCode.Contains(searchTerm) ||
                                         s.Receiver.PhoneNumber.Contains(searchTerm) ||
                                         s.Receiver.FirstName.Contains(searchTerm));
            }

            return View(await query.OrderByDescending(s => s.CreatedAt).ToListAsync());
        }

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
            return RedirectToAction("Active");
        }

        [HttpGet("Users")]
        public async Task<IActionResult> UserDirectory(string searchTerm)
        {
            var usersQuery = _context.Users.AsQueryable();
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

        [HttpGet("ViewRequests/{userId}")]
        public async Task<IActionResult> ViewRequests(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            var requests = await _context.CourierRequests.Include(r => r.User)
                .Where(r => r.UserId == userId && !r.IsCompleted).ToListAsync();

            ViewBag.UserName = $"{user?.FirstName} {user?.LastName}";
            return View(requests);
        }

        [HttpPost("ApproveRequest")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            var req = await _context.CourierRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.CourierRequestId == requestId);

            if (req == null) return NotFound();

            // 1. SELF-HEALING STATUS (With Description Fix)
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit")
                         ?? await _context.Statuses.FirstOrDefaultAsync();

            if (status == null)
            {
                status = new Status
                {
                    Name = "In Transit",
                    Description = "Shipment is currently moving between nodes." // FIXED: Added non-null description
                };
                _context.Statuses.Add(status);
                await _context.SaveChangesAsync();
            }

            // 2. SELF-HEALING ROUTE
            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync();
            if (route == null)
            {
                route = new DeliveryRoute
                {
                    StartLocation = "Origin",
                    EndLocation = "Destination",
                    Price = 10.00m,
                    DistanceKm = 1.0,
                    EstimatedTimeHours = 1.0
                };
                _context.DeliveryRoutes.Add(route);
                await _context.SaveChangesAsync();
            }

            // 3. CREATE SHIPMENT
            var newShipment = new Shipment
            {
                TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CreatedAt = DateTime.UtcNow,
                CourierId = _userManager.GetUserId(User),
                SenderId = req.UserId,
                ReceiverId = req.UserId, // Placeholder FK
                StatusId = status.StatusId,
                DeliveryRouteId = route.DeliveryRouteId,
                IsFragile = false,
                IsCashOnDelivery = false,
                CodAmount = 0,
                Notes = $"[REQ #{req.CourierRequestId}] DEST: {req.DropoffAddress} | DESC: {req.PackageDescription}",
                EstimatedDelivery = DateTime.UtcNow.AddDays(2)
            };

            try
            {
                _context.Shipments.Add(newShipment);

                req.Status = "Approved";
                req.IsCompleted = true;
                _context.Entry(req).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "SYSTEM CLEARED: Shipment Active.";
                return RedirectToAction("Active");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "SQL VALIDATION ERROR: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("UserDirectory");
            }
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
        [HttpGet("CreateShipment")]
        public async Task<IActionResult> CreateShipment(string prefillId, int? requestId)
        {
            ViewBag.Offices = await _context.Offices.Where(o => o.IsActive).ToListAsync();
            ViewBag.DefaultOfficeId = await _context.Offices
                .Where(o => o.CodeName == "BASE-ZERO")
                .Select(o => o.OfficeId)
                .FirstOrDefaultAsync();

            var model = new Shipment();

            if (requestId.HasValue)
            {
                var req = await _context.CourierRequests.FindAsync(requestId.Value);
                if (req != null)
                {
                    req.Status = "Approved";
                    req.IsCompleted = true;
                    await _context.SaveChangesAsync();

                    model.SenderId = req.UserId;
                    model.Notes = $"[AUTO-PREFILL] Description: {req.PackageDescription} | To: {req.DropoffAddress}";
                }
            }
            else if (!string.IsNullOrEmpty(prefillId))
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
                TempData["SuccessMessage"] = $"MANIFEST {model.TrackingCode} AUTHORIZED.";
                return RedirectToAction("Active");
            }

            ViewBag.Offices = await _context.Offices.Where(o => o.IsActive).ToListAsync();
            return View(model);
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

        [HttpGet("GetUserByPhone")]
        public async Task<IActionResult> GetUserByPhone(string phone)
        {
            var user = await _context.Users
                .Where(u => u.PhoneNumber == phone)
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .FirstOrDefaultAsync();
            return Json(user);
        }
    }
}