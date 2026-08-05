namespace LabBooking.Domain.Entities
{
    /// <summary>
    /// Khoa/Bộ môn — dùng để phân nhóm User và Resource theo khoa/bộ môn,
    /// phục vụ thống kê tỷ lệ sử dụng theo khoa/bộ môn.
    /// </summary>
    public class Department : Common.BaseEntity
    {
        /// <summary>Tên khoa/bộ môn (UNIQUE).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Danh sách User thuộc khoa/bộ môn.</summary>
        public ICollection<User> Users { get; set; } = [];

        /// <summary>Danh sách Resource do khoa/bộ môn quản lý.</summary>
        public ICollection<Resource> Resources { get; set; } = [];
    }
}
