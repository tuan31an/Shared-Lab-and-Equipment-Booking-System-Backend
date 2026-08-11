using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Waitlists;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Waitlists.Commands
{
    public class LeaveWaitlistCommand : IRequest<WaitlistDto>
    {
        public Guid WaitlistId { get; set; }
    }

    public class LeaveWaitlistCommandHandler : IRequestHandler<LeaveWaitlistCommand, WaitlistDto>
    {
        private readonly IRepository<Waitlist> _waitlists;
        private readonly IRepository<Resource> _resources;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public LeaveWaitlistCommandHandler(
            IRepository<Waitlist> waitlists,
            IRepository<Resource> resources,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _waitlists = waitlists;
            _resources = resources;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<WaitlistDto> Handle(LeaveWaitlistCommand request, CancellationToken cancellationToken)
        {
            var entry = await _waitlists.GetByIdAsync(request.WaitlistId, cancellationToken)
                ?? throw new NotFoundException($"Waitlist entry {request.WaitlistId} not found.");

            if (entry.RequesterId != _currentUser.UserId && _currentUser.Role != "Admin")
                throw new UnauthorizedException("You can only leave your own waitlist entries.");

            if (entry.Status is WaitlistStatus.Notified or WaitlistStatus.Converted)
                throw new ArgumentException("This waitlist entry can no longer be withdrawn.");

            entry.Status = WaitlistStatus.Expired;
            _waitlists.Update(entry);
            await _uow.SaveChangesAsync(cancellationToken);

            var resource = await _resources.GetByIdAsync(entry.ResourceId, cancellationToken);
            return WaitlistEvaluation.ToDto(entry, resource?.Name);
        }
    }
}