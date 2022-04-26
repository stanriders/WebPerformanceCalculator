using Microsoft.EntityFrameworkCore.Migrations;

namespace WebPerformanceCalculator.Migrations
{
    public partial class AddAdjustmentPercentage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AdjustmentPercentage",
                table: "Maps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentPercentage",
                table: "Maps");
        }
    }
}
