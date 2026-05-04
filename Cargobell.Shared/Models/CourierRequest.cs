using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Cargobell.Shared.Models
{
    public class CourierRequest
    {
        public int CourierRequestId { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string PickupAddress { get; set; }

        [Required]
        public string DropoffAddress { get; set; }

        [Required]
        public string PackageDescription { get; set; }

        // --- НОВИ ПОЛЕТА ЗА ПРЕЦИЗНОСТ ---
        [Required]
        public string PackageType { get; set; } // Envelope, Small, Standard, Heavy

        [Required]
        public string DestinationZone { get; set; } // Domestic, EU, Global

        public decimal EstimatedPrice { get; set; }
        public bool IsFragile { get; set; }
        // --------------------------------

        public DateTime PreferredPickupTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "Pending";
    }
}