using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSimulator.Web.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeAndDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CorrectOptionIndex",
                table: "Questions",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "CorrectOptionIndices",
                table: "Questions",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Questions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "Questions",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectOptionIndices",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Questions",
                newName: "CorrectOptionIndex");
        }
    }
}
