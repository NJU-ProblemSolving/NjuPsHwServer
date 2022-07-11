namespace NjuCsCmsHelper.Server.Controllers;

using Models;
using Services;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MistakeController : ControllerBase
{
    private readonly ILogger<MistakeController> logger;
    private readonly AppDbContext dbContext;
    private readonly IMyAppService myAppService;

    public MistakeController(ILogger<MistakeController> logger, AppDbContext dbContext, IMyAppService myAppService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.myAppService = myAppService;
    }

    /// <summary>获取所有人未订正的错题信息</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<MistakesOfStudent>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Get()
    {
        var list = await dbContext.Mistakes.Where(m => m.CorrectedInId == null)
                       .Select(m => new { m.StudentId, Mistake = new ProblemDTO { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId } })
                       .ToListAsync();
        foreach (var item in list)
            await myAppService.FillProblemDTO(item.Mistake);
        var res = list.GroupBy(m => m.StudentId)
                      .Select(g => new MistakesOfStudent { StudentId = g.Key, Mistakes = g.Select(m => m.Mistake).ToList() })
                      .ToList();
        return Ok(res);
    }
}

public class ProblemDTO
{
    public int AssignmentId { get; set; }
    public int ProblemId { get; set; }
    public string Display { get; set; } = null!;
}

public class MistakesOfStudent
{
    public int StudentId { get; set; }
    public List<ProblemDTO> Mistakes { get; set; } = null!;
}
