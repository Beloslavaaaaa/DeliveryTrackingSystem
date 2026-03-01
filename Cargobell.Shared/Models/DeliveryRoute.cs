namespace Cargobell.Shared.Models
{
    public class DeliveryRoute
    {
        public int DeliveryRouteId { get; set; }
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public decimal Price { get; set; }
        public double DistanceKm { get; set; }
        public double EstimatedTimeHours { get; set; }
    }
}