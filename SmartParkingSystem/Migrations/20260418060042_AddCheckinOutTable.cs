using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckinOutTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckInOuts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlateImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckInStationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckInImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutStationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckOutImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    FeeAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    FeeCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeeStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInOuts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInTime",
                table: "CheckInOuts",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStatus",
                table: "CheckInOuts",
                column: "FeeStatus");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlate_Active",
                table: "CheckInOuts",
                columns: new[] { "LicensePlate", "Status" },
                filter: "[Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInOuts");
        }
    }
}
