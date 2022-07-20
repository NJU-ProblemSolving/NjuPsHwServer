namespace NjuCsCmsHelper.Server.Controllers;

using Models;
using Services;

[Route("api/[controller]/{studentId:int?}")]
[ApiController]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly ILogger<StudentController> logger;
    private readonly AppDbContext dbContext;
    private readonly IAuthorizationService authorizationService;
    private readonly IMyAppService myAppService;
    private readonly SubmissionService submissionService;
    private readonly MailingService mailingService;

    public StudentController(ILogger<StudentController> logger, AppDbContext dbContext,
                             IAuthorizationService authorizationService,
                             IMyAppService myAppService, SubmissionService submissionService, MailingService mailingService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.authorizationService = authorizationService;
        this.myAppService = myAppService;
        this.submissionService = submissionService;
        this.mailingService = mailingService;
    }

    /// <summary>新建学生用户</summary>
    [HttpPost]
    [Route("Create")]
    [Authorize("Admin")]
    [ProducesResponseType(typeof(Student), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] Student student)
    {
        if (student is null) throw new ArgumentNullException(nameof(student));

        await dbContext.Students.AddAsync(student);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { studentId = student.Id }, student);
    }

    /// <summary>获取学生信息</summary>
    [HttpGet]
    [Route("")]
    [Authorize("Admin")]
    [ProducesResponseType(typeof(Student), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int? studentId)
    {
        if (studentId == null) return BadRequest("Expect student ID");

        var student = await dbContext.Students.Where(student => student.Id == studentId!).SingleOrDefaultAsync();
        if (student == null) return NotFound("Student ID not found");

        return Ok(student);
    }

    /// <summary>重置 Token 并发送邮件</summary>
    [HttpPost]
    [AllowAnonymous]
    [Route("ResetToken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetToken(int studentId)
    {
        var student = await dbContext.Students.Where(student => student.Id == studentId).SingleOrDefaultAsync();
        if (student == null) return NotFound("Student ID not found");

        var token = await dbContext.Tokens.FirstOrDefaultAsync(token => token.StudentId == studentId);
        if (token == null)
        {
            token = new Token { StudentId = studentId, Id = null!, IsAdmin = false };
        }
        else
        {
            dbContext.Tokens.Remove(token);
            await dbContext.SaveChangesAsync();
        }

        var random = new Random();
        var base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < 10; i++)
            sb.Append(base62[random.Next(62)]);
        token.Id = sb.ToString();
        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();

        try
        {
            await mailingService.SendToken(studentId);
        }
        catch (HttpResponseException ex)
        {
            return new ObjectResult(ex.Value)
            {
                StatusCode = ex.Status,
            };
        }

        return Ok();
    }

    /// <summary>提交作业</summary>
    /// <param name="assignmentId">作业ID</param>
    /// <param name="studentId">提交人</param>
    /// <param name="submittedAt">提交时间</param>
    /// <param name="file">附件</param>
    [HttpPost]
    [Route("Submit")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> Submit(int studentId, [FromForm] int assignmentId, [FromForm] DateTimeOffset? submittedAt,
                                            IFormFile file)
    {
        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Unauthorized();

        if (!User.IsInRole("Admin") && file.Length > AppConfig.AttachmentSizeLimit) return BadRequest("File too big");

        if (!await dbContext.Assignments.AnyAsync(a => a.Id == assignmentId)) return NotFound("assignmentId");
        if (!await dbContext.Students.AnyAsync(s => s.Id == studentId)) return NotFound("studentId");
        if (submittedAt != null && !User.IsInRole("Admin")) return Unauthorized("submittedAt");

        var submission = new Submission
        {
            StudentId = studentId,
            AssignmentId = assignmentId,
            SubmittedAt = submittedAt ?? DateTimeOffset.Now,
        };
        if (await submissionService.AddSubmissionAsync(submission))
            await submissionService.AddAttachmentAsync(submission, file.FileName, file.OpenReadStream());

        return Ok();
    }

    /// <summary>获取某个学生的作业情况汇总</summary>
    [HttpGet]
    [Authorize]
    [Route("SubmissionSummary")]
    [ProducesResponseType(typeof(List<SubmissionSummaryDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissionSummary(int? studentId)
    {
        if (studentId == null) return BadRequest("Expect student ID");

        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Unauthorized();

        var submissions = await dbContext.Submissions
            .Where(submission => submission.StudentId == studentId)
            .OrderBy(submission => submission.AssignmentId)
            .Select(submission => new SubmissionSummaryDTO
            {
                AssignmentId = submission.AssignmentId,
                Grade = submission.Grade,
                SubmittedAt = submission.SubmittedAt,
                NeedCorrection = submission.NeedCorrection
                    .OrderBy(mistake => mistake.ProblemId)
                    .Select(m => new ProblemDTO { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                    .ToList(),
                HasCorrected = submission.HasCorrected
                    .OrderBy(mistake => mistake.AssignmentId)
                    .ThenBy(mistake => mistake.ProblemId)
                    .Select(m => new ProblemDTO { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                    .ToList(),
                Comment = submission.Comment,
            })
                              .ToListAsync();
        foreach (var s in submissions)
        {
            s.AssignmentName = await myAppService.GetAssignmentNameById(s.AssignmentId);
            s.NeedCorrection.ForEach(x => myAppService.FillProblemDTO(x));
            s.HasCorrected.ForEach(x => myAppService.FillProblemDTO(x));
        }
        return Ok(submissions);
    }
}

public class SubmissionSummaryDTO
{
    public int AssignmentId { get; set; }
    public string AssignmentName { get; set; } = "";
    public Grade Grade { get; set; } = Grade.None;
    public DateTimeOffset SubmittedAt { get; set; }
    public List<ProblemDTO> NeedCorrection { get; set; } = null!;
    public List<ProblemDTO> HasCorrected { get; set; } = null!;
    public string Comment { get; set; } = "";
}
