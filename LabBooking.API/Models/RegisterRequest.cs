using System.ComponentModel.DataAnnotations;

namespace LabBooking.API.Models
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "FullName is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }
    }
}