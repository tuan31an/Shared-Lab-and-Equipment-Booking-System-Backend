namespace LabBooking.Application.Common.Exceptions
{
    /// <summary>
    /// Ném ra khi không tìm thấy tài nguyên yêu cầu. Được GlobalExceptionHandler ánh xạ sang HTTP 404.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
