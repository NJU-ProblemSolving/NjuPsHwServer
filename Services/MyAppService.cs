namespace NjuCsCmsHelper.Server.Services;

using Microsoft.Extensions.Caching.Memory;
using NjuCsCmsHelper.Datas;
using NjuCsCmsHelper.Models;

public class AttachmentInfo
{
    public int AttachmentId { get; set; }
    public string AttachmentFilename { get; set; } = null!;
}

public class MyAppService
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

    public async Task FillProblemDTO(MistakeDto problem)
    {
        problem.Display = $"{await GetAssignmentNameById(problem.AssignmentId)}.{problem.ProblemId}";
    }
    public async Task<MistakeDto> GetProblemDTO(int assignmentId, int problemId) => new MistakeDto
    {
        AssignmentId = assignmentId,
        ProblemId = problemId,
        Display = $"{await GetAssignmentNameById(assignmentId)}.{problemId}",
    };
}
