using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Waitlists;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
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
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");
            var isAdmin = _currentUser.Role == "Admin";

            var list = await _waitlists.ListAsync(w =>
                (isAdmin || w.RequesterId == userId) &&
                (!request.ActiveOnly || w.Status == WaitlistStatus.Waiting),
                cancellationToken);

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return list
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => WaitlistEvaluation.ToDto(w, resources.TryGetValue(w.ResourceId, out var r) ? r.Name : null))
                .ToList();
        }
    }
}