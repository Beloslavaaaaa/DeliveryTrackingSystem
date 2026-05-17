using System.Collections.Generic;
using Cargobell.Shared.Models;

namespace Cargobell.Shared.ViewModels
{
    public class ShipmentTrackingViewModel
    {
        public string TrackingCode { get; set; }
        public string Status { get; set; }
        public string Route { get; set; }
        public List<StatusHistory> StatusHistory { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public bool IsFragile { get; set; }
        public bool IsCashOnDelivery { get; set; }
        public decimal CodAmount { get; set; }
        public string? Notes { get; set; }

        // NEW: Base shipping price field
        public decimal ShippingCost { get; set; }
    }
}