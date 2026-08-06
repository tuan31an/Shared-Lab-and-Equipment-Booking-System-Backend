using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>Vi phạm: trả trễ (check-out muộn) hoặc không đến nhận phòng (no-show).</summary>
    public class Violation : Common.BaseEntity
    {
        /// <summary>Người vi phạm (FK → User).</summary>
        public Guid UserId { get; set; }

        /// <summary>Booking liên quan (FK → Booking), nếu có.</summary>
        public Guid? BookingId { get; set; }

        /// <summary>Loại vi phạm.</summary>
        public ViolationType Type { get; set; }

        /// <summary>Thời điểm ghi nhận.</summary>
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Ghi chú.</summary>
        public string? Note { get; set; }

        /// <summary>Người vi phạm.</summary>
        public User User { get; set; } = null!;

        /// <summary>Booking liên quan.</summary>
        public Booking? Booking { get; set; }
    }
}
