namespace NjuCsCmsHelper.Server.Controllers;

using Models;
using Services;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SubmissionController : ControllerBase
{
    private readonly ILogger<SubmissionController> logger;
    private readonly AppDbContext dbContext;
    private readonly IAuthorizationService authorizationService;
    private readonly IMyAppService attachmentService;

    public SubmissionController(ILogger<SubmissionController> logger, AppDbContext dbContext,
                                IAuthorizationService authorizationService, IMyAppService attachmentService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.authorizationService = authorizationService;
        this.attachmentService = attachmentService;
    }

    /// <summary>提交作业</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="studentId">提交人</param>
    /// <param name="file">附件</param>
    [HttpPost]
    [Route("Submit")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromForm] int studentId, [FromForm] int assignmentId,
                                            [FromForm] IFormFile file)
    {
        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Unauthorized();

        if (file.Length > AppConfig.AttachmentSizeLimit) return BadRequest("文件过大");

        if (!await dbContext.Assignments.AnyAsync(a => a.Id == assignmentId)) return NotFound("assignmentId");
        if (!await dbContext.Students.AnyAsync(s => s.Id == studentId)) return NotFound("studentId");

        var submission = new Submission {
            StudentId = studentId,
            AssignmentId = assignmentId,
            SubmittedAt = DateTimeOffset.Now,
        };
        await attachmentService.AddSubmissionAsync(submission);
        await attachmentService.AddAttachmentAsync(submission, file.FileName, file.OpenReadStream());

        return Ok();
    }
}