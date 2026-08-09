using LabBooking.Application.Common.Exceptions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Resources.Commands
{
    public class DeleteResourceCommand : IRequest
    {
        public Guid Id { get; set; }
    }

    public class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IUnitOfWork _uow;

        public DeleteResourceCommandHandler(IRepository<Resource> resources, IUnitOfWork uow)
        {
            _resources = resources;
            _uow = uow;
        }

        public async Task Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
        {
            var resource = await _resources.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.Id} not found.");

            resource.IsDeleted = true;
            resource.MarkUpdated();
            _resources.Update(resource);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}