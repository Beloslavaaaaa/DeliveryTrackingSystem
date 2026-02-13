using DeliveryTrackingSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace DeliveryTrackingSystem.ViewModels
{
    public class UserProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Shipment> Shipments { get; set; }
    }
}
