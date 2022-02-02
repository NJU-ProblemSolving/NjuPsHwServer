using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NjuCsCmsHelper.Server.Migrations
{
    public partial class V3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false)
                                                 .Annotation("Sqlite:Autoincrement", true),
                                        SubmissionId = table.Column<int>(type: "INTEGER", nullable: false),
                                        FileName = table.Column<string>(type: "TEXT", nullable: false) },
                constraints: table => {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(name: "FK_Attachments_Submissions_SubmissionId", column: x => x.SubmissionId,
                                     principalTable: "Submissions", principalColumn: "Id",
                                     onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(name: "Tokens",
                                         columns: table =>
                                             new { Id = table.Column<string>(type: "TEXT", nullable: false),
                                                   StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                                                   IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false) },
                                         constraints: table => { table.PrimaryKey("PK_Tokens", x => x.Id); });

            migrationBuilder.CreateIndex(name: "IX_Attachments_SubmissionId", table: "Attachments",
                                         column: "SubmissionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Attachments");

            migrationBuilder.DropTable(name: "Tokens");
        }
    }
}
