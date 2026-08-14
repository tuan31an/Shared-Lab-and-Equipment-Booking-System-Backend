using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Resources.Queries
{
    public class GetResourcesQuery : IRequest<PaginationResponse<ResourceDto>>
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public ResourceType? Type { get; set; }

        public Guid? DepartmentId { get; set; }

        public ResourceStatus? Status { get; set; }

        public string? Keyword { get; set; }
    }

    public class GetResourcesQueryHandler : IRequestHandler<GetResourcesQuery, PaginationResponse<ResourceDto>>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Department> _departments;
        private readonly IRepository<User> _users;

        public GetResourcesQueryHandler(IRepository<Resource> resources, IRepository<Department> departments, IRepository<User> users)
        {
            _resources = resources;
            _departments = departments;
            _users = users;
        }

        public async Task<PaginationResponse<ResourceDto>> Handle(GetResourcesQuery request, CancellationToken cancellationToken)
        {
            var all = await _resources.ListAsync(r =>
                (!request.Type.HasValue || r.Type == request.Type) &&
                (!request.DepartmentId.HasValue || r.DepartmentId == request.DepartmentId) &&
                (!request.Status.HasValue || r.Status == request.Status) &&
                (string.IsNullOrWhiteSpace(request.Keyword) || r.Name.Contains(request.Keyword)),
                cancellationToken);

            var total = all.Count;
            var page = all
                .OrderByDescending(r => r.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var departments = (await _departments.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);

            return new PaginationResponse<ResourceDto>(
                page.Select(r => ToDto(r, departments, users)).ToList(),
                total, request.Page, request.PageSize);
        }

        internal static ResourceDto ToDto(Resource r, IReadOnlyDictionary<Guid, Department> departments, IReadOnlyDictionary<Guid, User> users)
        {
            departments.TryGetValue(r.DepartmentId ?? Guid.Empty, out var dept);
            users.TryGetValue(r.LabManagerId ?? Guid.Empty, out var manager);

            return new ResourceDto(
                r.Id,
                r.Name,
                r.Type.ToString(),
                r.Specifications,
                r.ImageUrl,
                r.UsageRules,
                r.DepartmentId,
                dept?.Name,
                r.LabManagerId,
                manager?.FullName,
                r.Status.ToString(),
                r.CreatedAt);
        }
    }
}