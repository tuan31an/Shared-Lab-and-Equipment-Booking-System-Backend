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
    public class UpdateResourceCommand : IRequest<ResourceDto>
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required.")]
        public ResourceType Type { get; set; }

        public string? Specifications { get; set; }
        public string? ImageUrl { get; set; }
        public string? UsageRules { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? LabManagerId { get; set; }
        public ResourceStatus Status { get; set; } = ResourceStatus.Available;
    }

    public class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, ResourceDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Department> _departments;
        private readonly IRepository<User> _users;
        private readonly IUnitOfWork _uow;

        public UpdateResourceCommandHandler(IRepository<Resource> resources, IRepository<Department> departments, IRepository<User> users, IUnitOfWork uow)
        {
            _resources = resources;
            _departments = departments;
            _users = users;
            _uow = uow;
        }

        public async Task<ResourceDto> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
        {
            var resource = await _resources.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.Id} not found.");

            if (request.DepartmentId.HasValue && await _departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken) == null)
                throw new NotFoundException($"Department {request.DepartmentId} not found.");

            if (request.LabManagerId.HasValue && await _users.GetByIdAsync(request.LabManagerId.Value, cancellationToken) == null)
                throw new NotFoundException($"User {request.LabManagerId} not found.");

            resource.Name = request.Name.Trim();
            resource.Type = request.Type;
            resource.Specifications = request.Specifications;
            resource.ImageUrl = request.ImageUrl;
            resource.UsageRules = request.UsageRules;
            resource.DepartmentId = request.DepartmentId;
            resource.LabManagerId = request.LabManagerId;
            resource.Status = request.Status;
            resource.MarkUpdated();

            _resources.Update(resource);
            await _uow.SaveChangesAsync(cancellationToken);

            var departments = (await _departments.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            return GetResourcesQueryHandler.ToDto(resource, departments, users);
        }
    }
}