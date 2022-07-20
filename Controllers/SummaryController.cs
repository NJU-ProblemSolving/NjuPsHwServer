namespace NjuCsCmsHelper.Server.Controllers;

using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize("Admin")]
public class SummaryController : ControllerBase
{
    private readonly ILogger<SummaryController> logger;
    private readonly AppDbContext dbContext;

    public SummaryController(ILogger<SummaryController> logger, AppDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    /// <summary>期末汇总成绩</summary>
    [HttpGet]
    public async Task<IActionResult> GetSummary([FromQuery] List<int> exceptionList)
    {
        var assignmentIds = await dbContext.Assignments.Select(a => a.Id).OrderBy(x => x).ToListAsync();
        assignmentIds = assignmentIds.Except(exceptionList).ToList();
        var assignments = new Dictionary<int, Assignment>();
        foreach (var assignmentId in assignmentIds)
        {
            assignments.Add(assignmentId, await dbContext.Assignments.SingleAsync(x => x.Id == assignmentId));
        }

        var res = new System.Text.StringBuilder();
        res.Append("学号, 姓名");
        foreach (var assignmentId in assignmentIds) { res.Append(", ").Append(assignments[assignmentId].Name); }
        res.AppendLine(", 总分");

        foreach (var student in dbContext.Students)
        {
            res.Append(NumberFormatInfo.InvariantInfo, $"{student.Id}, {student.Name}");
            var totalScore = 0.0;
            foreach (var assignmentId in assignmentIds)
            {
                var submission =
                    await dbContext.Submissions.Where(x => x.StudentId == student.Id && x.AssignmentId == assignmentId)
                        .Select(x => new
                        {
                            x.Grade,
                            x.SubmittedAt,
                            Corrected = x.NeedCorrection.Where(y => y.CorrectedIn != null).Count(),
                            Total = x.NeedCorrection.Count,
                        })
                        .SingleOrDefaultAsync();

                if (submission == null) { res.Append(", 未提交"); }
                else
                {
                    var basicScore = submission.Grade.ToScore();
                    totalScore += basicScore;
                    res.Append(", ").Append(submission.Grade.GetDescription());
                    // 迟交
                    if (submission.SubmittedAt > assignments[assignmentId].Deadline)
                    {
                        res.Append('*');
                        totalScore -= 10;
                    }
                    if (submission.Total != 0)
                    {
                        // 订正完成返还 10%，未订正加扣 30%
                        var revisionScore = 0.1 * submission.Corrected - 0.3 * (submission.Total - submission.Corrected);
                        revisionScore = MathF.Max(100 - basicScore, 20) * revisionScore / submission.Total;
                        totalScore += revisionScore;
                        res.Append(NumberFormatInfo.InvariantInfo, $"({submission.Corrected}/{submission.Total})");
                    }
                }
            }
            res.AppendLine(NumberFormatInfo.InvariantInfo, $", {totalScore / assignmentIds.Count}");
        }

        return Ok(res.ToString());
    }
}
