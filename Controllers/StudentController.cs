namespace NjuCsCmsHelper.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly ILogger<StudentController> logger;
    private readonly Models.ApplicationDbContext dbContext;

    public StudentController(ILogger<StudentController> logger, Models.ApplicationDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    [HttpGet]
    [Route("{id:int}/Reviewer")]
    [AllowAnonymous]
    public async Task<IActionResult> Reviewer(int id)
    {
        var student = await dbContext.Students.Where(student => student.Id == id).SingleOrDefaultAsync();
        if (student == null) return NotFound("Not Found");
        return Ok(student.ReviewerId);
    }
}