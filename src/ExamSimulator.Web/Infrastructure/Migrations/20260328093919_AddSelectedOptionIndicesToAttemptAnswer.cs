using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSimulator.Web.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedOptionIndicesToAttemptAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionIndices",
                table: "ExamAttemptAnswers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedOptionIndices",
                table: "ExamAttemptAnswers");
        }
    }
}
