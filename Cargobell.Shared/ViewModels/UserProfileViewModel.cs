using Cargobell.Shared.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Cargobell.Shared.ViewModels
{
    public class UserProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Shipment> Shipments { get; set; }
    }
}
