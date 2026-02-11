using Microsoft.AspNetCore.Mvc;

namespace DeliveryTrackingSystem.Controllers
{
    public class ErrorsController : Controller
    {
        [Route("Error/404")]
        public IActionResult NotFound()
        {
            ViewData["ErrorCode"] = "404";
            ViewData["Message"] = "THE CARGO HAS VANISHED";
            ViewData["Subline"] = "The coordinates you provided do not exist in our global registry.";
            return View("ErrorPage");
        }

        [Route("Error/403")]
        public IActionResult AccessDenied()
        {
            ViewData["ErrorCode"] = "403";
            ViewData["Message"] = "RESTRICTED ACCESS";
            ViewData["Subline"] = "Your clearance level is insufficient for this sector.";
            return View("ErrorPage");
        }

        [Route("Error/500")]
        public IActionResult ServerError()
        {
            ViewData["ErrorCode"] = "500";
            ViewData["Message"] = "SYSTEM MALFUNCTION";
            ViewData["Subline"] = "Our engineers have been dispatched to resolve a terminal discrepancy.";
            return View("ErrorPage");
        }
    }
}