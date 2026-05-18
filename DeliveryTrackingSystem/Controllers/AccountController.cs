using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DeliveryTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.DateOfBirth > DateTime.Now)
                {
                    ModelState.AddModelError("DateOfBirth", "Date of birth cannot be in the future.");
                    return View(model);
                }

                var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "This phone number is already registered.");
                    return View(model);
                }

                var today = DateTime.Today;
                var age = today.Year - model.DateOfBirth.Year;
                if (model.DateOfBirth.Date > today.AddYears(-age)) age--;

                string? savedFileName = null;

                if (age < 18)
                {
                    if (model.DeclarationFile == null)
                    {
                        ModelState.AddModelError("DeclarationFile", "Minors must upload a parental declaration.");
                        return View(model);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "declarations");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    savedFileName = Guid.NewGuid().ToString() + "_" + model.DeclarationFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, savedFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.DeclarationFile.CopyToAsync(fileStream);
                    }
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber,
                    DeclarationFilePath = savedFileName,
                    IsApproved = (age >= 18)
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!user.IsApproved)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("WaitingApproval");
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult WaitingApproval() => View();

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var isCourier = await _userManager.IsInRoleAsync(user, "Courier");

                    if (!isCourier && (!user.IsApproved && !string.IsNullOrEmpty(user.DeclarationFilePath)))
                    {
                        await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                        return RedirectToAction("WaitingApproval");
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (user != null && await _userManager.IsInRoleAsync(user, "Courier"))
                    {
                        return RedirectToAction("Index", "CourierPanel");
                    }

                    return RedirectToAction("Index", "Dashboard");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}