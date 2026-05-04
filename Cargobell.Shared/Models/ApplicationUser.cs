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

        // FIXED: New fields for minor safety
        public string? DeclarationFilePath { get; set; }
        public bool IsApproved { get; set; } = true; // Default to true for adults, we will flip it for minors

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