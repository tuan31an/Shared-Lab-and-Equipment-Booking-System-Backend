namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Refresh token dùng để cấp lại access token khi hết hạn.
    /// Lưu DB để có thể thu hồi (revoke) khi logout hoặc rotate.
    /// </summary>
    public class RefreshToken : Common.BaseEntity
    {
        public Guid UserId { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>Null khi token còn hiệu lực.</summary>
        public DateTime? RevokedAtUtc { get; set; }

        public User? User { get; set; }
    }
}