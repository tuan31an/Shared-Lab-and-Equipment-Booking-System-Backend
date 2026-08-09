using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Departments.Queries
{
    public class GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>
    {
    }

    public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
    {
        private readonly IRepository<Department> _departments;

        public GetDepartmentsQueryHandler(IRepository<Department> departments)
        {
            _departments = departments;
        }

        public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
        {
            var items = await _departments.GetAllAsync(cancellationToken);
            return items.OrderBy(d => d.Name).Select(d => new DepartmentDto(d.Id, d.Name)).ToList();
        }
    }
}