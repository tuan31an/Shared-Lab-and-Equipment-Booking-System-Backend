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
            var all = await _incidents.ListAsync(null, cancellationToken);
            var scope = await ScopeAsync(all, cancellationToken);

            var filtered = scope
                .Where(i =>
                    (!request.Status.HasValue || i.Status == request.Status) &&
                    (!request.ResourceId.HasValue || i.ResourceId == request.ResourceId))
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

        private async Task<IReadOnlyList<Incident>> ScopeAsync(IReadOnlyList<Incident> all, CancellationToken cancellationToken)
        {
            var role = _currentUser.Role;
            if (role == "Admin")
                return all;

            if (role == "LabManager")
            {
                var managedIds = (await _resources.GetAllAsync(cancellationToken))
                    .Where(r => r.LabManagerId == _currentUser.UserId)
                    .Select(r => r.Id)
                    .ToHashSet();
                return all.Where(i => managedIds.Contains(i.ResourceId)).ToList();
            }

            // Requester: chỉ thấy sự cố mình đã báo cáo.
            return all.Where(i => i.ReportedBy == _currentUser.UserId).ToList();
        }
    }
}