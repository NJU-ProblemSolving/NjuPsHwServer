namespace NjuCsCmsHelper.Server.Controllers;

using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AssignmentController : ControllerBase
{
    private readonly ILogger<AssignmentController> logger;
    private readonly AppDbContext dbContext;

    public AssignmentController(ILogger<AssignmentController> logger, AppDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    /// <summary>获取所有作业的信息</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<Assignment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get() { return Ok(await dbContext.Assignments.ToListAsync()); }

    /// <summary>获取指定作业的信息</summary>
    [HttpGet]
    [Route("{assignmentId:int}")]
    [ProducesResponseType(typeof(Assignment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int assignmentId)
    {
        if (await dbContext.Assignments.SingleOrDefaultAsync(assignment => assignment.Id == assignmentId)
                is Assignment assignment)
            return Ok(assignment);

        return NotFound();
    }

    /// <summary>新建作业</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Assignment), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] Assignment assignment)
    {
        if (await dbContext.Assignments.AnyAsync(a => a.Id == assignment.Id))
            return Conflict("Assignment ID already exists");

        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = assignment.Id }, assignment);
    }
}
