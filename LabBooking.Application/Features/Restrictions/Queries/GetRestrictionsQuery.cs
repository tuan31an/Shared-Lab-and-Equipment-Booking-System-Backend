using LabBooking.Application.Common;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Restrictions.Queries
{
    public class GetRestrictionsQuery : IRequest<IReadOnlyList<RestrictionDto>>
    {
        public Guid? UserId { get; set; }

        public bool ActiveOnly { get; set; }
    }

    public class GetRestrictionsQueryHandler : IRequestHandler<GetRestrictionsQuery, IReadOnlyList<RestrictionDto>>
    {
        private readonly IRepository<Restriction> _restrictions;
        private readonly IRepository<User> _users;
        private readonly ICurrentUser _currentUser;

        public GetRestrictionsQueryHandler(
            IRepository<Restriction> restrictions,
            IRepository<User> users,
            ICurrentUser currentUser)
        {
            _restrictions = restrictions;
            _users = users;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<RestrictionDto>> Handle(GetRestrictionsQuery request, CancellationToken cancellationToken)
        {
            var all = await _restrictions.ListAsync(null, cancellationToken);

            var scoped = _currentUser.Role == "Admin"
                ? all
                : all.Where(r => r.UserId == _currentUser.UserId).ToList();

            var now = DateTime.UtcNow;
            var filtered = scoped
                .Where(r =>
                    (!request.UserId.HasValue || r.UserId == request.UserId) &&
                    (!request.ActiveOnly || (r.StartDate <= now.Date && r.EndDate >= now.Date)))
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);

            return filtered.Select(r =>
            {
                users.TryGetValue(r.UserId, out var user);
                return new RestrictionDto(
                    r.Id,
                    r.UserId,
                    user?.FullName,
                    r.StartDate,
                    r.EndDate,
                    r.Reason,
                    r.CreatedBy);
            }).ToList();
        }
    }
}