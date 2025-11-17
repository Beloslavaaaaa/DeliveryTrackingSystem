using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryTrackingSystem.Models
{
    public class Rating
    {
        public int RatingId { get; set; }

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; }

        public int CourierId { get; set; }
        public User Courier { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }

        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
