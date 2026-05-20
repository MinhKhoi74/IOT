using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchScopeToParkingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.Guid>(
                name: "BranchId",
                table: "VehicleLocationDetections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<System.Guid>(
                name: "BranchId",
                table: "CheckInOuts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocation_BranchId",
                table: "VehicleLocationDetections",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInOut_BranchId",
                table: "CheckInOuts",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInOuts_Branches_BranchId",
                table: "CheckInOuts",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleLocationDetections_Branches_BranchId",
                table: "VehicleLocationDetections",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckInOuts_Branches_BranchId",
                table: "CheckInOuts");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleLocationDetections_Branches_BranchId",
                table: "VehicleLocationDetections");

            migrationBuilder.DropIndex(
                name: "IX_VehicleLocation_BranchId",
                table: "VehicleLocationDetections");

            migrationBuilder.DropIndex(
                name: "IX_CheckInOut_BranchId",
                table: "CheckInOuts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "VehicleLocationDetections");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CheckInOuts");
        }
    }
}
