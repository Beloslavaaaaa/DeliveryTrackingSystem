using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DeliveryTrackingSystem.Models
{
    public class Shipment
    {
        public int ShipmentId { get; set; }

        [Required, MaxLength(20)]
        public string TrackingCode { get; set; }

        public string SenderId { get; set; }
        public IdentityUser Sender { get; set; }

        public string ReceiverId { get; set; }
        public IdentityUser Receiver { get; set; }

        public string CourierId { get; set; }
        public IdentityUser Courier { get; set; }

        public int DeliveryRouteId { get; set; }
        public DeliveryRoute DeliveryRoute { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredDate { get; set; }

        public ICollection<StatusHistory> StatusHistory { get; set; }
        public ICollection<Rating> Ratings { get; set; }
    }
}