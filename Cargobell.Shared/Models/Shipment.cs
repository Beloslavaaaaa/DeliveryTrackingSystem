using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Cargobell.Shared.Models
{
    public class Shipment
    {
        public int ShipmentId { get; set; }

        [Required, MaxLength(20)]
        public string TrackingCode { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public string CourierId { get; set; }
        public ApplicationUser Courier { get; set; }

        public int DeliveryRouteId { get; set; }
        public DeliveryRoute DeliveryRoute { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredDate { get; set; }

        public ICollection<StatusHistory> StatusHistory { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public bool IsCashOnDelivery { get; set; }
        public decimal CodAmount { get; set; }
        public bool IsFragile { get; set; }
        public string Notes { get; set; }
    }
}