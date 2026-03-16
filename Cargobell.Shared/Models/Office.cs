using System.ComponentModel.DataAnnotations;

namespace Cargobell.Shared.Models
{
    public class Office
    {
        [Key]
        public int OfficeId { get; set; }

        [Required]
        public string Name { get; set; } 

        [Required]
        public string CodeName { get; set; } 

        [Required]
        public string Address { get; set; }

        public string City { get; set; }

        public bool IsActive { get; set; } = true;
    }
}