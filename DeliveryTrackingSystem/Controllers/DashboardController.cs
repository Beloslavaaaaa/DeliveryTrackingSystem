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
            return View(user);
        }

        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
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
            if (user == null) return Challenge();

            // If the user is a Courier, send them to their specific portal
            if (await _userManager.IsInRoleAsync(user, "Courier"))
            {
                return RedirectToAction("Index", "CourierPanel");
            }

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
                    Amount = s.DeliveryRoute != null ? s.DeliveryRoute.Price.ToString("C") : "$0.00",
                    Date = s.CreatedAt.ToString("MMM dd"),
                    Status = s.Status != null ? s.Status.Name : "Pending"
                }).ToList()
            };

            return View(model);
        }
    }
}