using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NjuCsCmsHelper.Server.Migrations
{
    public partial class TokenStudentIdIsForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "FileName", table: "Attachments", newName: "Filename");

            migrationBuilder.CreateIndex(name: "IX_Tokens_StudentId", table: "Tokens", column: "StudentId");

            migrationBuilder.AddForeignKey(name: "FK_Tokens_Students_StudentId", table: "Tokens", column: "StudentId",
                                           principalTable: "Students", principalColumn: "Id",
                                           onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Tokens_Students_StudentId", table: "Tokens");

            migrationBuilder.DropIndex(name: "IX_Tokens_StudentId", table: "Tokens");

            migrationBuilder.RenameColumn(name: "Filename", table: "Attachments", newName: "FileName");
        }
    }
}
