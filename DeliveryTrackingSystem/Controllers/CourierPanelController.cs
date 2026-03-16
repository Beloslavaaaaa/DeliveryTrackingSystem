using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data;
using Cargobell.Shared.Models;
using Cargobell.Data.Data;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize(Roles = "Courier")]
    [Route("Courier/Portal")]
    public class CourierPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourierPanelController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var stats = new
            {
                MyActive = await _context.Shipments.CountAsync(s => s.CourierId == userId && s.Status.Name != "Delivered"),
                Available = await _context.Shipments.CountAsync(s => s.CourierId == null && s.Status.Name == "Ready For Pickup"),
                TotalCollected = await _context.Shipments
                    .Where(s => s.CourierId == userId && s.Status.Name == "Delivered" && s.IsCashOnDelivery)
                    .SumAsync(s => s.DeliveryRoute.Price)
            };
            ViewBag.Stats = stats;
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
                    Note = $"Operational update via Courier Terminal."
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageShipments));
        }
        [HttpPost("QuickRegister")]
        public async Task<IActionResult> QuickRegister(string firstName, string lastName, string email, string phone)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phone,
                EmailConfirmed = true,
                DateOfBirth = DateTime.Now.AddYears(-20) 
            };

            var result = await _userManager.CreateAsync(user, "CargobellTemporary123!");

            if (result.Succeeded)
            {
                return Json(new { success = true, userId = user.Id, fullName = $"{user.FirstName} {user.LastName}" });
            }
            return Json(new { success = false, message = "Error creating user profile." });
        }
        [HttpGet("Users")]
        public async Task<IActionResult> UserDirectory(string searchTerm)
        {
            var courierUsers = await _userManager.GetUsersInRoleAsync("Courier");
            var courierIdList = courierUsers.Select(c => c.Id).ToList();

            var query = _userManager.Users
                .Where(u => !courierIdList.Contains(u.Id));

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
        [HttpGet("CreateShipment")]
        public async Task<IActionResult> CreateShipment(string prefillId)
        {
            ViewBag.PrefillId = prefillId;
            ViewBag.Routes = await _context.DeliveryRoutes.ToListAsync();
            ViewBag.Offices = await _context.Offices.Where(o => o.IsActive).ToListAsync(); // Load Offices

            var courierUsers = await _userManager.GetUsersInRoleAsync("Courier");
            var courierIdList = courierUsers.Select(c => c.Id).ToList();
            ViewBag.Users = await _userManager.Users.Where(u => !courierIdList.Contains(u.Id)).ToListAsync();

            return View();
        }

        [HttpPost("CreateShipment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(Shipment model)
        {
            var courier = await _userManager.GetUserAsync(User);

            model.TrackingCode = "CB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            model.CreatedAt = DateTime.UtcNow;
            model.CourierId = courier.Id;

            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "In Transit");
            model.StatusId = status?.StatusId ?? 1;

            if (ModelState.IsValid)
            {
                _context.Shipments.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageShipments", new { filter = "active" });
            }

            var courierUsers = await _userManager.GetUsersInRoleAsync("Courier");
            var courierIdList = courierUsers.Select(c => c.Id).ToList();
            ViewBag.Routes = await _context.DeliveryRoutes.ToListAsync();
            ViewBag.Users = await _userManager.Users.Where(u => !courierIdList.Contains(u.Id)).ToListAsync();
            return View(model);
        }
    }
}