using Cargobell.Shared.Models;

namespace DeliveryTrackingSystem.Controllers
{
    public class UserRequestViewModel
    {
        public ApplicationUser User { get; set; }
        public int RequestCount { get; set; }
        public bool IsWaitingApproval => !User.IsApproved && !string.IsNullOrEmpty(User.DeclarationFilePath);
    }
}