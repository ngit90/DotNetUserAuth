using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SampleApp.Models
{
    public class Registration
    {
        public Registration()
        {
            ProfileImgPath = "/images/placeholder.png";
        }
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(18, ErrorMessage = "Name cannot exceed 8 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        //[Required(ErrorMessage = "Phone number is required.")]
        [Range(1000000000, 9999999999, ErrorMessage = "Phone number must be 10 digits.")]
        public long Phone { get; set; }  // Changed to long to handle larger numbers

        //[Required(ErrorMessage = "Gender is required.")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Invalid gender selection.")]
        public string? Gender { get; set; }

        //[Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)] // Masks password input in forms
        public string? Password { get; set; }

        public string ProfileImgPath { get; set; }
      
        [NotMapped]
        [DataType(DataType.Upload)]
        [ValidateNever]
        public IFormFile ProfileImg { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "User";

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "Active";

        public string? GoogleId { get; set; }

    }
}
