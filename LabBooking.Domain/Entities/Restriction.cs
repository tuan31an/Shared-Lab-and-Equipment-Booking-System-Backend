namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Hạn chế quyền đặt lịch tạm thời. Khi tồn tại Restriction đang hiệu lực
    /// (ngày hiện tại nằm trong [start_date, end_date]), hệ thống chặn User tạo
    /// Booking mới và cập nhật User.status = Restricted.
    /// </summary>
    public class Restriction : Common.BaseEntity
    {
        /// <summary>Người bị hạn chế (FK → User).</summary>
        public Guid UserId { get; set; }

        /// <summary>Ngày bắt đầu hạn chế.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Ngày kết thúc hạn chế.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Lý do (số lần vi phạm...).</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>Người áp dụng (FK → User, Admin).</summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>Người bị hạn chế.</summary>
        public User User { get; set; } = null!;

        /// <summary>Người áp dụng (Admin).</summary>
        public User? CreatedByUser { get; set; }
    }
}
