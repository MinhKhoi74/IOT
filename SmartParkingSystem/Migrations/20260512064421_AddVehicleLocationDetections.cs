using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleLocationDetections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleLocationDetections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CheckInOutId = table.Column<int>(type: "int", nullable: true),
                    CameraId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParkingLotCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ZoneCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColumnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    ImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullFrameImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLatest = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLocationDetections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleLocationDetections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehicleLocationDetections_CheckInOuts_CheckInOutId",
                        column: x => x.CheckInOutId,
                        principalTable: "CheckInOuts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehicleLocationDetections_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocation_DetectedAt",
                table: "VehicleLocationDetections",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocation_LatestByPlate",
                table: "VehicleLocationDetections",
                columns: new[] { "LicensePlate", "IsLatest" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocation_Status",
                table: "VehicleLocationDetections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocationDetections_CheckInOutId",
                table: "VehicleLocationDetections",
                column: "CheckInOutId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocationDetections_UserId",
                table: "VehicleLocationDetections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocationDetections_VehicleId",
                table: "VehicleLocationDetections",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleLocationDetections");
        }
    }
}
