using Cargobell.Shared.Models;

namespace DeliveryTrackingSystem.Controllers
{
    public class UserRequestViewModel
    {
        public ApplicationUser User { get; set; }
        public int RequestCount { get; set; }
    }
}