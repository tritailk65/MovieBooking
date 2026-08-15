using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftPropertiesForBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatCode",
                schema: "booking",
                table: "bookingitems");

            migrationBuilder.AlterColumn<string>(
                name: "SeatId",
                schema: "booking",
                table: "bookingitems",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Showtime",
                schema: "booking",
                table: "bookingitems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Showtime",
                schema: "booking",
                table: "bookingitems");

            migrationBuilder.AlterColumn<int>(
                name: "SeatId",
                schema: "booking",
                table: "bookingitems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatCode",
                schema: "booking",
                table: "bookingitems",
                type: "text",
                nullable: true);
        }
    }
}
