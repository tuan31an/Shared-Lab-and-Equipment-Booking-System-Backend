using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>Thông báo cho người dùng (nhắc lịch, waitlist, duyệt lịch...).</summary>
    public class Notification : Common.BaseEntity
    {
        /// <summary>Người nhận (FK → User).</summary>
        public Guid UserId { get; set; }

        /// <summary>Loại thông báo.</summary>
        public NotificationType Type { get; set; }

        /// <summary>Nội dung thông báo.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Trạng thái đã đọc.</summary>
        public bool IsRead { get; set; }
    }
}
