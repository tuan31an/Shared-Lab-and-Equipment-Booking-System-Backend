namespace LabBooking.Domain.Interfaces
{
    /// <summary>
    /// Đơn vị công việc — bọc việc lưu thay đổi của toàn bộ repository
    /// trong một transaction ngầm (ApplicationDbContext hiện thực).
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
