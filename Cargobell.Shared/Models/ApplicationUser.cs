using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Cargobell.Shared.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public DateTime? DateOfBirth { get; set; }
        public int Age
        {
            get
            {
                
                if (!DateOfBirth.HasValue) return 0;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}