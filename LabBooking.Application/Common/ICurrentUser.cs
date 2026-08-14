namespace LabBooking.Application.Common
{
    /// <summary>Lấy thông tin người dùng hiện tại từ token yêu cầu (triển khai ở tầng API).</summary>
    public interface ICurrentUser
    {
        /// <summary>Id người dùng đã xác thực, hoặc null nếu chưa xác thực.</summary>
        Guid? UserId { get; }

        /// <summary>Role người dùng, hoặc null nếu chưa xác thực.</summary>
        string? Role { get; }
    }
}