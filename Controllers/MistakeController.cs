namespace NjuCsCmsHelper.Server.Controllers;

using NjuCsCmsHelper.Models;

[Route("api/[controller]")]
[ApiController]
public class MistakeController : AppControllerBase<MistakeController>
{
    public MistakeController(IServiceProvider provider) : base(provider) { }

    /// <summary>获取所有人未订正的错题信息</summary>
    [HttpGet]
    [Authorize("Reviewer")]
    [ProducesResponseType(typeof(List<MistakesOfStudent>), StatusCodes.Status201Created)]
    public async Task<IActionResult> GetMistakes()
    {
        var list = await dbContext.Mistakes.Where(m => m.CorrectedInId == null)
                       .Select(m => new { m.StudentId, Mistake = new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId } })
                       .ToListAsync();
        foreach (var item in list)
            await myAppService.FillProblemDTO(item.Mistake);
        var res = list.GroupBy(m => m.StudentId)
                      .Select(g => new MistakesOfStudent { StudentId = g.Key, Mistakes = g.Select(m => m.Mistake).ToList() })
                      .ToList();
        return Ok(res);
    }
}
