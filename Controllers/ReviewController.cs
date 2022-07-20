namespace NjuCsCmsHelper.Server.Controllers;

using Models;
using Services;

[Route("api/[controller]")]
[ApiController]
[Authorize("Reviewer")]
public class ReviewController : ControllerBase
{
    private readonly ILogger<ReviewController> logger;
    private readonly AppDbContext dbContext;
    private readonly IMyAppService myAppService;
    private readonly SubmissionService submissionService;

    public ReviewController(ILogger<ReviewController> logger, AppDbContext dbContext, IMyAppService myAppService, SubmissionService submissionService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.myAppService = myAppService;
        this.submissionService = submissionService;
    }

    /// <summary>获取评阅结果</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="reviewerId">评阅人ID，函数将获取该评阅人的评阅结果，为空时获取本次作业的所有结果</param>
    /// <returns>评阅结果列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ReviewInfoDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int assignmentId, int? reviewerId)
    {
        if (await GetAssignment(assignmentId) == null)
            return NotFound("Assignment ID not exists");

        var submissions = dbContext.Submissions.Where(submission => submission.AssignmentId == assignmentId);
        if (reviewerId != null)
            submissions = submissions.Where(submission => submission.Student.ReviewerId == reviewerId);

        var infos = await submissions
                        .Select(submission => new ReviewInfoDTO
                        {
                            StudentId = submission.StudentId,
                            StudentName = submission.Student.Name,
                            SubmittedAt = submission.SubmittedAt,
                            Grade = submission.Grade,
                            NeedCorrection = submission.NeedCorrection.OrderBy(m => m.AssignmentId).ThenBy(m => m.ProblemId)
                                                 .Select(m => new ProblemDTO { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                                                 .ToList(),
                            HasCorrected =
                                submission.HasCorrected.OrderBy(m => m.AssignmentId).ThenBy(m => m.ProblemId).Select(m => new ProblemDTO { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                                    .ToList(),
                            Comment = submission.Comment,
                            Track = submission.Track,
                        })
                        .ToListAsync();

        foreach (var info in infos)
        {
            info.NeedCorrection.ForEach(x => myAppService.FillProblemDTO(x));
            info.HasCorrected.ForEach(x => myAppService.FillProblemDTO(x));
        }

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
                return new ObjectResult(ex.Value)
                {
                    StatusCode = ex.Status,
                };
            }
        }

        await dbContext.SaveChangesAsync();
        return Ok();
    }

    private Task<bool> IsAssignmentIdExists(int assignmentId) => dbContext.Assignments.AnyAsync(a => a.Id == assignmentId);
    private Task<Assignment?> GetAssignment(int assignmentId) => dbContext.Assignments.SingleOrDefaultAsync(a => a.Id == assignmentId);

    private async Task SetMistake(ICollection<ProblemDTO> problemList, int studentId, Submission submission)
    {
        var mistakes = await dbContext.Mistakes.Where(m => m.MadeInId == submission.Id).ToListAsync();
        var taskList = problemList.Select(async problem =>
        {
            var mistake = mistakes.SingleOrDefault(mistake => mistake.AssignmentId == problem.AssignmentId &&
                                                              mistake.ProblemId == problem.ProblemId);
            if (mistake != null)
            {
                mistakes.Remove(mistake);
                return;
            }
            if (!await IsAssignmentIdExists(problem.AssignmentId))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                                $"Assignemnt ID of problem {problem} not exists");
            mistake = new Mistake
            {
                StudentId = studentId,
                AssignmentId = problem.AssignmentId,
                ProblemId = problem.ProblemId,
                MadeIn = submission,
            };
            await dbContext.Mistakes.AddAsync(mistake);
        });
        await Task.WhenAll(taskList);
        dbContext.Mistakes.RemoveRange(mistakes);
    }
    private async Task CorrectMistake(ICollection<ProblemDTO> problemList, int studentId, Submission submission)
    {
        var taskList = problemList.Select(async problem =>
        {
            var mistake =
                await dbContext.Mistakes
                    .Where(m => m.StudentId == studentId && m.AssignmentId == problem.AssignmentId && m.ProblemId == problem.ProblemId)
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
    /// <returns>压缩包包含需批改的作业、发送反馈邮件的脚本。</returns>
    [HttpGet]
    [Route("Archieve")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewAchieve(int assignmentId, int reviewerId)
    {
        var assignment = await dbContext.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId);
        if (assignment == null)
            return NotFound("Assignment not exists");

        var attachments =
            await dbContext.Attachments
                .Where(x => x.Submission.AssignmentId == assignmentId && x.Submission.Student.ReviewerId == reviewerId)
                .Select(x => new AttachmentInfo
                {
                    AttachmentId = x.Id,
                    AttachmentFilename = $"{x.Submission.StudentId}-{x.Submission.Student.Name}--{x.Filename}",
                }).ToListAsync();

        var pipe = new System.IO.Pipelines.Pipe();
        await submissionService.GetArchiveAsync(assignmentId, reviewerId, assignment.Name, attachments, pipe.Writer.AsStream());
        return File(pipe.Reader.AsStream(), "application/octet-stream", $"{assignment.Name}.zip");
    }
}

public class ReviewInfoDTO
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Grade Grade { get; set; } = Grade.None;
    public List<ProblemDTO> NeedCorrection { get; set; } = new();
    public List<ProblemDTO> HasCorrected { get; set; } = new();
    public string Comment { get; set; } = "";
    public string Track { get; set; } = "";
}
