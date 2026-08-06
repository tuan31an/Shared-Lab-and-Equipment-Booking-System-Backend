using System.ComponentModel.DataAnnotations;

namespace LabBooking.API.Models
{
    public class RefreshRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}