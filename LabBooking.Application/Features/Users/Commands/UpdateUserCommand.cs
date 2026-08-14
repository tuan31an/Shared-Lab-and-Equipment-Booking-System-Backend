using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Users.Queries;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Users.Commands
{
    public class UpdateUserCommand : IRequest<UserDto>
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "FullName is required.")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Role is invalid.")]
        public UserRole? Role { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [EnumDataType(typeof(UserStatus), ErrorMessage = "Status is invalid.")]
        public UserStatus? Status { get; set; }

        public Guid? DepartmentId { get; set; }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Department> _departments;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public UpdateUserCommandHandler(IRepository<User> users, IRepository<Department> departments, ICurrentUser currentUser, IUnitOfWork uow)
        {
            _users = users;
            _departments = departments;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            if (!request.Role.HasValue || !Enum.IsDefined(request.Role.Value))
                throw new ArgumentException("Role is invalid.");
            if (!request.Status.HasValue || !Enum.IsDefined(request.Status.Value))
                throw new ArgumentException("Status is invalid.");

            var user = await _users.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"User {request.Id} not found.");

            if (user.Id == _currentUser.UserId &&
                (request.Role.Value != user.Role || request.Status.Value != user.Status))
                throw new ConflictException("You cannot change your own role or status.");

            // Không được phế truất/huỷ kích hoạt admin cuối cùng còn hoạt động.
            if (user.Role == UserRole.Admin &&
                (request.Role.Value != UserRole.Admin || request.Status.Value != UserStatus.Active))
            {
                var activeAdmins = await _users.ListAsync(
                    u => u.Role == UserRole.Admin && u.Status == UserStatus.Active, cancellationToken);
                if (activeAdmins.Count <= 1)
                    throw new ConflictException("Cannot demote or disable the last active Admin.");
            }

            var fullName = request.FullName.Trim();
            if (fullName.Length == 0)
                throw new ArgumentException("FullName is required.");

            if (request.DepartmentId.HasValue &&
                await _departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken) == null)
                throw new NotFoundException($"Department {request.DepartmentId} not found.");

            user.FullName = fullName;
            user.Role = request.Role.Value;
            user.Status = request.Status.Value;
            user.DepartmentId = request.DepartmentId;
            user.MarkUpdated();

            _users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);

            return GetUsersQueryHandler.ToDto(user);
        }
    }
}
