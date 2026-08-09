namespace LabBooking.Domain.Common
{
    /// <summary>
    /// Lớp cơ sở (Base) cho mọi Entity trong tầng Domain.
    /// Gom các thuộc tính dùng chung: khóa chính (Id) và mốc thời gian (audit).
    /// Các Entity khác chỉ cần kế thừa lớp này để có sẵn Id, CreatedAt, UpdatedAt.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>Khóa chính dùng chung cho mọi Entity.</summary>
        public Guid Id { get; protected set; }

        /// <summary>Thời điểm bản ghi được tạo (UTC).</summary>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>Thời điểm bản ghi được cập nhật gần nhất (UTC). Null nếu chưa từng sửa.</summary>
        public DateTime? UpdatedAt { get; protected set; }

        /// <summary>Dùng khi tạo mới một Entity: tự sinh Id và thời điểm tạo.</summary>
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Dùng khi dựng lại (rehydrate) Entity từ dữ liệu đã có trong database,
        /// giữ nguyên Id và các mốc thời gian gốc.
        /// </summary>
        protected BaseEntity(Guid id, DateTime createdAt, DateTime? updatedAt)
        {
            Id = id;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Đánh dấu Entity vừa được thay đổi. Gọi trong các phương thức nghiệp vụ
        /// làm thay đổi trạng thái để cập nhật lại mốc thời gian.
        /// </summary>
        public void MarkUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
