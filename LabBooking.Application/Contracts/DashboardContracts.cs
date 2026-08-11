namespace LabBooking.Application.Contracts
{
    public record ResourceUsageDto(
        Guid ResourceId,
        string? ResourceName,
        string? DepartmentName,
        int BookedMinutes,
        int ActualMinutes,
        decimal UsagePercent);

    public record DepartmentUsageDto(
        Guid DepartmentId,
        string? DepartmentName,
        int BookedMinutes,
        int ActualMinutes,
        decimal UsagePercent);

    public record UsageDashboardDto(
        DateTime From,
        DateTime To,
        decimal OverallUsagePercent,
        int TotalBookedMinutes,
        int TotalActualMinutes,
        IReadOnlyList<ResourceUsageDto> ByResource,
        IReadOnlyList<DepartmentUsageDto> ByDepartment);

    public record MaintenanceCostByResourceDto(
        Guid ResourceId,
        string? ResourceName,
        int MaintenanceCount,
        decimal? TotalCost);

    public record MaintenanceReportDto(
        DateTime From,
        DateTime To,
        int TotalCount,
        decimal? TotalCost,
        IReadOnlyList<MaintenanceDto> Items,
        IReadOnlyList<MaintenanceCostByResourceDto> ByResource);
}