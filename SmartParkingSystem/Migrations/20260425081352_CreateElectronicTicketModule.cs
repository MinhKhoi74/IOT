using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class CreateElectronicTicketModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ElectronicTickets_Bookings_BookingId",
                table: "ElectronicTickets");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicTickets_BookingId",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "ElectronicTickets");

            migrationBuilder.RenameColumn(
                name: "ValidUntil",
                table: "ElectronicTickets",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "ElectronicTickets",
                newName: "CheckInDateTime");

            migrationBuilder.RenameIndex(
                name: "IX_ElectronicTickets_TicketCode",
                table: "ElectronicTickets",
                newName: "IX_ElectronicTicket_TicketCode");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ElectronicTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "ElectronicTickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutDateTime",
                table: "ElectronicTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "ElectronicTickets",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "ElectronicTickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParkingLotName",
                table: "ElectronicTickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "ElectronicTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToUserAt",
                table: "ElectronicTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ElectronicTickets",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTicket_CheckInDateTime",
                table: "ElectronicTickets",
                column: "CheckInDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTicket_CreatedAt",
                table: "ElectronicTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTicket_LicensePlate",
                table: "ElectronicTickets",
                column: "LicensePlate");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTicket_Status",
                table: "ElectronicTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTicket_UserId",
                table: "ElectronicTickets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ElectronicTicket_User",
                table: "ElectronicTickets",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ElectronicTicket_User",
                table: "ElectronicTickets");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicTicket_CheckInDateTime",
                table: "ElectronicTickets");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicTicket_CreatedAt",
                table: "ElectronicTickets");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicTicket_LicensePlate",
                table: "ElectronicTickets");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicTicket_Status",
                table: "ElectronicTickets");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicTicket_UserId",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "CheckOutDateTime",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "ParkingLotName",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "SentToUserAt",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ElectronicTickets");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ElectronicTickets",
                newName: "ValidUntil");

            migrationBuilder.RenameColumn(
                name: "CheckInDateTime",
                table: "ElectronicTickets",
                newName: "IssuedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ElectronicTicket_TicketCode",
                table: "ElectronicTickets",
                newName: "IX_ElectronicTickets_TicketCode");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "ElectronicTickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bookings_Slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "Slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bookings_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_BookingId",
                table: "ElectronicTickets",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotId",
                table: "Bookings",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleId",
                table: "Bookings",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ElectronicTickets_Bookings_BookingId",
                table: "ElectronicTickets",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
