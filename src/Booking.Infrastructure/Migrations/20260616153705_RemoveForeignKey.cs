using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_buyers_UserId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_UserId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "booking",
                table: "bookings",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "BuyerId",
                schema: "booking",
                table: "bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_BuyerId",
                schema: "booking",
                table: "bookings",
                column: "BuyerId");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_buyers_BuyerId",
                schema: "booking",
                table: "bookings",
                column: "BuyerId",
                principalSchema: "booking",
                principalTable: "buyers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_buyers_BuyerId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_BuyerId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "BuyerId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "booking",
                table: "bookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                schema: "booking",
                table: "bookings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_buyers_UserId",
                schema: "booking",
                table: "bookings",
                column: "UserId",
                principalSchema: "booking",
                principalTable: "buyers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
