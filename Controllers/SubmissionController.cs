namespace NjuCsCmsHelper.Server.Controllers;

using Datas;
using Models;
using Services;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SubmissionController : AppControllerBase<SubmissionController>
{
    public SubmissionController(IServiceProvider provider) : base(provider) { }

    /// <summary>获取某个学生的作业情况汇总</summary>
    [HttpGet]
    [Authorize]
    [Route("Summary")]
    [ProducesResponseType(typeof(List<SubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubmissionSummary([Required] int studentId)
    {
        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Forbid();

        var submissions = await dbContext.Submissions
            .Where(submission => submission.StudentId == studentId)
            .OrderBy(submission => submission.AssignmentId)
            .Select(submission => new SubmissionDto
            {
                AssignmentId = submission.AssignmentId,
                Grade = submission.Grade,
                SubmittedAt = submission.SubmittedAt,
                NeedCorrection = submission.NeedCorrection
                    .OrderBy(mistake => mistake.ProblemId)
                    .Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                    .ToList(),
                HasCorrected = submission.HasCorrected
                    .OrderBy(mistake => mistake.AssignmentId)
                    .ThenBy(mistake => mistake.ProblemId)
                    .Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                    .ToList(),
                Comment = submission.Comment,
            })
                              .ToListAsync();
        foreach (var s in submissions)
        {
            s.AssignmentName = await myAppService.GetAssignmentNameById(s.AssignmentId);
            await Task.WhenAll(s.NeedCorrection.Select(x => myAppService.FillProblemDTO(x)));
            await Task.WhenAll(s.HasCorrected.Select(x => myAppService.FillProblemDTO(x)));
        }
        return Ok(submissions);
    }

    /// <summary>提交作业</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="studentId">提交人</param>
    /// <param name="submittedAt">提交时间</param>
    /// <param name="file">附件</param>
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromForm, Required] int studentId, [FromForm, Required] int assignmentId, [FromForm] DateTimeOffset? submittedAt,
                                            IFormFile file)
    {
        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Forbid();

        if (!User.IsInRole("Admin") && file.Length > AppConfig.AttachmentSizeLimit) return BadRequest("File too big");

        if (!await dbContext.Assignments.AnyAsync(a => a.Id == assignmentId)) return NotFound("Assignment not found");
        if (!await dbContext.Students.AnyAsync(s => s.Id == studentId)) return NotFound("Student not found");
        if (submittedAt != null && !User.IsInRole("Admin")) return Forbid("Unauthorized to set submission time");

        var assignmentInDb = await dbContext.Assignments.SingleOrDefaultAsync(a => a.Id == assignmentId);
        var submissionInDb = await dbContext.Submissions.SingleOrDefaultAsync(s => s.StudentId == studentId && s.AssignmentId == assignmentId);
        if (assignmentInDb != null && submissionInDb != null) {
            if (submissionInDb.SubmittedAt >= assignmentInDb.Deadline) {
                return BadRequest("作业截止后已有一份提交（已锁定），无法再次提交。");
            }
            
            if (submissionInDb.Grade != Grade.None) {
                return BadRequest("此作业已经被批改（已锁定），无法再次提交。");
            }
        }
        
        var submission = new Submission
        {
            StudentId = studentId,
            AssignmentId = assignmentId,
            SubmittedAt = submittedAt ?? DateTimeOffset.Now,
        };
        if (await submissionService.AddSubmissionAsync(submission))
        {
            await submissionService.AddAttachmentAsync(submission, file.FileName, file.OpenReadStream());
            return CreatedAtAction(nameof(GetSubmissionSummary), new { studentId }, null);
        }
        else
        {
            return NoContent();
        }
    }

    /// <summary>获取学生提交的附件</summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="assignmentId">作业ID</param>
    /// <returns>包含附件的压缩包</returns>
    [HttpGet]
    [Route("Attachments")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionAttachments(int studentId, int assignmentId)
    {
        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Forbid();

        if (!await dbContext.Assignments.AnyAsync(a => a.Id == assignmentId)) return NotFound("Assignment not found");
        if (!await dbContext.Students.AnyAsync(s => s.Id == studentId)) return NotFound("Student not found");

        var attachments = await dbContext.Submissions.Where(s => s.StudentId == studentId && s.AssignmentId == assignmentId).Select(s => s.Attachments.ToList()).SingleOrDefaultAsync();
        if (attachments == null) return NotFound("Submission not found");

        if (attachments.Count == 0)
        {
            return NoContent();
        }
        else if (attachments.Count == 1)
        {
            var attachment = attachments.Single();
            var stream = submissionService.OpenRead(attachment.Id);
            return File(stream, "application/octet-stream", attachment.Filename);
        }
        else
        {
            return Forbid();
        }
    }

    /// <summary>删除作业提交</summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="assignmentId">作业ID</param>
    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSubmission(int studentId, int assignmentId)
    {
        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Forbid();

        if (!await dbContext.Assignments.AnyAsync(a => a.Id == assignmentId)) return NotFound("Assignment not found");
        if (!await dbContext.Students.AnyAsync(s => s.Id == studentId)) return NotFound("Student not found");

        var submission = await dbContext.Submissions.SingleOrDefaultAsync(s => s.StudentId == studentId && s.AssignmentId == assignmentId);
        if (submission == null) return NotFound("Submission not found");

        dbContext.Submissions.Remove(submission);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
