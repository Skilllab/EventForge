using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventBookingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_end_at",
                schema: "EventBooking",
                table: "Events",
                column: "end_at");

            migrationBuilder.CreateIndex(
                name: "IX_Events_start_at",
                schema: "EventBooking",
                table: "Events",
                column: "start_at");

            migrationBuilder.CreateIndex(
                name: "IX_Events_title",
                schema: "EventBooking",
                table: "Events",
                column: "title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_end_at",
                schema: "EventBooking",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_start_at",
                schema: "EventBooking",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_title",
                schema: "EventBooking",
                table: "Events");
        }
    }
}
