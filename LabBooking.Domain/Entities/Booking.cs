using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>Yêu cầu đặt lịch phòng/thiết bị.</summary>
    public class Booking : Common.BaseEntity
    {
        /// <summary>Phòng/thiết bị được đặt (FK → Resource).</summary>
        public Guid ResourceId { get; set; }

        /// <summary>Người đặt lịch (FK → User).</summary>
        public Guid RequesterId { get; set; }

        /// <summary>Mức ưu tiên áp dụng (FK → PriorityRule).</summary>
        public Guid? RuleId { get; set; }

        /// <summary>Thời điểm bắt đầu.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Thời điểm kết thúc (CHECK end_time > start_time).</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Mục đích sử dụng.</summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>Trạng thái yêu cầu.</summary>
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        /// <summary>Người duyệt (FK → User, Lab Manager).</summary>
        public Guid? ApprovedBy { get; set; }

        /// <summary>Thời điểm duyệt.</summary>
        public DateTime? ApprovedAt { get; set; }
    }
}
