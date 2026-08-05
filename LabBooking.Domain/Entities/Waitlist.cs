using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Hàng đợi chờ khi khung giờ kín. Khi một Booking bị huỷ hoặc hết hạn giữ chỗ,
    /// hệ thống quét Waitlist theo resource_id + khung giờ giao nhau để thông báo
    /// theo thứ tự đăng ký trước.
    /// </summary>
    public class Waitlist : Common.BaseEntity
    {
        /// <summary>Phòng/thiết bị mong muốn (FK → Resource).</summary>
        public Guid ResourceId { get; set; }

        /// <summary>Người chờ (FK → User).</summary>
        public Guid RequesterId { get; set; }

        /// <summary>Khung giờ mong muốn — bắt đầu.</summary>
        public DateTime DesiredStart { get; set; }

        /// <summary>Khung giờ mong muốn — kết thúc.</summary>
        public DateTime DesiredEnd { get; set; }

        /// <summary>Trạng thái yêu cầu chờ.</summary>
        public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

        /// <summary>Thời điểm hệ thống thông báo có chỗ trống.</summary>
        public DateTime? NotifiedAt { get; set; }

        /// <summary>Phòng/thiết bị mong muốn.</summary>
        public Resource Resource { get; set; } = null!;

        /// <summary>Người chờ.</summary>
        public User Requester { get; set; } = null!;
    }
}
