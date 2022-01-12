namespace NjuCsCmsHelper.Server.Controllers;

using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AssignmentController : ControllerBase
{
    private readonly ILogger<AssignmentController> logger;
    private readonly Models.ApplicationDbContext dbContext;

    public AssignmentController(ILogger<AssignmentController> logger, Models.ApplicationDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get() { return Ok(await dbContext.Assignments.ToListAsync()); }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        if (await dbContext.Assignments.SingleOrDefaultAsync(assignment => assignment.Id == id)
                is Assignment assignment)
        {
            return Ok(assignment);
        }
        return NotFound();
    }

    [HttpPost]
    [Route("{id:int}")]
    public async Task<IActionResult> Create(int id, [FromBody] Assignment assignment)
    {
        if (id == 0) return BadRequest("Assignment id cannot be zero");
        if (assignment.Id != 0 && id != assignment.Id)
            return BadRequest("The `id` field in `Assignment` does not match with the id given in the path");
        if (await dbContext.Assignments.AnyAsync(a => a.Id == id)) return Conflict("Assignment ID already exists");
        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = assignment.Id }, assignment);
    }
}
