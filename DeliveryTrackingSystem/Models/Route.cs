using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryTrackingSystem.Models
{
    public class Route
    {
        public int RouteId { get; set; }

        [Required]
        public string StartLocation { get; set; }

        [Required]
        public string EndLocation { get; set; }

        public double DistanceKm { get; set; }
        public double EstimatedTimeHours { get; set; }

        public ICollection<Shipment> Shipments { get; set; }
    }
}
