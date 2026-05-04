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

        // --- НОВИ МЕТОДИ ЗА УПРАВЛЕНИЕ НА ПОТРЕБИТЕЛИ ---

        [HttpPost("ApproveUser")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"USER {user.FirstName.ToUpper()} APPROVED SUCCESSFULLY.";
            }
            return RedirectToAction("UserDirectory");
        }

        [HttpPost("RejectUser")]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsApproved = false;
                user.DeclarationFilePath = "REJECTED";
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "REGISTRATION REJECTED.";
            }
            return RedirectToAction("UserDirectory");
        }

        // --- КРАЙ НА НОВИТЕ МЕТОДИ ---

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

        [HttpPost("ApproveRequest")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            var req = await _context.CourierRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.CourierRequestId == requestId);

            if (req == null) return NotFound();

            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit")
                         ?? await _context.Statuses.FirstOrDefaultAsync();

            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync();

            var newShipment = new Shipment
            {
                TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CreatedAt = DateTime.UtcNow,
                CourierId = _userManager.GetUserId(User),
                SenderId = req.UserId,
                ReceiverId = req.UserId,
                StatusId = status?.StatusId ?? 1,
                DeliveryRouteId = route?.DeliveryRouteId ?? 1,
                IsFragile = false,
                IsCashOnDelivery = false,
                CodAmount = 0,
                Notes = $"[REQ #{req.CourierRequestId}] DEST: {req.DropoffAddress}",
                EstimatedDelivery = DateTime.UtcNow.AddDays(2)
            };

            _context.Shipments.Add(newShipment);
            req.Status = "Approved";
            req.IsCompleted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "SYSTEM CLEARED: Shipment Active.";
            return RedirectToAction("Active");
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
    }
}