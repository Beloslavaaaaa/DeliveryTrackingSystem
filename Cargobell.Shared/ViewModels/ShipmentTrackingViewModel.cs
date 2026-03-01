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
    }
}