using Microsoft.EntityFrameworkCore.Migrations;

namespace WebPerformanceCalculator.Migrations
{
    public partial class AddIndices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Scores_UpdateTime_LocalPp",
                table: "Scores",
                columns: new[] { "UpdateTime", "LocalPp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scores_UpdateTime_LocalPp",
                table: "Scores");
        }
    }
}
