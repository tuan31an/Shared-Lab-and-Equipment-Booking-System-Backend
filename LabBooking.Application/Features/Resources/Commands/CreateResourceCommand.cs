using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Resources.Queries;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Resources.Commands
{
    public class CreateResourceCommand : IRequest<ResourceDto>
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required.")]
        [EnumDataType(typeof(ResourceType), ErrorMessage = "Type is invalid.")]
        public ResourceType? Type { get; set; }

        public string? Specifications { get; set; }
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        public string? UsageRules { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? LabManagerId { get; set; }
    }

    public class CreateResourceCommandHandler : IRequestHandler<CreateResourceCommand, ResourceDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Department> _departments;
        private readonly IRepository<User> _users;
        private readonly IUnitOfWork _uow;

        public CreateResourceCommandHandler(IRepository<Resource> resources, IRepository<Department> departments, IRepository<User> users, IUnitOfWork uow)
        {
            _resources = resources;
            _departments = departments;
            _users = users;
            _uow = uow;
        }

        public async Task<ResourceDto> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
        {
            if (!request.Type.HasValue || !Enum.IsDefined(request.Type.Value))
                throw new ArgumentException("Type is invalid.");

            var name = request.Name.Trim();
            if (name.Length == 0)
                throw new ArgumentException("Name is required.");

            if (request.DepartmentId.HasValue && await _departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken) == null)
                throw new NotFoundException($"Department {request.DepartmentId} not found.");

            if (request.LabManagerId.HasValue)
            {
                var manager = await _users.GetByIdAsync(request.LabManagerId.Value, cancellationToken)
                    ?? throw new NotFoundException($"User {request.LabManagerId} not found.");
                if (manager.Role != UserRole.LabManager)
                    throw new ArgumentException("LabManagerId must reference a user with the LabManager role.");
            }

            var resource = new Resource
            {
                Name = name,
                Type = request.Type.Value,
                Specifications = request.Specifications,
                ImageUrl = request.ImageUrl?.Trim(),
                UsageRules = request.UsageRules,
                DepartmentId = request.DepartmentId,
                LabManagerId = request.LabManagerId,
                Status = ResourceStatus.Available
            };

            await _resources.AddAsync(resource, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            var departments = (await _departments.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            return GetResourcesQueryHandler.ToDto(resource, departments, users);
        }
    }
}
