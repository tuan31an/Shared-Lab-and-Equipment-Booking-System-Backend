namespace LabBooking.Domain.Common
{
    /// <summary>
    /// Container trung lập cho một trang dữ liệu: các phần tử của trang + tổng số bản ghi.
    /// Dùng chung cho tầng Domain/Application, không phụ thuộc framework.
    /// </summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int TotalCount { get; }

        public PagedResult(IReadOnlyList<T> items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }
    }
}
