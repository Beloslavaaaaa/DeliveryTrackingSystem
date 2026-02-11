using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DeliveryTrackingSystem.Models
{
    public class Rating
    {
        public int RatingId { get; set; }

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; }

        public string CourierId { get; set; }
        public IdentityUser Courier { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }
        public string Comment { get; set; }
    }
}