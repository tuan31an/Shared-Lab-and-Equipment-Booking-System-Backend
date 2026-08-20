using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Restrictions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Restrictions.Commands
{
    public class RemoveRestrictionCommand : IRequest<RestrictionDto>
    {
        public Guid RestrictionId { get; set; }
    }

    public class RemoveRestrictionCommandHandler : IRequestHandler<RemoveRestrictionCommand, RestrictionDto>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Restriction> _restrictions;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public RemoveRestrictionCommandHandler(
            IRepository<User> users,
            IRepository<Restriction> restrictions,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _users = users;
            _restrictions = restrictions;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<RestrictionDto> Handle(RemoveRestrictionCommand request, CancellationToken cancellationToken)
        {
            if (_currentUser.Role != "Admin")
                throw new UnauthorizedException("Only an Admin can remove restrictions.");

            var restriction = await _restrictions.GetByIdAsync(request.RestrictionId, cancellationToken)
                ?? throw new NotFoundException($"Restriction {request.RestrictionId} not found.");

            var user = await _users.GetByIdAsync(restriction.UserId, cancellationToken);

            _restrictions.Remove(restriction);
            await _uow.SaveChangesAsync(cancellationToken);
            await RestrictionEvaluation.SyncUserStatusAsync(_restrictions, _users, restriction.UserId, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new RestrictionDto(
                restriction.Id,
                restriction.UserId,
                user?.FullName,
                restriction.StartDate,
                restriction.EndDate,
                restriction.Reason,
                restriction.CreatedBy);
        }
    }
}