using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabBooking.Infrastructure.Sqlserver.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOverlapTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ràng buộc anti-chồng lấn ở tầng DB (backstop app-layer check):
            //  - Không cho hai Booking "giữ chỗ" (Pending/Approved) trùng khung giờ trên cùng resource.
            //  - Booking giữ chỗ không được chồng lấn với Maintenance chưa Completed.
            //  - Hai Maintenance chưa Completed không được chồng lấn trên cùng resource.
            // Enum lưu dạng int: BookingStatus Pending=0, Approved=1; MaintenanceStatus Completed=2.
            migrationBuilder.Sql("""
                IF OBJECT_ID('dbo.TR_Bookings_BlockOverlap', 'TR') IS NOT NULL
                    DROP TRIGGER dbo.TR_Bookings_BlockOverlap;
                GO
                CREATE TRIGGER dbo.TR_Bookings_BlockOverlap
                ON dbo.Bookings
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN dbo.Bookings b
                            ON b.ResourceId = i.ResourceId
                            AND b.Id <> i.Id
                        WHERE i.[Status] IN (0, 1)
                          AND b.[Status] IN (0, 1)
                          AND i.StartTime < b.EndTime
                          AND b.StartTime < i.EndTime
                    )
                    BEGIN
                        THROW 50001, N'Overlapping booking on the same resource is not allowed.', 16;
                    END

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN dbo.Maintenances m
                            ON m.ResourceId = i.ResourceId
                        WHERE i.[Status] IN (0, 1)
                          AND m.[Status] <> 2
                          AND i.StartTime < m.EndTime
                          AND m.StartTime < i.EndTime
                    )
                    BEGIN
                        THROW 50002, N'Booking overlaps a scheduled maintenance period.', 16;
                    END
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID('dbo.TR_Maintenances_BlockOverlap', 'TR') IS NOT NULL
                    DROP TRIGGER dbo.TR_Maintenances_BlockOverlap;
                GO
                CREATE TRIGGER dbo.TR_Maintenances_BlockOverlap
                ON dbo.Maintenances
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN dbo.Maintenances m
                            ON m.ResourceId = i.ResourceId
                            AND m.Id <> i.Id
                        WHERE i.[Status] <> 2
                          AND m.[Status] <> 2
                          AND i.StartTime < m.EndTime
                          AND m.StartTime < i.EndTime
                    )
                    BEGIN
                        THROW 51001, N'Overlapping maintenance on the same resource is not allowed.', 16;
                    END

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        JOIN dbo.Bookings b
                            ON b.ResourceId = i.ResourceId
                        WHERE i.[Status] <> 2
                          AND b.[Status] IN (0, 1)
                          AND i.StartTime < b.EndTime
                          AND b.StartTime < i.EndTime
                    )
                    BEGIN
                        THROW 51002, N'Maintenance overlaps an existing booking.', 16;
                    END
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_Bookings_BlockOverlap;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_Maintenances_BlockOverlap;");
        }
    }
}
