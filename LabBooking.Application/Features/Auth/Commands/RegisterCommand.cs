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
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Department> _departments;
        private readonly IUnitOfWork _uow;

        public RegisterCommandHandler(
            IRepository<User> users,
            IRepository<Department> departments,
            IUnitOfWork uow)
        {
            _users = users;
            _departments = departments;
            _uow = uow;
        }

        public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.");

            var fullName = request.FullName.Trim();
            if (fullName.Length == 0)
                throw new ArgumentException("FullName is required.");

            var email = request.Email.Trim();
            var existing = await _users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing != null)
                throw new ConflictException("Email already exists.");

            if (request.DepartmentId.HasValue &&
                await _departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken) == null)
                throw new NotFoundException($"Department {request.DepartmentId} not found.");

            var user = new User
            {
                FullName = fullName,
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
