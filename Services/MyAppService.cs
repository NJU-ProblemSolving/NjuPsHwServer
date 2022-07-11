namespace NjuCsCmsHelper.Server.Services;

using Microsoft.Extensions.Caching.Memory;
using NjuCsCmsHelper.Server.Controllers;
using NjuCsCmsHelper.Models;

public interface IMyAppService
{
    Task<string?> GetAssignmentNameById(int assignmentId);
    // Task<ProblemDTO> GetProblemDTO(Mistake mistake);
    Task<ProblemDTO> GetProblemDTO(int assignmentId, int problemId);
    Task FillProblemDTO(ProblemDTO problem);
}

public class AttachmentInfo
{
    public int AttachmentId;
    public string AttachmentFilename = null!;
}

public class MyAppService : IMyAppService
{
    private readonly AppDbContext dbContext;
    private readonly IConfiguration configuration;
    private readonly IMemoryCache cache;

    public MyAppService(AppDbContext dbContext, IConfiguration configuration, IMemoryCache cache)
    {
        this.dbContext = dbContext;
        this.configuration = configuration;
        this.cache = cache;
    }

    public async Task<string?> GetAssignmentNameById(int assignmentId)
    {
        if (!cache.TryGetValue($"AssignmentId-{assignmentId}", out string? assignmentName))
        {
            assignmentName = await dbContext.Assignments.Where(x => x.Id == assignmentId).Select(x => x.Name).SingleOrDefaultAsync();
            cache.Set($"AssignmentId-{assignmentId}", assignmentName);
        }
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
