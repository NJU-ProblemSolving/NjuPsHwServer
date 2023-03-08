namespace NjuCsCmsHelper.Server.Controllers;

using Datas;
using Models;
using Services;
using Utils;

[Route("api/[controller]/{assignmentId:int}")]
[ApiController]
[Authorize("Reviewer")]
public class ReviewController : AppControllerBase<ReviewController>
{
    public ReviewController(IServiceProvider provider) : base(provider) { }

    /// <summary>获取评阅结果</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="reviewerId">评阅人ID，函数将获取该评阅人的评阅结果，为空时获取本次作业的所有结果</param>
    /// <returns>评阅结果列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ReviewInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReview(int assignmentId, int? reviewerId)
    {
        if (await GetAssignment(assignmentId) == null)
            return NotFound("Assignment ID not exists");

        var submissions = dbContext.Submissions.Where(submission => submission.AssignmentId == assignmentId);
        if (reviewerId != null)
            submissions = submissions.Where(submission => submission.Student.ReviewerId == reviewerId);

        var infos = await submissions
                        .Select(submission => new ReviewInfoDto
                        {
                            StudentId = submission.StudentId,
                            StudentName = submission.Student.Name,
                            SubmittedAt = submission.SubmittedAt,
                            Grade = submission.Grade,
                            NeedCorrection = submission.NeedCorrection.OrderBy(m => m.AssignmentId).ThenBy(m => m.ProblemId)
                                                 .Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                                                 .ToList(),
                            HasCorrected =
                                submission.HasCorrected.OrderBy(m => m.AssignmentId).ThenBy(m => m.ProblemId).Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                                    .ToList(),
                            Comment = submission.Comment,
                            Track = submission.Track,
                        })
                        .ToListAsync();

        foreach (var info in infos)
        {
            await Task.WhenAll(info.NeedCorrection.Select(x => myAppService.FillProblemDTO(x)));
            await Task.WhenAll(info.HasCorrected.Select(x => myAppService.FillProblemDTO(x)));
        }

        return Ok(infos);
    }

    /// <summary>更新评阅结果</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="reviewResults">更新后的评阅列表</param>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateReview(int assignmentId, [FromBody] List<ReviewInfoDto> reviewResults)
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
        return NoContent();
    }

    private Task<bool> IsAssignmentIdExists(int assignmentId) => dbContext.Assignments.AnyAsync(a => a.Id == assignmentId);
    private Task<Assignment?> GetAssignment(int assignmentId) => dbContext.Assignments.SingleOrDefaultAsync(a => a.Id == assignmentId);

    private async Task SetMistake(ICollection<MistakeDto> problemList, int studentId, Submission submission)
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
    private async Task CorrectMistake(ICollection<MistakeDto> problemList, int studentId, Submission submission)
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviewAchieve(int assignmentId, int? reviewerId)
    {
        var assignment = await dbContext.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId);
        if (assignment == null)
            return NotFound("Assignment not exists");

        var query = dbContext.Attachments
                .Where(x => x.Submission.AssignmentId == assignmentId);

        if (reviewerId is not null)
        {
            query = query.Where(x => x.Submission.Student.ReviewerId == reviewerId);
        }

        var attachments = await query
                .Select(x => new AttachmentInfo
                {
                    AttachmentId = x.Id,
                    AttachmentFilename = $"{x.Submission.StudentId}-{x.Submission.Student.Name}--{x.Filename}",
                }).ToListAsync();

        var tempFile = new TempFile();
        using (var writeStream = tempFile.OpenWrite())
        {
            await submissionService.GetArchiveAsync(assignmentId, reviewerId, assignment.Name, attachments, writeStream);
        }
        var readStream = tempFile.OpenRead(true);
        return File(readStream, "application/octet-stream", $"{assignment.Name}.zip");
    }

    /// <summary>重新随机评阅人</summary>
    [HttpPost]
    [Route("ReassignReviewer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReassignReviewer()
    {
        var students = await dbContext.Students.ToListAsync<Student?>();
        var reviewerIds = AppConfig.ReviewerName.Keys.ToList();
        if (students.Count % reviewerIds.Count != 0) {
            students.AddRange(Enumerable.Repeat<Student?>(null, reviewerIds.Count - students.Count % reviewerIds.Count));
        }
        students.Shuffle();

        foreach (var (student, i) in students.Select((s, i) => (s, i)))
        {
            if (student != null) {
                student.ReviewerId = reviewerIds[i % reviewerIds.Count];
            }
        }
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}

public class ReviewInfoDto
{
    [Required]
    public int StudentId { get; set; }
    [Required]
    public string StudentName { get; set; } = null!;
    [Required]
    public DateTimeOffset SubmittedAt { get; set; }
    [Required]
    public Grade Grade { get; set; } = Grade.None;
    [Required]
    public List<MistakeDto> NeedCorrection { get; set; } = new();
    [Required]
    public List<MistakeDto> HasCorrected { get; set; } = new();
    [Required(AllowEmptyStrings = true)]
    public string Comment { get; set; } = "";
    [Required(AllowEmptyStrings = true)]
    public string Track { get; set; } = "";
}
