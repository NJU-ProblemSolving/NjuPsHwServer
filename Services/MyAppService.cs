namespace NjuCsCmsHelper.Server.Services;

using Microsoft.Extensions.Caching.Memory;
using NjuCsCmsHelper.Models;
using NjuCsCmsHelper.Server.Controllers;

public interface IMyAppService
{
    Task<string> GetAssignmentNameById(int assignmentId);
    Task<ProblemDTO> GetProblemDTO(int assignmentId, int problemId);
    Task FillProblemDTO(ProblemDTO problem);
}

public class AttachmentInfo
{
    public int AttachmentId { get; set; }
    public string AttachmentFilename { get; set; } = null!;
}

public class MyAppService : IMyAppService
{
    private readonly AppDbContext dbContext;
    private readonly IMemoryCache cache;

    public MyAppService(AppDbContext dbContext, IMemoryCache cache)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<string> GetAssignmentNameById(int assignmentId)
    {
        if (!cache.TryGetValue($"AssignmentId-{assignmentId}", out string? assignmentName))
        {
            assignmentName = await dbContext.Assignments.Where(x => x.Id == assignmentId).Select(x => x.Name).SingleOrDefaultAsync();
            cache.Set($"AssignmentId-{assignmentId}", assignmentName);
        }
        if (assignmentName == null) throw new KeyNotFoundException($"Assignment ID {assignmentId} not found");
        return assignmentName;
    }

    public async Task FillProblemDTO(ProblemDTO problem)
    {
        problem.Display = $"{await GetAssignmentNameById(problem.AssignmentId)}.{problem.ProblemId}";
    }
    public async Task<ProblemDTO> GetProblemDTO(int assignmentId, int problemId) => new ProblemDTO
    {
        AssignmentId = assignmentId,
        ProblemId = problemId,
        Display = $"{await GetAssignmentNameById(assignmentId)}.{problemId}",
    };
}
