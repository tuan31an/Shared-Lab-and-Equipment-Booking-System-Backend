using LabBooking.Domain.Enums;

namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Phòng/Thiết bị dùng chung (phân biệt qua Type) để dùng chung logic
    /// lịch/xung đột/bảo trì — tránh trùng lặp bảng.
    /// </summary>
    public class Resource : Common.BaseEntity
    {
        /// <summary>Tên phòng/thiết bị.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Loại: phòng hoặc thiết bị.</summary>
        public ResourceType Type { get; set; }

        /// <summary>Thông số kỹ thuật.</summary>
        public string? Specifications { get; set; }

        /// <summary>Hình ảnh minh hoạ.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Quy định sử dụng.</summary>
        public string? UsageRules { get; set; }

        /// <summary>Khoa/bộ môn quản lý (FK → Department).</summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>Người phụ trách duyệt lịch (FK → User).</summary>
        public Guid? LabManagerId { get; set; }

        /// <summary>Trạng thái phòng/thiết bị.</summary>
        public ResourceStatus Status { get; set; } = ResourceStatus.Available;

        /// <summary>Soft-delete thay vì xoá cứng để giữ toàn vẹn lịch sử.</summary>
        public bool IsDeleted { get; set; }
    }
}
