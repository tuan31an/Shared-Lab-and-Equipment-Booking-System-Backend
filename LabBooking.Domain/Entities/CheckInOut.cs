namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Check-in/Check-out của một Booking (quan hệ 1-1, booking_id UNIQUE).
    /// check_out_time trễ hơn end_time của Booking quá ngưỡng cấu hình
    /// sẽ tự sinh một bản ghi Violation (type = Late).
    /// </summary>
    public class CheckInOut : Common.BaseEntity
    {
        /// <summary>Booking tương ứng (FK → Booking, UNIQUE).</summary>
        public Guid BookingId { get; set; }

        /// <summary>Thời điểm check-in thực tế.</summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>Thời điểm check-out thực tế.</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Thời lượng sử dụng thực tế (phút), tính toán từ check-in/check-out.</summary>
        public int? ActualDuration { get; set; }
    }
}
