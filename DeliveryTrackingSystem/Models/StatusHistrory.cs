using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryTrackingSystem.Models
{
    public class StatusHistory
    {
        public int StatusHistoryId { get; set; }

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }

        public string Location { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
