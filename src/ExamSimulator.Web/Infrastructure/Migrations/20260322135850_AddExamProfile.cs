using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSimulator.Web.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExamProfileId",
                table: "Questions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ExamProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamProfileId",
                table: "Questions",
                column: "ExamProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ExamProfiles_ExamProfileId",
                table: "Questions",
                column: "ExamProfileId",
                principalTable: "ExamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ExamProfiles_ExamProfileId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "ExamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ExamProfileId",
                table: "Questions");

            migrationBuilder.AlterColumn<string>(
                name: "ExamProfileId",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
