using System.ComponentModel.DataAnnotations;

namespace Cargobell.Shared.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "First name can only contain letters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Last name can only contain letters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\+?[0-9]*$", ErrorMessage = "Invalid phone format. Only numbers and '+' allowed.")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
    }
}