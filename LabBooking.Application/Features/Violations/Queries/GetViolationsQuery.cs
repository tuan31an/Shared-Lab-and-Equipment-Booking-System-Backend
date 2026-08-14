using LabBooking.Application.Common;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Violations.Queries
{
    public class GetViolationsQuery : IRequest<IReadOnlyList<ViolationDto>>
    {
        public Guid? UserId { get; set; }

        public ViolationType? Type { get; set; }
    }

    public class GetViolationsQueryHandler : IRequestHandler<GetViolationsQuery, IReadOnlyList<ViolationDto>>
    {
        private readonly IRepository<Violation> _violations;
        private readonly IRepository<User> _users;
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly ICurrentUser _currentUser;

        public GetViolationsQueryHandler(
            IRepository<Violation> violations,
            IRepository<User> users,
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            ICurrentUser currentUser)
        {
            _violations = violations;
            _users = users;
            _bookings = bookings;
            _resources = resources;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ViolationDto>> Handle(GetViolationsQuery request, CancellationToken cancellationToken)
        {
            var role = _currentUser.Role;
            var userId = _currentUser.UserId;
            var ownOnly = role != "Admin" && role != "LabManager";

            // LabManager: lọc vi phạm theo booking trên resource phụ trách (trong RAM, tập đã thu hẹp).
            HashSet<Guid>? managedBookingIds = null;
            if (role == "LabManager" && userId.HasValue)
            {
                var managedResourceIds = (await _resources.GetAllAsync(cancellationToken))
                    .Where(r => r.LabManagerId == userId)
                    .Select(r => r.Id)
                    .ToHashSet();
                managedBookingIds = (await _bookings.GetAllAsync(cancellationToken))
                    .Where(b => managedResourceIds.Contains(b.ResourceId))
                    .Select(b => b.Id)
                    .ToHashSet();
            }

            var list = await _violations.ListAsync(v =>
                (!request.UserId.HasValue || v.UserId == request.UserId) &&
                (!request.Type.HasValue || v.Type == request.Type) &&
                (!ownOnly || v.UserId == userId),
                cancellationToken);

            var filtered = (role == "LabManager"
                ? list.Where(v => v.BookingId.HasValue && managedBookingIds?.Contains(v.BookingId.Value) == true)
                : list)
                .OrderByDescending(v => v.RecordedAt)
                .ToList();

            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);

            return filtered.Select(v =>
            {
                users.TryGetValue(v.UserId, out var user);
                return new ViolationDto(
                    v.Id,
                    v.UserId,
                    user?.FullName,
                    v.BookingId,
                    v.Type.ToString(),
                    v.RecordedAt,
                    v.Note);
            }).ToList();
        }
    }
}