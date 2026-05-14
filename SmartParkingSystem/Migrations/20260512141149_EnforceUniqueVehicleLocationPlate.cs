using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueVehicleLocationPlate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleLocation_LatestByPlate",
                table: "VehicleLocationDetections");

            migrationBuilder.Sql("""
                WITH RankedLocations AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY LicensePlate
                            ORDER BY DetectedAt DESC, Id DESC
                        ) AS RowNumber
                    FROM VehicleLocationDetections
                )
                DELETE FROM VehicleLocationDetections
                WHERE Id IN (
                    SELECT Id
                    FROM RankedLocations
                    WHERE RowNumber > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocation_UniquePlate",
                table: "VehicleLocationDetections",
                column: "LicensePlate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleLocation_UniquePlate",
                table: "VehicleLocationDetections");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLocation_LatestByPlate",
                table: "VehicleLocationDetections",
                columns: new[] { "LicensePlate", "IsLatest" });
        }
    }
}
