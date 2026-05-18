using System.ComponentModel.DataAnnotations;

namespace Cargobell.Shared.Models
{
    public class Status
    {
        public int StatusId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public ICollection<StatusHistory> StatusHistories { get; set; }
    }
}
