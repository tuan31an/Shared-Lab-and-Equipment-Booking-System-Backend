using LabBooking.Domain.Common;
using System.Linq.Expressions;

namespace LabBooking.Domain.Interfaces
{
    /// <summary>
    /// Repository chung cho mọi Entity: CRUD + truy vấn cơ bản.
    /// Định nghĩa ở Domain, hiện thực ở tầng Infrastructure.
    /// </summary>
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        void Update(T entity);

        void Remove(T entity);
    }
}
