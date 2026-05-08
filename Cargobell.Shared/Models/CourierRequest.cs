using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cargobell.Shared.Models
{
    public class CourierRequest
    {
        [Key]
        public int CourierRequestId { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Pickup address is required")]
        public string? PickupAddress { get; set; }

        [Required(ErrorMessage = "Dropoff address is required")]
        public string? DropoffAddress { get; set; }

        [Required(ErrorMessage = "Please describe the package contents")]
        public string? PackageDescription { get; set; }

        public string? PackageType { get; set; }
        public string? DestinationZone { get; set; }

        public bool IsFragile { get; set; }
        public bool IsCashOnDelivery { get; set; }

        public decimal? CodAmount { get; set; }
        public decimal EstimatedPrice { get; set; }

        public string? Status { get; set; } = "Pending";
        public bool IsCompleted { get; set; }

        [Required(ErrorMessage = "Please select a pickup date")]
        public DateTime PreferredPickupTime { get; set; }

        public string? PreferredPickupTimeEnd { get; set; }
        public bool IsExactTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsCustomSender { get; set; }
        public string? CustomSenderName { get; set; }
        public string? CustomSenderPhone { get; set; }
        public string? CustomSenderEmail { get; set; }
    }
}