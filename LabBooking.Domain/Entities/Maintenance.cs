using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Lịch bảo trì phòng/thiết bị. Khung thời gian Maintenance áp dụng cùng
    /// ràng buộc chống chồng lấn như Booking để tự động khoá lịch đặt.
    /// </summary>
    public class Maintenance : Common.BaseEntity
    {
        /// <summary>Phòng/thiết bị bảo trì (FK → Resource).</summary>
        public Guid ResourceId { get; set; }

        /// <summary>Bắt đầu bảo trì.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Kết thúc bảo trì (CHECK end_time > start_time).</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Nội dung bảo trì.</summary>
        public string? Description { get; set; }

        /// <summary>Chi phí phát sinh.</summary>
        public decimal? Cost { get; set; }

        /// <summary>Trạng thái đợt bảo trì.</summary>
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

        /// <summary>Người lập lịch (FK → User, Lab Manager).</summary>
        public Guid? CreatedBy { get; set; }
    }
}
