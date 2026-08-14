using LabBooking.Application.Common;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Incidents.Queries
{
    public class GetIncidentsQuery : IRequest<IReadOnlyList<IncidentDto>>
    {
        public IncidentStatus? Status { get; set; }

        public Guid? ResourceId { get; set; }
    }

    public class GetIncidentsQueryHandler : IRequestHandler<GetIncidentsQuery, IReadOnlyList<IncidentDto>>
    {
        private readonly IRepository<Incident> _incidents;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly ICurrentUser _currentUser;

        public GetIncidentsQueryHandler(
            IRepository<Incident> incidents,
            IRepository<Resource> resources,
            IRepository<User> users,
            ICurrentUser currentUser)
        {
            _incidents = incidents;
            _resources = resources;
            _users = users;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<IncidentDto>> Handle(GetIncidentsQuery request, CancellationToken cancellationToken)
        {
            var role = _currentUser.Role;
            var userId = _currentUser.UserId;
            var ownOnly = role != "Admin" && role != "LabManager";

            // LabManager: lọc theo resource phụ trách trong RAM (tập đã thu hẹp bởi bộ lọc SQL).
            HashSet<Guid>? managedResourceIds = null;
            if (role == "LabManager" && userId.HasValue)
            {
                managedResourceIds = (await _resources.GetAllAsync(cancellationToken))
                    .Where(r => r.LabManagerId == userId)
                    .Select(r => r.Id)
                    .ToHashSet();
            }

            var list = await _incidents.ListAsync(i =>
                (!request.Status.HasValue || i.Status == request.Status) &&
                (!request.ResourceId.HasValue || i.ResourceId == request.ResourceId) &&
                (!ownOnly || i.ReportedBy == userId),
                cancellationToken);

            var filtered = (role == "LabManager"
                ? list.Where(i => managedResourceIds?.Contains(i.ResourceId) == true)
                : list)
                .OrderByDescending(i => i.ReportedAt)
                .ToList();

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);

            return filtered.Select(i =>
            {
                resources.TryGetValue(i.ResourceId, out var resource);
                users.TryGetValue(i.ReportedBy, out var reporter);
                return new IncidentDto(
                    i.Id,
                    i.ResourceId,
                    resource?.Name,
                    i.BookingId,
                    i.ReportedBy,
                    reporter?.FullName,
                    i.Description,
                    i.ImageUrl,
                    i.Status.ToString(),
                    i.ReportedAt);
            }).ToList();
        }
    }
}