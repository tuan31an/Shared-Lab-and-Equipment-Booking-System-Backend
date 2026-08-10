using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.PriorityRules.Queries
{
    public class GetPriorityRulesQuery : IRequest<IReadOnlyList<PriorityRuleDto>>
    {
    }

    public class GetPriorityRulesQueryHandler : IRequestHandler<GetPriorityRulesQuery, IReadOnlyList<PriorityRuleDto>>
    {
        private readonly IRepository<PriorityRule> _rules;

        public GetPriorityRulesQueryHandler(IRepository<PriorityRule> rules)
        {
            _rules = rules;
        }

        public async Task<IReadOnlyList<PriorityRuleDto>> Handle(GetPriorityRulesQuery request, CancellationToken cancellationToken)
        {
            var rules = await _rules.GetAllAsync(cancellationToken);
            return rules
                .OrderBy(r => r.PriorityLevel)
                .Select(r => new PriorityRuleDto(r.Id, r.Name, r.PriorityLevel, r.Description))
                .ToList();
        }
    }
}