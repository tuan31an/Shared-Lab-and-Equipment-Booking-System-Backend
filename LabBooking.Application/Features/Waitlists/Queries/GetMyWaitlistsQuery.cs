using LabBooking.Application.Common;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Waitlists;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Waitlists.Queries
{
    public class GetMyWaitlistsQuery : IRequest<IReadOnlyList<WaitlistDto>>
    {
        public bool ActiveOnly { get; set; }
    }

    public class GetMyWaitlistsQueryHandler : IRequestHandler<GetMyWaitlistsQuery, IReadOnlyList<WaitlistDto>>
    {
        private readonly IRepository<Waitlist> _waitlists;
        private readonly IRepository<Resource> _resources;
        private readonly ICurrentUser _currentUser;

        public GetMyWaitlistsQueryHandler(
            IRepository<Waitlist> waitlists,
            IRepository<Resource> resources,
            ICurrentUser currentUser)
        {
            _waitlists = waitlists;
            _resources = resources;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<WaitlistDto>> Handle(GetMyWaitlistsQuery request, CancellationToken cancellationToken)
        {
            var all = await _waitlists.ListAsync(null, cancellationToken);

            var scoped = _currentUser.Role == "Admin"
                ? all
                : all.Where(w => w.RequesterId == _currentUser.UserId).ToList();

            var active = scoped.Where(w => w.Status == Domain.Enums.WaitlistStatus.Waiting).ToList();
            if (request.ActiveOnly)
                scoped = active;

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return scoped
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => WaitlistEvaluation.ToDto(w, resources.TryGetValue(w.ResourceId, out var r) ? r.Name : null))
                .ToList();
        }
    }
}