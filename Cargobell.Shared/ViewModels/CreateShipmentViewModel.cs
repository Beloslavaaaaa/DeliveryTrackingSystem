using System.ComponentModel.DataAnnotations;

namespace Cargobell.Shared.ViewModels
{
    public class CreateShipmentViewModel
    {
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public int DeliveryRouteId { get; set; }

        public string? SenderManualName { get; set; }
        public string? ReceiverManualName { get; set; }

        [Required]
        public string SenderPhone { get; set; } = string.Empty;
        [Required]
        public string ReceiverPhone { get; set; } = string.Empty;

        public string DestinationType { get; set; } = "Hub"; // "Hub" or "Manual"
        public int? EndOfficeId { get; set; }
        public string? ManualDestinationAddress { get; set; }

        public decimal DestinationZone { get; set; } = 1.0m;
        public decimal PackageType { get; set; } = 5.0m;
        public decimal ShippingCost { get; set; }

        public bool IsCashOnDelivery { get; set; }
        public decimal CodAmount { get; set; }
        public bool IsFragile { get; set; }
        public string? Notes { get; set; }
    }
}