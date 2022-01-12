using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NjuCsCmsHelper.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false)
                                                 .Annotation("Sqlite:Autoincrement", true),
                                        NumberOfProblems = table.Column<int>(type: "INTEGER", nullable: false),
                                        DeadLine = table.Column<long>(type: "INTEGER", nullable: false) },
                constraints: table => { table.PrimaryKey("PK_Assignments", x => x.Id); });

            migrationBuilder.CreateTable(name: "Students",
                                         columns: table =>
                                             new { Id = table.Column<int>(type: "INTEGER", nullable: false)
                                                            .Annotation("Sqlite:Autoincrement", true),
                                                   Name = table.Column<string>(type: "TEXT", nullable: false),
                                                   ReviewerId = table.Column<int>(type: "INTEGER", nullable: false) },
                                         constraints: table => { table.PrimaryKey("PK_Students", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false)
                                                 .Annotation("Sqlite:Autoincrement", true),
                                        StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                                        AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                                        SubmittedAt = table.Column<long>(type: "INTEGER", nullable: false),
                                        Grade = table.Column<int>(type: "INTEGER", nullable: false),
                                        Comment = table.Column<string>(type: "TEXT", nullable: false),
                                        Track = table.Column<string>(type: "TEXT", nullable: false) },
                constraints: table => {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(name: "FK_Submissions_Assignments_AssignmentId", column: x => x.AssignmentId,
                                     principalTable: "Assignments", principalColumn: "Id",
                                     onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_Submissions_Students_StudentId", column: x => x.StudentId,
                                     principalTable: "Students", principalColumn: "Id",
                                     onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mistakes",
                columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false)
                                                 .Annotation("Sqlite:Autoincrement", true),
                                        StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                                        AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                                        ProblemId = table.Column<int>(type: "INTEGER", nullable: false),
                                        MakedInId = table.Column<int>(type: "INTEGER", nullable: false),
                                        CorrectedInId = table.Column<int>(type: "INTEGER", nullable: true) },
                constraints: table => {
                    table.PrimaryKey("PK_Mistakes", x => x.Id);
                    table.ForeignKey(name: "FK_Mistakes_Assignments_AssignmentId", column: x => x.AssignmentId,
                                     principalTable: "Assignments", principalColumn: "Id",
                                     onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_Mistakes_Students_StudentId", column: x => x.StudentId,
                                     principalTable: "Students", principalColumn: "Id",
                                     onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_Mistakes_Submissions_CorrectedInId", column: x => x.CorrectedInId,
                                     principalTable: "Submissions", principalColumn: "Id");
                    table.ForeignKey(name: "FK_Mistakes_Submissions_MakedInId", column: x => x.MakedInId,
                                     principalTable: "Submissions", principalColumn: "Id",
                                     onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_Mistakes_AssignmentId", table: "Mistakes", column: "AssignmentId");

            migrationBuilder.CreateIndex(name: "IX_Mistakes_CorrectedInId", table: "Mistakes", column: "CorrectedInId");

            migrationBuilder.CreateIndex(name: "IX_Mistakes_MakedInId", table: "Mistakes", column: "MakedInId");

            migrationBuilder.CreateIndex(name: "IX_Mistakes_StudentId", table: "Mistakes", column: "StudentId");

            migrationBuilder.CreateIndex(name: "IX_Submissions_AssignmentId", table: "Submissions",
                                         column: "AssignmentId");

            migrationBuilder.CreateIndex(name: "IX_Submissions_StudentId", table: "Submissions", column: "StudentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Mistakes");

            migrationBuilder.DropTable(name: "Submissions");

            migrationBuilder.DropTable(name: "Assignments");

            migrationBuilder.DropTable(name: "Students");
        }
    }
}
