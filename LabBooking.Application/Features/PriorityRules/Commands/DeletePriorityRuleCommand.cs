using LabBooking.Application.Common.Exceptions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.PriorityRules.Commands
{
    public class DeletePriorityRuleCommand : IRequest
    {
        public Guid Id { get; set; }
    }

    public class DeletePriorityRuleCommandHandler : IRequestHandler<DeletePriorityRuleCommand>
    {
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<Booking> _bookings;
        private readonly IUnitOfWork _uow;

        public DeletePriorityRuleCommandHandler(IRepository<PriorityRule> rules, IRepository<Booking> bookings, IUnitOfWork uow)
        {
            _rules = rules;
            _bookings = bookings;
            _uow = uow;
        }

        public async Task Handle(DeletePriorityRuleCommand request, CancellationToken cancellationToken)
        {
            var rule = await _rules.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"Priority rule {request.Id} not found.");

            if (await _bookings.FirstOrDefaultAsync(b => b.RuleId == request.Id, cancellationToken) != null)
                throw new ConflictException("Priority rule is in use by existing bookings and cannot be deleted.");

            _rules.Remove(rule);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}