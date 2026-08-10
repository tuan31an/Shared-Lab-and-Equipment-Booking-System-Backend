using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Auth.Commands
{
    public class RegisterCommand : IRequest<UserDto>
    {
        [Required(ErrorMessage = "FullName is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
    {
        private readonly IRepository<User> _users;
        private readonly IUnitOfWork _uow;

        public RegisterCommandHandler(IRepository<User> users, IUnitOfWork uow)
        {
            _users = users;
            _uow = uow;
        }

        public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();
            var existing = await _users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing != null)
                throw new ConflictException("Email already exists.");

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Requester,
                DepartmentId = request.DepartmentId,
                Status = UserStatus.Active
            };

            await _users.AddAsync(user, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Status.ToString(), user.CreatedAt);
        }
    }
}
