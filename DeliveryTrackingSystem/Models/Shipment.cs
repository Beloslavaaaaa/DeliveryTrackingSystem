using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryTrackingSystem.Models
{
    public class Shipment
    {
        public int ShipmentId { get; set; }

        [Required, MaxLength(20)]
        public string TrackingCode { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; }

        public int ReceiverId { get; set; }
        public User Receiver { get; set; }

        public int CourierId { get; set; }
        public User Courier { get; set; }

        public int RouteId { get; set; }
        public Route Route { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredDate { get; set; }

        public ICollection<StatusHistory> StatusHistory { get; set; }
        public ICollection<Rating> Ratings { get; set; }
    }
}
