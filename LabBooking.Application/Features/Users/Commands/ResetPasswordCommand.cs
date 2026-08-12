using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Users.Commands
{
    public class ResetPasswordCommand : IRequest
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "NewPassword is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IRepository<User> _users;
        private readonly IUnitOfWork _uow;

        public ResetPasswordCommandHandler(IRepository<User> users, IUnitOfWork uow)
        {
            _users = users;
            _uow = uow;
        }

        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"User {request.Id} not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MarkUpdated();
            _users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
