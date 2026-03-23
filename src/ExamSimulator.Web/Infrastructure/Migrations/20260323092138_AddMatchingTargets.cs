using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSimulator.Web.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchingTargets",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchingTargets",
                table: "Questions");
        }
    }
}
