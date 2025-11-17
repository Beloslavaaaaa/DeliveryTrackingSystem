using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryTrackingSystem.Models
{
    public class Status
    {
        public int StatusId { get; set; }

        [Required]
        public string Name { get; set; } 

        public string Description { get; set; }

        public ICollection<Shipment> Shipments { get; set; }
        public ICollection<StatusHistory> StatusHistories { get; set; }
    }
}
