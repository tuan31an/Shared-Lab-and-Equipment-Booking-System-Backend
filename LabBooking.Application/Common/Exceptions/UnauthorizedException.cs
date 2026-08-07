namespace LabBooking.Application.Common.Exceptions
{
    /// <summary>
    /// Ném ra khi thông tin xác thực sai hoặc token không hợp lệ.
    /// Được GlobalExceptionHandler ánh xạ sang HTTP 401.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
