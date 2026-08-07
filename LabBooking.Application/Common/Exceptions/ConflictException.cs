namespace LabBooking.Application.Common.Exceptions
{
    /// <summary>
    /// Ném ra khi tài nguyên bị trùng/đã tồn tại (ví dụ email đã đăng ký).
    /// Được GlobalExceptionHandler ánh xạ sang HTTP 409.
    /// </summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
