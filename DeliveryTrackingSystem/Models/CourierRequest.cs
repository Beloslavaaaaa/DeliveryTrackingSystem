using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DeliveryTrackingSystem.Models
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

        public DateTime PreferredPickupTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "Pending";
    }
}