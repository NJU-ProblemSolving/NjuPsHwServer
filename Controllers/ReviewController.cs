namespace NjuCsCmsHelper.Server.Controllers;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Features;
using Services;
using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReviewController : ControllerBase
{
    private readonly ILogger<ReviewController> logger;
    private readonly AppDbContext dbContext;
    private readonly IAuthorizationService authorizationService;
    private readonly IAttachmentService attachmentService;

    private static readonly Regex ProblemNameRegex = new(@"(\d+)-(\d+)");

    public ReviewController(ILogger<ReviewController> logger, AppDbContext dbContext,
                            IAuthorizationService authorizationService, IAttachmentService attachmentService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.authorizationService = authorizationService;
        this.attachmentService = attachmentService;
    }

    /// <summary>获取评阅结果</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="reviewerId">评阅人ID，函数将获取该评阅人的评阅结果，为空时获取本次作业的所有结果</param>
    /// <returns>评阅结果列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ReviewInfoDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int assignmentId, int? reviewerId)
    {
        if (!await IsAssignmentIdExists(assignmentId)) return NotFound("Assignment ID not exists");

        var submissions = dbContext.Submissions.Where(submission => submission.AssignmentId == assignmentId);
        if (reviewerId != null)
            submissions = submissions.Where(submission => submission.Student.ReviewerId == reviewerId);

        var infos = await submissions
                        .Select(submission => new ReviewInfoDTO {
                            StudentId = submission.StudentId,
                            StudentName = submission.Student.Name,
                            SubmittedAt = submission.SubmittedAt,
                            Grade = submission.Grade,
                            NeedCorrection = submission.NeedCorrection
                                                 .Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                                                 .ToList(),
                            HasCorrected =
                                submission.HasCorrected.Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                                    .ToList(),
                            Comment = submission.Comment,
                            Track = submission.Track,
                        })
                        .ToListAsync();

        return Ok(infos);
    }

    /// <summary>更新评阅结果</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="reviewResults">更新后的评阅列表</param>
    [HttpPost]
    public async Task<IActionResult> Update(int assignmentId, [FromBody] List<ReviewInfoDTO> reviewResults)
    {
        if (!await IsAssignmentIdExists(assignmentId)) return NotFound("Assignment ID not exists");

        foreach (var result in reviewResults)
        {
            var submission = await dbContext.Submissions.SingleOrDefaultAsync(
                submission => submission.StudentId == result.StudentId && submission.AssignmentId == assignmentId);
            if (submission is null)
                return NotFound($"#{result.StudentId}'s submission for assignment {assignmentId} not exists");

            try
            {
                submission.Grade = result.Grade;
                submission.Comment = result.Comment;
                submission.Track = result.Track;
                await SetMistake(result.NeedCorrection, result.StudentId, submission);
                await dbContext.Mistakes.Where(m => m.CorrectedInId == submission.Id)
                    .ForEachAsync(m => m.CorrectedIn = null);
                await CorrectMistake(result.HasCorrected, result.StudentId, submission);
            }
            catch (HttpResponseException ex)
            {
                return new ObjectResult(ex.Value) {
                    StatusCode = ex.Status,
                };
            }
        }

        await dbContext.SaveChangesAsync();
        return Ok();
    }

    private Task<bool> IsAssignmentIdExists(int assignmentId) => dbContext.Assignments.AnyAsync(a => a.Id ==
                                                                                                     assignmentId);
    private Task<bool> IsStudentIdExists(int studentId) => dbContext.Students.AnyAsync(s => s.Id == studentId);

    private (int, int) ParseProblemId(string problemName)
    {
        var matchResult = ProblemNameRegex.Match(problemName);
        if (!matchResult.Success) throw new FormatException($"Invalid problem name `{problemName}`");
        var assignmentId = int.Parse(matchResult.Groups[1].Value);
        var problemId = int.Parse(matchResult.Groups[2].Value);
        return (assignmentId, problemId);
    }
    private async Task SetMistake(ICollection<string> problemNameList, int studentId, Submission submission)
    {
        var mistakes = await dbContext.Mistakes.Where(m => m.MakedInId == submission.Id).ToListAsync();
        var taskList = problemNameList.Select(async problem => {
            var (assignmentId, problemId) = ParseProblemId(problem);
            var mistake = mistakes.SingleOrDefault(mistake => mistake.AssignmentId == assignmentId &&
                                                              mistake.ProblemId == problemId);
            if (mistake != null)
            {
                mistakes.Remove(mistake);
                return;
            }
            if (!await IsAssignmentIdExists(assignmentId))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                                $"Assignemnt ID `{assignmentId} not exists");
            mistake = new Mistake {
                StudentId = studentId,
                AssignmentId = assignmentId,
                ProblemId = problemId,
                MakedIn = submission,
            };
            await dbContext.Mistakes.AddAsync(mistake);
        });
        await Task.WhenAll(taskList);
        dbContext.Mistakes.RemoveRange(mistakes);
    }
    private async Task CorrectMistake(ICollection<string> problemNameList, int studentId, Submission submission)
    {
        var taskList = problemNameList.Select(async problem => {
            var (assignmentId, problemId) = ParseProblemId(problem);
            var mistake =
                await dbContext.Mistakes
                    .Where(m => m.StudentId == studentId && m.AssignmentId == assignmentId && m.ProblemId == problemId)
                    .SingleOrDefaultAsync();
            if (mistake == null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                                                $"Student {studentId} didn't make mistake at {problem}");
            if (mistake.CorrectedIn != null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                                                $"Student {studentId} has corrected {problem}");
            mistake.CorrectedIn = submission;
            return mistake;
        });
        await Task.WhenAll(taskList);
    }

    /// <summary>获取评阅压缩包</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="reviewerId">评阅人ID</param>
    /// <returns>包含需批改的作业、结果发送脚本的 zip 压缩包</returns>
    [HttpGet]
    [Route("Archieve")]
    [Produces("application/octet-stream")]
    public async Task GetReviewAchieve(int assignmentId, int reviewerId)
    {
        var attachments =
            await dbContext.Attachments
                .Where(x => x.Submission.AssignmentId == assignmentId && x.Submission.Student.ReviewerId == reviewerId)
                .ToListAsync();
        var info = attachments
                       .Select(x => new AttachmentInfo {
                           AttachmentId = x.Id,
                           AttachmentFilename = $"{x.Submission.StudentId}-{x.Submission.Student.Name}--{x.Filename}",
                       })
                       .ToList();

        this.Response.StatusCode = 200;
        this.Response.Headers.ContentDisposition = "attachment; filename=\"achieve.zip\"";
        this.Response.Headers.ContentType = "application/octet-stream";
        var feature = HttpContext.Features.Get<IHttpBodyControlFeature>() !;
        feature.AllowSynchronousIO = true;
        await Task.Run(() => attachmentService.GetArchiveAsync(info, Response.Body));
    }
}

public class ReviewInfoDTO
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Grade Grade { get; set; } = Grade.None;
    public List<string> NeedCorrection { get; set; } = new List<string>();
    public List<string> HasCorrected { get; set; } = new List<string>();
    public string Comment { get; set; } = "";
    public string Track { get; set; } = "";
}
