using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateSequence(
                name: "bookingseg",
                schema: "booking",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "buyerseg",
                schema: "booking",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "orderitemseq",
                schema: "booking",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "bookingstatus",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookingstatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "buyers",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    IdentityGuid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buyers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cardtypes",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cardtypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationEventLog",
                schema: "booking",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTypeName = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    TimesSent = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEventLog", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "requests",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "paymentmethods",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CardNumber = table.Column<string>(type: "text", nullable: true),
                    SecurityNumber = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    CardHolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Expiration = table.Column<DateTime>(type: "timestamp with time zone", maxLength: 25, nullable: false),
                    _cardTypeId = table.Column<int>(type: "integer", nullable: false),
                    BuyerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paymentmethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paymentmethods_buyers_BuyerId",
                        column: x => x.BuyerId,
                        principalSchema: "booking",
                        principalTable: "buyers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_paymentmethods_cardtypes__cardTypeId",
                        column: x => x._cardTypeId,
                        principalSchema: "booking",
                        principalTable: "cardtypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    HallId = table.Column<int>(type: "integer", nullable: false),
                    BookingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    _bookingStatusId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookings_bookingstatus__bookingStatusId",
                        column: x => x._bookingStatusId,
                        principalSchema: "booking",
                        principalTable: "bookingstatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookings_buyers_UserId",
                        column: x => x.UserId,
                        principalSchema: "booking",
                        principalTable: "buyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookings_paymentmethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalSchema: "booking",
                        principalTable: "paymentmethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bookingitems",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    SeatId = table.Column<int>(type: "integer", nullable: false),
                    SeatCode = table.Column<string>(type: "text", nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookingitems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookingitems_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "bookings",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "booking",
                table: "bookingstatus",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "submitted" },
                    { 2, "awaitingseatvalidation" },
                    { 3, "seatconfirmed" },
                    { 4, "paid" },
                    { 5, "cancelled" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookingitems_BookingId",
                schema: "booking",
                table: "bookingitems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings__bookingStatusId",
                schema: "booking",
                table: "bookings",
                column: "_bookingStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_PaymentMethodId",
                schema: "booking",
                table: "bookings",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                schema: "booking",
                table: "bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_buyers_IdentityGuid",
                schema: "booking",
                table: "buyers",
                column: "IdentityGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paymentmethods__cardTypeId",
                schema: "booking",
                table: "paymentmethods",
                column: "_cardTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_paymentmethods_BuyerId",
                schema: "booking",
                table: "paymentmethods",
                column: "BuyerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookingitems",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "IntegrationEventLog",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "requests",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "bookingstatus",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "paymentmethods",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "buyers",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "cardtypes",
                schema: "booking");

            migrationBuilder.DropSequence(
                name: "bookingseg",
                schema: "booking");

            migrationBuilder.DropSequence(
                name: "buyerseg",
                schema: "booking");

            migrationBuilder.DropSequence(
                name: "orderitemseq",
                schema: "booking");
        }
    }
}
