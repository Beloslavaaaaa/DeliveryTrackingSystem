using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cargobell.Data;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Cargobell.Data.Data;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // FIXED: Check approval status for profile access
            if (!user.IsApproved) return RedirectToAction("WaitingApproval", "Account");

            return View(user);
        }

        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // FIXED: Check approval status for settings access
            if (!user.IsApproved) return RedirectToAction("WaitingApproval", "Account");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model, string NewEmail, string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            ModelState.Remove("DateOfBirth");
            ModelState.Remove("Age");

            var passwordCheck = await _userManager.CheckPasswordAsync(user, CurrentPassword);
            if (!passwordCheck)
            {
                ModelState.AddModelError(string.Empty, "Current password verification failed.");
                return View("Settings", user);
            }

            if (!string.IsNullOrEmpty(NewPassword))
            {
                if (NewPassword != ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "New password confirmation does not match.");
                }
            }

            if (!ModelState.IsValid) return View("Settings", user);

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrEmpty(NewEmail) && user.Email != NewEmail)
            {
                user.Email = NewEmail;
                user.UserName = NewEmail;
                user.NormalizedEmail = NewEmail.ToUpper();
                user.NormalizedUserName = NewEmail.ToUpper();
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    var passResult = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
                    if (!passResult.Succeeded)
                    {
                        foreach (var err in passResult.Errors) ModelState.AddModelError(string.Empty, err.Description);
                        return View("Settings", user);
                    }
                }

                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Profile");
            }

            return View("Settings", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Profile", new { message = "Password Updated" });
            }

            return View("Settings", user);
        }

        [HttpGet("/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // FIXED: If the user is under 18 and not yet approved, send them to the waiting page
            if (!user.IsApproved)
            {
                return RedirectToAction("WaitingApproval", "Account");
            }

            // If the user is a Courier, send them to their specific portal
            if (await _userManager.IsInRoleAsync(user, "Courier"))
            {
                return RedirectToAction("Index", "CourierPanel");
            }

            // --- LOGIC FOR SHIPMENT COUNT ---
            // We count shipments where the user is involved AND the status is not 'Delivered'
            var activeShipmentsCount = await _context.Shipments
                .Include(s => s.Status)
                .Where(s => (s.SenderId == user.Id || s.ReceiverId == user.Id) &&
                            (s.Status == null || s.Status.Name != "Delivered"))
                .CountAsync();

            var userShipments = await _context.Shipments
                .Include(s => s.Status)
                .Include(s => s.DeliveryRoute)
                .Where(s => s.SenderId == user.Id || s.ReceiverId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                UserName = (!string.IsNullOrEmpty(user.FirstName) ? user.FirstName : user.Email.Split('@')[0]).ToUpper(),

                // ASSIGN THE COUNT HERE
                ActiveShipmentsCount = activeShipmentsCount,

                TotalSpent = await _context.Shipments
                    .Where(s => s.SenderId == user.Id)
                    .SumAsync(s => s.DeliveryRoute != null ? s.DeliveryRoute.Price : 0),
                TotalEarned = await _context.Shipments
                    .Where(s => s.ReceiverId == user.Id)
                    .SumAsync(s => s.DeliveryRoute != null ? s.DeliveryRoute.Price : 0),
                RecentActions = userShipments.Select(s => new RecentAction
                {
                    Title = $"Cargo #{s.TrackingCode}",
                    Type = s.SenderId == user.Id ? "Delivery From You" : "Delivery To You",
                    // FIXED: Explicitly formats currency to Euros (€) instead of falling back to system default (Leva)
                    Amount = s.DeliveryRoute != null ? $"€{s.CodAmount.ToString("N2")}" : "€0.00",
                    Date = s.CreatedAt.ToString("MMM dd"),
                    Status = s.Status != null ? s.Status.Name : "Pending"
                }).ToList()
            };

            return View(model);
        }
        [HttpPost("ApproveUser")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsApproved = true;
                // Можем да изтрием файла след одобрение, за да пестим място, или да го пазим за архив
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"USER {user.FirstName} {user.LastName} HAS BEEN ACTIVATED.";
            }
            return RedirectToAction("UserDirectory");
        }

        [HttpPost("RejectUser")]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Маркираме го като отхвърлен. 
                // Тук можем да ползваме специално поле или просто да изтрием пътя към файла, 
                // за да знае системата, че е отказан.
                user.IsApproved = false;
                user.DeclarationFilePath = "REJECTED"; // Сигнал за Front-end-а
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "REGISTRATION DENIED.";
            }
            return RedirectToAction("UserDirectory");
        }
    }
}