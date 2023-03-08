namespace NjuCsCmsHelper.Server.Controllers;

using Datas;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AssignmentController : AppControllerBase<AssignmentController>
{
    public AssignmentController(IServiceProvider provider) : base(provider) { }

    /// <summary>获取所有作业的信息</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Assignment>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAssignments() { return Ok(await dbContext.Assignments.ToListAsync()); }

    /// <summary>获取指定作业的信息</summary>
    [HttpGet]
    [Route("{assignmentId:int}")]
    [ProducesResponseType(typeof(Assignment), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAssignment(int assignmentId)
    {
        if (await dbContext.Assignments.SingleOrDefaultAsync(assignment => assignment.Id == assignmentId)
                is Assignment assignment)
            return Ok(assignment);

        return NotFound();
    }

    /// <summary>新建作业</summary>
    [HttpPost]
    [Authorize("Admin")]
    [ProducesResponseType(typeof(Assignment), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAssignment([FromBody] Assignment assignment)
    {
        if (await dbContext.Assignments.AnyAsync(a => a.Name == assignment.Name))
            return Conflict("Assignment ID already exists");

        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssignment), new { assignmentId = assignment.Id }, assignment);
    }

    /// <summary>更新作业信息</summary>
    [HttpPut]
    [Authorize("Admin")]
    [Route("{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAssignment(int assignmentId, [FromBody] Assignment assignment)
    {
        if (assignment.Id != 0 && assignment.Id != assignmentId) return BadRequest();
        var assignmentInDb = await dbContext.Assignments.SingleAsync(a => a.Id == assignmentId);
        if (assignmentInDb == null) return NotFound("Assignment ID not found");

        assignmentInDb.Name = assignment.Name;
        assignmentInDb.NumberOfProblems = assignment.NumberOfProblems;
        assignmentInDb.Deadline = assignment.Deadline;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>删除作业信息</summary>
    [HttpDelete]
    [Authorize("Admin")]
    [Route("{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAssignment(int assignmentId)
    {
        var assignment = await dbContext.Assignments.SingleAsync(a => a.Id == assignmentId);
        if (assignment == null) return NotFound("Assignment ID not found");

        dbContext.Assignments.Remove(assignment);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
