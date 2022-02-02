namespace NjuCsCmsHelper.Server.Controllers;

using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SummaryController : ControllerBase
{
    private readonly ILogger<SummaryController> logger;
    private readonly AppDbContext dbContext;

    public SummaryController(ILogger<SummaryController> logger, AppDbContext dbContext)
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
            var totalScore = 0.0;
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
                    var submissionScore = submission.Grade.ToScore();
                    totalScore += submissionScore;
                    res.Append($", {submission.Grade.GetDescription()}");
                    if (submission.Total == 0) { res.Append($", -"); }
                    else
                    {
                        totalScore += (100 - submissionScore) * 0.1 * submission.Corrected / submission.Total;
                        totalScore -= MathF.Max(100 - submissionScore, 10) * 0.5 *
                                      (submission.Total - submission.Corrected) / submission.Total;
                        res.Append($", {submission.Corrected}/{submission.Total}");
                    }
                }
            }
            res.AppendLine($", {totalScore / assignments.Count}");
        }

        return Ok(res.ToString());
    }
}