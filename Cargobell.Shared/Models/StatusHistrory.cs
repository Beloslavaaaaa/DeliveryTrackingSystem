using System;
namespace Cargobell.Shared.Models
{
    public class StatusHistory
    {
        public int StatusHistoryId { get; set; }

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Location { get; set; }
        public string Note { get; set; } 
    }
}