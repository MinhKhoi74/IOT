using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParking.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyPassPricingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<System.DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Key", "Value" },
                values: new object[] { "MonthlyPass.MonthlyAmount", "200000" });

            migrationBuilder.Sql("""
                UPDATE mp
                SET Amount = revenue.Amount
                FROM MonthlyPasses mp
                CROSS APPLY (
                    SELECT TOP (1) CAST(ABS(wt.Amount) AS decimal(18, 2)) AS Amount
                    FROM WalletTransactions wt
                    WHERE wt.ReferenceType = 'MonthlyPass'
                      AND wt.Type = 'MonthlyPassRevenue'
                      AND wt.Amount > 0
                      AND UPPER(wt.Description) LIKE '%' + UPPER(mp.LicensePlate)
                    ORDER BY wt.CreatedAt DESC
                ) revenue
                WHERE mp.Amount = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
