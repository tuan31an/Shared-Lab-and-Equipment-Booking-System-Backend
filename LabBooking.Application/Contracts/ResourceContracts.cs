namespace LabBooking.Application.Contracts
{
    public record DepartmentDto(Guid Id, string Name);

    public record ResourceDto(
        Guid Id,
        string Name,
        string Type,
        string? Specifications,
        string? ImageUrl,
        string? UsageRules,
        Guid? DepartmentId,
        string? DepartmentName,
        Guid? LabManagerId,
        string? LabManagerName,
        string Status,
        DateTime CreatedAt);
}