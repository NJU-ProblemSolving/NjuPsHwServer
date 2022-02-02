using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NjuCsCmsHelper.Server.Migrations
{
    public partial class AddUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Mistakes_StudentId",
                table: "Mistakes");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId_AssignmentId",
                table: "Submissions",
                columns: new[] { "StudentId", "AssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mistakes_StudentId_AssignmentId_ProblemId",
                table: "Mistakes",
                columns: new[] { "StudentId", "AssignmentId", "ProblemId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_StudentId_AssignmentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Mistakes_StudentId_AssignmentId_ProblemId",
                table: "Mistakes");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Mistakes_StudentId",
                table: "Mistakes",
                column: "StudentId");
        }
    }
}
