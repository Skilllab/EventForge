using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventBookingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_user_id",
                schema: "EventBooking",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "EventBooking");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_user_id",
                schema: "EventBooking",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                schema: "EventBooking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_user_id",
                schema: "EventBooking",
                table: "Bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_login",
                schema: "EventBooking",
                table: "Users",
                column: "login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_user_id",
                schema: "EventBooking",
                table: "Bookings",
                column: "user_id",
                principalSchema: "EventBooking",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
