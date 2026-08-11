using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;

namespace LabBooking.Application.Features.Restrictions
{
    /// <summary>Đánh giá Restriction: kiểm tra đang hiệu lực và đồng bộ User.Status.</summary>
    internal static class RestrictionEvaluation
    {
        public static bool IsActive(Restriction r, DateTime now)
            => r.StartDate <= now.Date && r.EndDate >= now.Date;

        /// <summary>Set User.Status = Restricted nếu còn hạn chế hiệu lực, ngược lại Active.</summary>
        public static async Task SyncUserStatusAsync(
            IRepository<Restriction> restrictions,
            IRepository<User> users,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var active = await restrictions.ListAsync(
                r => r.UserId == userId && r.StartDate <= now.Date && r.EndDate >= now.Date,
                cancellationToken);

            var user = await users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return;

            user.Status = active.Count > 0 ? UserStatus.Restricted : UserStatus.Active;
            users.Update(user);
        }
    }
}