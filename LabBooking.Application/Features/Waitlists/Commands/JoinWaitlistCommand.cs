using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Waitlists;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Waitlists.Commands
{
    public class JoinWaitlistCommand : IRequest<WaitlistDto>
    {
        [Required(ErrorMessage = "ResourceId is required.")]
        public Guid ResourceId { get; set; }

        [Required(ErrorMessage = "DesiredStart is required.")]
        public DateTime DesiredStart { get; set; }

        [Required(ErrorMessage = "DesiredEnd is required.")]
        public DateTime DesiredEnd { get; set; }
    }

    public class JoinWaitlistCommandHandler : IRequestHandler<JoinWaitlistCommand, WaitlistDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Waitlist> _waitlists;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public JoinWaitlistCommandHandler(
            IRepository<Resource> resources,
            IRepository<Waitlist> waitlists,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _resources = resources;
            _waitlists = waitlists;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<WaitlistDto> Handle(JoinWaitlistCommand request, CancellationToken cancellationToken)
        {
            if (request.DesiredEnd <= request.DesiredStart)
                throw new ArgumentException("DesiredEnd must be after DesiredStart.");

            if (request.DesiredStart <= DateTime.UtcNow)
                throw new ArgumentException("DesiredStart must be in the future.");

            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            var requesterId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var duplicate = await _waitlists.FirstOrDefaultAsync(w =>
                w.ResourceId == request.ResourceId &&
                w.RequesterId == requesterId &&
                w.Status == WaitlistStatus.Waiting &&
                w.DesiredStart < request.DesiredEnd && request.DesiredStart < w.DesiredEnd,
                cancellationToken);
            if (duplicate != null)
                throw new ConflictException("You already have an active waitlist entry for this time slot.");

            var entry = new Waitlist
            {
                ResourceId = request.ResourceId,
                RequesterId = requesterId,
                DesiredStart = request.DesiredStart,
                DesiredEnd = request.DesiredEnd,
                Status = WaitlistStatus.Waiting
            };

            await _waitlists.AddAsync(entry, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return WaitlistEvaluation.ToDto(entry, resource.Name);
        }
    }
}