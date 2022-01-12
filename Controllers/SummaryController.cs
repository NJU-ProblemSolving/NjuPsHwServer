using NjuCsCmsHelper.Models;

namespace NjuCsCmsHelper.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SummaryController : ControllerBase
{
    private readonly ILogger<StudentController> logger;
    private readonly Models.ApplicationDbContext dbContext;

    public SummaryController(ILogger<StudentController> logger, Models.ApplicationDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var assignments = await dbContext.Assignments.Select(a => a.Id).ToListAsync();
        // 除去订正提交
        assignments.Remove(13);

        var res = new System.Text.StringBuilder();
        res.Append("学号, 姓名");
        foreach (var assignment in assignments) { res.Append($", {assignment}, 订正"); }
        res.AppendLine(", 总分");

        foreach (var student in dbContext.Students.ToList())
        {
            res.Append($"{student.Id}, {student.Name}");
            var score = 0.0;
            foreach (var assignmentId in assignments)
            {
                var submission =
                    await dbContext.Submissions.Where(x => x.StudentId == student.Id && x.AssignmentId == assignmentId)
                        .Select(x => new {
                            Grade = x.Grade,
                            Corrected = x.NeedCorrection.Where(y => y.CorrectedIn != null).Count(),
                            Total = x.NeedCorrection.Count(),
                        })
                        .SingleOrDefaultAsync();

                if (submission == null) { res.Append(", 未提交, -"); }
                else
                {
                    score += submission.Grade switch {
                        Grade.A => 100, Grade.Aminus => 90, Grade.B => 80, Grade.Bminus => 75,
                        Grade.C => 70,  Grade.D => 60,      _ => 0,
                    };
                    res.Append($", {submission.Grade.ToDescriptionString()}");
                    if (submission.Total == 0)
                        res.Append($", -");
                    else
                        res.Append($", {submission.Corrected}/{submission.Total}");
                }
            }
            res.AppendLine($", {score / assignments.Count}");
        }

        return Ok(res.ToString());
    }
}