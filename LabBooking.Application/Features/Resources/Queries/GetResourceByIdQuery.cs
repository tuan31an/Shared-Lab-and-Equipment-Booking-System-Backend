using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Resources.Queries;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Resources.Queries
{
    public class GetResourceByIdQuery : IRequest<ResourceDto>
    {
        public Guid Id { get; set; }
    }

    public class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, ResourceDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Department> _departments;
        private readonly IRepository<User> _users;

        public GetResourceByIdQueryHandler(IRepository<Resource> resources, IRepository<Department> departments, IRepository<User> users)
        {
            _resources = resources;
            _departments = departments;
            _users = users;
        }

        public async Task<ResourceDto> Handle(GetResourceByIdQuery request, CancellationToken cancellationToken)
        {
            var resource = await _resources.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.Id} not found.");

            var departments = (await _departments.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);

            return GetResourcesQueryHandler.ToDto(resource, departments, users);
        }
    }
}