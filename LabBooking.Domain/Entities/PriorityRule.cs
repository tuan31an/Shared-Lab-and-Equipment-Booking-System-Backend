namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Quy tắc ưu tiên đặt lịch. Admin cấu hình bảng này; hệ thống tra cứu
    /// priority_level khi có tranh chấp khung giờ giữa nhiều Booking.
    /// </summary>
    public class PriorityRule : Common.BaseEntity
    {
        /// <summary>Tên quy tắc (VD: Đề tài nghiên cứu, Môn học, Tự học).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Số càng nhỏ càng ưu tiên cao.</summary>
        public int PriorityLevel { get; set; }

        /// <summary>Diễn giải áp dụng.</summary>
        public string? Description { get; set; }

        /// <summary>Danh sách Booking áp dụng quy tắc này.</summary>
        public ICollection<Booking> Bookings { get; set; } = [];
    }
}
