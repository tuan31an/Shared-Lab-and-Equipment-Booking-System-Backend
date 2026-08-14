namespace LabBooking.Application.Common.Exceptions
{
    /// <summary>
    /// Ném ra khi tài nguyên bị trùng/đã tồn tại (ví dụ email đã đăng ký).
    /// Được GlobalExceptionHandler ánh xạ sang HTTP 409.
    /// </summary>
    public class ConflictException : Exception
    {
        /// <summary>Dữ liệu chi tiết kèm theo lỗi (ví dụ: danh sách khung giờ thay thế).</summary>
        public object? Payload { get; }

        public ConflictException(string message, object? payload = null) : base(message)
        {
            Payload = payload;
        }
    }
}
