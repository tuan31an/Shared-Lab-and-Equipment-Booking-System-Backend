namespace LabBooking.Application.Contracts
{
    public record PriorityRuleDto(Guid Id, string Name, int PriorityLevel, string? Description);
}