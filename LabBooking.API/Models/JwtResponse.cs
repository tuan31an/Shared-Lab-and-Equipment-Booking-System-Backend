using System;

namespace LabBooking.API.Models
{
    public class JwtResponse
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
