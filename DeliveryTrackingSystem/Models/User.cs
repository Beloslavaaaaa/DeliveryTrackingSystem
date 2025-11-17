using System.ComponentModel.DataAnnotations;
using System.Data;

namespace DeliveryTrackingSystem.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public ICollection<Shipment> SentShipments { get; set; }
        public ICollection<Shipment> ReceivedShipments { get; set; }
        public ICollection<Shipment> AssignedShipments { get; set; }
        public ICollection<Rating> Ratings { get; set; }
    }
}
