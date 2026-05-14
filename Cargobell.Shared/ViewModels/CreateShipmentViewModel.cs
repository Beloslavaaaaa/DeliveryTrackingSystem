namespace Cargobell.Shared.ViewModels
{
    public class CreateShipmentViewModel
    {
        // Search Helpers
        public string? SenderPhone { get; set; }
        public string? ReceiverPhone { get; set; }

        // Actual IDs for the Database
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }

        // Manual Backup
        public string? SenderManualName { get; set; }
        public string? ReceiverManualName { get; set; }

        // Logistics
        public int DeliveryRouteId { get; set; }
        public decimal CodAmount { get; set; }
        public bool IsCashOnDelivery { get; set; }
        public bool IsFragile { get; set; }
        public string? Notes { get; set; }
    }
}