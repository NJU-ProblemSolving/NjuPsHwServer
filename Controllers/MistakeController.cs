namespace NjuCsCmsHelper.Server.Controllers;

using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MistakeController : ControllerBase
{
    private readonly ILogger<MistakeController> logger;
    private readonly AppDbContext dbContext;
    private IMemoryCache cache;

    public MistakeController(ILogger<MistakeController> logger, AppDbContext dbContext, IMemoryCache cache)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.cache = cache;
    }

    /// <summary>获取所有人未订正的错题信息</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<MistakeInfo>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Get()
    {
        var list = await dbContext.Mistakes.Where(m => m.CorrectedIn == null)
                       .Select(m => new { m.StudentId, Mistake = $"{m.AssignmentId}-{m.ProblemId}" })
                       .ToListAsync();
        var res = list.GroupBy(m => m.StudentId)
                      .Select(g => new MistakeInfo { StudentId = g.Key, Mistakes = g.Select(m => m.Mistake).ToList() })
                      .ToList();
        return Ok(res);
    }
}

public class MistakeInfo
{
    public int StudentId { get; set; }
    public List<string> Mistakes { get; set; } = null !;
}
