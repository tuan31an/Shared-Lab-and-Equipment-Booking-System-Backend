using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.PriorityRules.Commands
{
    public class CreatePriorityRuleCommand : IRequest<PriorityRuleDto>
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "PriorityLevel must be at least 1 (smaller = higher priority).")]
        public int PriorityLevel { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class CreatePriorityRuleCommandHandler : IRequestHandler<CreatePriorityRuleCommand, PriorityRuleDto>
    {
        private readonly IRepository<PriorityRule> _rules;
        private readonly IUnitOfWork _uow;

        public CreatePriorityRuleCommandHandler(IRepository<PriorityRule> rules, IUnitOfWork uow)
        {
            _rules = rules;
            _uow = uow;
        }

        public async Task<PriorityRuleDto> Handle(CreatePriorityRuleCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();
            if (name.Length == 0)
                throw new ArgumentException("Name is required.");

            var existing = await _rules.FirstOrDefaultAsync(r => r.Name == name || r.PriorityLevel == request.PriorityLevel, cancellationToken);
            if (existing != null)
                throw new ConflictException($"A priority rule with name '{request.Name}' or level {request.PriorityLevel} already exists.");

            var rule = new PriorityRule
            {
                Name = name,
                PriorityLevel = request.PriorityLevel,
                Description = request.Description?.Trim()
            };

            await _rules.AddAsync(rule, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new PriorityRuleDto(rule.Id, rule.Name, rule.PriorityLevel, rule.Description);
        }
    }
}
