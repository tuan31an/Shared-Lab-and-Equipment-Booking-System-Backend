using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Users.Queries;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Users.Commands
{
    public class CreateUserCommand : IRequest<UserDto>
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

        [Required(ErrorMessage = "Role is required.")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Role is invalid.")]
        public UserRole? Role { get; set; }

        public Guid? DepartmentId { get; set; }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Department> _departments;
        private readonly IUnitOfWork _uow;

        public CreateUserCommandHandler(IRepository<User> users, IRepository<Department> departments, IUnitOfWork uow)
        {
            _users = users;
            _departments = departments;
            _uow = uow;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (!request.Role.HasValue || !Enum.IsDefined(request.Role.Value))
                throw new ArgumentException("Role is invalid.");

            var fullName = request.FullName.Trim();
            if (fullName.Length == 0)
                throw new ArgumentException("FullName is required.");

            var email = request.Email.Trim();
            if (await _users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken) != null)
                throw new ConflictException("Email already exists.");

            if (request.DepartmentId.HasValue &&
                await _departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken) == null)
                throw new NotFoundException($"Department {request.DepartmentId} not found.");

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role.Value,
                DepartmentId = request.DepartmentId,
                Status = UserStatus.Active
            };

            await _users.AddAsync(user, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return GetUsersQueryHandler.ToDto(user);
        }
    }
}
