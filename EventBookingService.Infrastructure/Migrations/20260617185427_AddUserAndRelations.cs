using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventBookingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndRelations : Migration
    {
        // Выносим Guid в константу для удобного использования в обоих методах
        private static readonly Guid DummyUserId = new Guid("11111111-1111-1111-1111-111111111111");
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //Сначала создаем таблицу с пользователями
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

            // Вставляем пользователя-заглушку для существующих бронирований
            migrationBuilder.Sql($@"
                INSERT INTO ""EventBooking"".""Users"" (id, login, password_hash, role)
                VALUES ('{DummyUserId}', 'dummy_user', 'no_password_hash', 'User');
            ");


            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "EventBooking",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: DummyUserId);

           

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_user_id",
                schema: "EventBooking",
                table: "Bookings");

          

            migrationBuilder.DropIndex(
                name: "IX_Bookings_user_id",
                schema: "EventBooking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "EventBooking",
                table: "Bookings");


            // Удаляем пользователя-заглушку
            migrationBuilder.Sql($@"
                DELETE FROM ""EventBooking"".""Users"" 
                WHERE id = '{DummyUserId}';
            ");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "EventBooking");
        }
    }
}
