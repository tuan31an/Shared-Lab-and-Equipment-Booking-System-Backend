using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Users.Queries
{
    public class GetUsersQuery : IRequest<PaginationResponse<UserDto>>
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public UserRole? Role { get; set; }

        public UserStatus? Status { get; set; }

        public Guid? DepartmentId { get; set; }

        public string? Keyword { get; set; }
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginationResponse<UserDto>>
    {
        private readonly IRepository<User> _users;

        public GetUsersQueryHandler(IRepository<User> users)
        {
            _users = users;
        }

        public async Task<PaginationResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var all = await _users.ListAsync(u =>
                (!request.Role.HasValue || u.Role == request.Role) &&
                (!request.Status.HasValue || u.Status == request.Status) &&
                (!request.DepartmentId.HasValue || u.DepartmentId == request.DepartmentId) &&
                (string.IsNullOrWhiteSpace(request.Keyword) ||
                 u.FullName.Contains(request.Keyword) || u.Email.Contains(request.Keyword)),
                cancellationToken);

            var total = all.Count;
            var page = all
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PaginationResponse<UserDto>(page.Select(ToDto).ToList(), total, request.Page, request.PageSize);
        }

        internal static UserDto ToDto(User u)
            => new(u.Id, u.FullName, u.Email, u.Role.ToString(), u.Status.ToString(), u.CreatedAt);
    }
}
