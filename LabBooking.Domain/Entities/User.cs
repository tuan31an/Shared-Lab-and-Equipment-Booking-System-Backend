using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Người dùng hệ thống (Admin / Lab Manager / Requester).
    /// status = Restricted được hệ thống tự set khi Restriction đang hiệu lực.
    /// </summary>
    public class User : Common.BaseEntity
    {
        /// <summary>Họ tên.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Email dùng để đăng nhập (UNIQUE).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Mật khẩu đã băm.</summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Vai trò — quyết định phân quyền theo Role/Claim khi phát hành JWT.</summary>
        public UserRole Role { get; set; }

        /// <summary>Khoa/bộ môn trực thuộc (FK → Department).</summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>Trạng thái tài khoản.</summary>
        public UserStatus Status { get; set; } = UserStatus.Active;

        /// <summary>Soft-delete thay vì xoá cứng để giữ toàn vẹn lịch sử.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Khoa/bộ môn trực thuộc.</summary>
        public Department? Department { get; set; }

        /// <summary>Danh sách phòng/thiết bị đang phụ trách (Lab Manager).</summary>
        public ICollection<Resource> ManagedResources { get; set; } = [];

        /// <summary>Danh sách Booking đã tạo (Requester).</summary>
        public ICollection<Booking> RequestedBookings { get; set; } = [];

        /// <summary>Danh sách Booking đã duyệt (Lab Manager).</summary>
        public ICollection<Booking> ApprovedBookings { get; set; } = [];

        /// <summary>Danh sách yêu cầu chờ.</summary>
        public ICollection<Waitlist> Waitlists { get; set; } = [];

        /// <summary>Danh sách vi phạm.</summary>
        public ICollection<Violation> Violations { get; set; } = [];

        /// <summary>Danh sách hạn chế bị áp dụng.</summary>
        public ICollection<Restriction> Restrictions { get; set; } = [];

        /// <summary>Danh sách thông báo đã nhận.</summary>
        public ICollection<Notification> Notifications { get; set; } = [];
    }
}
