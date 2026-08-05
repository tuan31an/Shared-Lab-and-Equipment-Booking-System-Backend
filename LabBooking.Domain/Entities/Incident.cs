using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>Sự cố/Hư hỏng phòng, thiết bị.</summary>
    public class Incident : Common.BaseEntity
    {
        /// <summary>Booking liên quan (FK → Booking), nếu có.</summary>
        public Guid? BookingId { get; set; }

        /// <summary>Phòng/thiết bị bị sự cố (FK → Resource).</summary>
        public Guid ResourceId { get; set; }

        /// <summary>Người ghi nhận (FK → User).</summary>
        public Guid ReportedBy { get; set; }

        /// <summary>Mô tả sự cố.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Hình ảnh minh chứng.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Trạng thái xử lý.</summary>
        public IncidentStatus Status { get; set; } = IncidentStatus.Open;

        /// <summary>Thời điểm ghi nhận.</summary>
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    }
}
