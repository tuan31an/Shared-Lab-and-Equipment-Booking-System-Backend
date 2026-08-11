using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Restrictions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Restrictions.Commands
{
    public class CreateRestrictionCommand : IRequest<RestrictionDto>
    {
        [Required(ErrorMessage = "UserId is required.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "StartDate is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "EndDate is required.")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateRestrictionCommandHandler : IRequestHandler<CreateRestrictionCommand, RestrictionDto>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Restriction> _restrictions;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public CreateRestrictionCommandHandler(
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

        public async Task<RestrictionDto> Handle(CreateRestrictionCommand request, CancellationToken cancellationToken)
        {
            if (_currentUser.Role != "Admin")
                throw new UnauthorizedException("Only an Admin can restrict booking rights.");

            if (request.EndDate < request.StartDate)
                throw new ArgumentException("EndDate must be on or after StartDate.");

            var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException($"User {request.UserId} not found.");

            var restriction = new Restriction
            {
                UserId = request.UserId,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                Reason = request.Reason.Trim(),
                CreatedBy = _currentUser.UserId
            };

            user.Status = UserStatus.Restricted;
            _users.Update(user);
            await _restrictions.AddAsync(restriction, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new RestrictionDto(
                restriction.Id,
                restriction.UserId,
                user.FullName,
                restriction.StartDate,
                restriction.EndDate,
                restriction.Reason,
                restriction.CreatedBy);
        }
    }
}