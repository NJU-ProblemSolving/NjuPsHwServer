namespace NjuCsCmsHelper.Server.Controllers;

using Services;
using Models;

[Route("api/[controller]/{studentId:int?}")]
[ApiController]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly ILogger<StudentController> logger;
    private readonly AppDbContext dbContext;
    private readonly IAuthorizationService authorizationService;

    public StudentController(ILogger<StudentController> logger, AppDbContext dbContext,
                             IAuthorizationService authorizationService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.authorizationService = authorizationService;
    }

    /// <summary>获取某个学生的评阅人ID</summary>
    [HttpGet]
    [Route("ReviewerId")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewerId(int? studentId)
    {
        if (studentId == null) return BadRequest("Expect student ID");

        var student = await dbContext.Students.Where(student => student.Id == studentId !).SingleOrDefaultAsync();
        if (student == null) return NotFound("Student ID not found");

        return Ok(student.ReviewerId);
    }

    /// <summary>获取某个学生的作业情况汇总</summary>
    [HttpGet]
    [Route("SubmissionSummary")]
    public async Task<IActionResult> GetSubmissionSummary(int? studentId)
    {
        if (studentId == null) return BadRequest("Expect student ID");

        var authorizeResult =
            await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Unauthorized();

        var submissions = await dbContext.Submissions.Where(submission => submission.StudentId == studentId)
                              .Select(submission => new {
                                  AssignmentId = submission.AssignmentId,
                                  Grade = submission.Grade,
                                  NeedCorrection = submission.NeedCorrection.OrderBy(mistake => mistake.ProblemId)
                                                       .Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                                                       .ToList(),
                                  HasCorrected = submission.HasCorrected.OrderBy(mistake => mistake.AssignmentId)
                                                     .ThenBy(mistake => mistake.ProblemId)
                                                     .Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                                                     .ToList(),
                                  Comment = submission.Comment,
                              })
                              .ToListAsync();
        return Ok(submissions);
    }
}