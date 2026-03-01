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
        public DateTime DateOfBirth { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}