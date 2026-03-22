public class CreateShipmentViewModel
{
    public string? SenderPhone { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? SenderId { get; set; }
    public string? ReceiverId { get; set; }
    public string? SenderManualName { get; set; }
    public string? ReceiverManualName { get; set; }
    public int DeliveryRouteId { get; set; }
    public decimal CodAmount { get; set; }
    public bool IsCashOnDelivery { get; set; }
    public bool IsFragile { get; set; }
    public string? Notes { get; set; }
}