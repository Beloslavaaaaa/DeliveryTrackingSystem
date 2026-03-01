using Cargobell.Shared.Models;

namespace Cargobell.Shared.ViewModels
{
    public class ShipmentRouteViewModel
    {
        public string TrackingCode { get; set; }
        public string Route { get; set; }
        public string CourierName { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
    }
}
