using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabBooking.Infrastructure.Sqlserver.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViolationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Violations_BookingId",
                table: "Violations");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_BookingId_Type",
                table: "Violations",
                columns: new[] { "BookingId", "Type" },
                unique: true,
                filter: "[BookingId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Violations_BookingId_Type",
                table: "Violations");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_BookingId",
                table: "Violations",
                column: "BookingId");
        }
    }
}
