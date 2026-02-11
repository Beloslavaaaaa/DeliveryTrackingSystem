using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryTrackingSystem.Models;
using System.Collections.Generic;
using DeliveryTrackingSystem.Models;

namespace DeliveryTrackingSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                UserName = User.Identity.Name,
                TotalSpent = 1250.50m,
                TotalEarned = 450.00m,
                RecentActions = new List<RecentAction>
                {
                    new RecentAction { Title = "Incoming Cargo #8821", Type = "Delivery To You", Amount = "$45.00", Date = "Today", Status = "In Transit" },
                    new RecentAction { Title = "Export Manifest #7712", Type = "Delivery From You", Amount = "$120.00", Date = "Yesterday", Status = "Processing" },
                    new RecentAction { Title = "COD Settlement", Type = "Payment Received", Amount = "+$350.00", Date = "2 days ago", Status = "Completed" }
                }
            };

            return View(model);
        }
    }
}