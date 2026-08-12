using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Users.Commands
{
    public class ChangePasswordCommand : IRequest
    {
        [Required(ErrorMessage = "CurrentPassword is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "NewPassword is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IRepository<User> _users;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public ChangePasswordCommandHandler(IRepository<User> users, ICurrentUser currentUser, IUnitOfWork uow)
        {
            _users = users;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("User not authenticated.");
            var user = await _users.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"User {userId} not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedException("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MarkUpdated();
            _users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
