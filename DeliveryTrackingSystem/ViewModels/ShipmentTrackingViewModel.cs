using DeliveryTrackingSystem.Models;

namespace DeliveryTrackingSystem.ViewModels
{
    public class ShipmentTrackingViewModel
    {
        public string TrackingCode { get; set; }
        public Shipment Shipment { get; set; }
    }
}
