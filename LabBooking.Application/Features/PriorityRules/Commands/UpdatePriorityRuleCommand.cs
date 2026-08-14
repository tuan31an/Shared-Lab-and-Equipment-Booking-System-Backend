using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.PriorityRules.Commands
{
    public class UpdatePriorityRuleCommand : IRequest<PriorityRuleDto>
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "PriorityLevel must be at least 1 (smaller = higher priority).")]
        public int PriorityLevel { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdatePriorityRuleCommandHandler : IRequestHandler<UpdatePriorityRuleCommand, PriorityRuleDto>
    {
        private readonly IRepository<PriorityRule> _rules;
        private readonly IUnitOfWork _uow;

        public UpdatePriorityRuleCommandHandler(IRepository<PriorityRule> rules, IUnitOfWork uow)
        {
            _rules = rules;
            _uow = uow;
        }

        public async Task<PriorityRuleDto> Handle(UpdatePriorityRuleCommand request, CancellationToken cancellationToken)
        {
            var rule = await _rules.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"Priority rule {request.Id} not found.");

            var name = request.Name.Trim();
            if (name.Length == 0)
                throw new ArgumentException("Name is required.");

            var clash = await _rules.FirstOrDefaultAsync(r => r.Id != request.Id && (r.Name == name || r.PriorityLevel == request.PriorityLevel), cancellationToken);
            if (clash != null)
                throw new ConflictException($"Another priority rule already uses name '{request.Name}' or level {request.PriorityLevel}.");

            rule.Name = name;
            rule.PriorityLevel = request.PriorityLevel;
            rule.Description = request.Description?.Trim();
            rule.MarkUpdated();

            _rules.Update(rule);
            await _uow.SaveChangesAsync(cancellationToken);

            return new PriorityRuleDto(rule.Id, rule.Name, rule.PriorityLevel, rule.Description);
        }
    }
}
