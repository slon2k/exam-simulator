using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSimulator.Web.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ExamProfileId = table.Column<string>(nullable: false),
                    Prompt = table.Column<string>(nullable: false),
                    Options = table.Column<string>(nullable: false),
                    CorrectOptionIndex = table.Column<int>(nullable: false),
                    TopicTag = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Questions");
        }
    }
}
