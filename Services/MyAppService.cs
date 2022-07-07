namespace NjuCsCmsHelper.Services;

using System.IO.Compression;
using Microsoft.Extensions.Caching.Memory;
using NjuCsCmsHelper.Server.Controllers;
using NjuCsCmsHelper.Models;

public interface IMyAppService
{
    Task<string?> GetAssignmentNameById(int assignmentId);
    Task<ProblemDTO> GetProblemDTO(Mistake mistake);
    Task<ProblemDTO> GetProblemDTO(int assignmentId, int problemId);
    Task<bool> AddSubmissionAsync(Submission submission);
    Task AddAttachmentAsync(Submission submission, string filename, Stream attachmentStream);
    Task GetArchiveAsync(int assignmentId, int reviewerId, string assignmentName, IEnumerable<AttachmentInfo> attachmentList, Stream outStream);
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
        string? assignmentName;
        if (!cache.TryGetValue($"AssignmentId-{assignmentId}", out assignmentName))
        {
            assignmentName = await dbContext.Assignments.Where(x => x.Id == assignmentId).Select(x => x.Name).SingleOrDefaultAsync();
            cache.Set($"AssignmentId-{assignmentId}", assignmentName);
        }
        return assignmentName;
    }

    public Task<ProblemDTO> GetProblemDTO(Mistake mistake) => GetProblemDTO(mistake.AssignmentId, mistake.ProblemId);
    public async Task<ProblemDTO> GetProblemDTO(int assignmentId, int problemId) => new ProblemDTO
    {
        AssignmentId = assignmentId,
        ProblemId = problemId,
        Display = $"{await GetAssignmentNameById(assignmentId)}.{problemId}",
    };

    public async Task<bool> AddSubmissionAsync(Submission submission)
    {
        var oldSubmission = await dbContext.Submissions.Include(x => x.Attachments)
                                .SingleOrDefaultAsync(x => x.StudentId == submission.StudentId &&
                                                           x.AssignmentId == submission.AssignmentId);
        if (oldSubmission != null)
        {
            if (oldSubmission.SubmittedAt >= submission.SubmittedAt) return false;

            dbContext.Attachments.RemoveRange(oldSubmission.Attachments);
            dbContext.Submissions.Remove(oldSubmission);
        }
        await dbContext.Submissions.AddAsync(submission);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AddAttachmentAsync(Submission submission, string filename, Stream attachmentStream)
    {
        var attachment = new Attachment
        {
            Submission = submission,
            Filename = filename,
        };
        await dbContext.Attachments.AddAsync(attachment);
        await dbContext.SaveChangesAsync();
        if (!Directory.Exists("attachments"))
        {
            Directory.CreateDirectory("attachments");
        }
        using var file = File.Create($"attachments/{attachment.Id}");
        await attachmentStream.CopyToAsync(file);
    }

    public async Task GetArchiveAsync(int assignmentId, int reviewerId, string assignmentName, IEnumerable<AttachmentInfo> attachmentList, Stream outStream)
    {
        using var zipStream = new ZipArchive(outStream, ZipArchiveMode.Create, true);

        {
            var entry = zipStream.CreateEntry($"{assignmentName}/send.py");
            var script = await File.ReadAllTextAsync($"Assets/SendMail/send.py");
            script = script.Replace("#$reviewerId", reviewerId.ToString());
            script = script.Replace("#$assignmentId", assignmentId.ToString());
            script = script.Replace("#$assignmentName", $"\"{assignmentName}\"");
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(script));
        }
        {
            var entry = zipStream.CreateEntry($"{assignmentName}/sendconfig.json");
            using var file = File.OpenRead($"Assets/SendMail/sendconfig.json");
            using var entryStream = entry.Open();
            await file.CopyToAsync(entryStream);
        }

        foreach (var attachmentInfo in attachmentList)
        {
            var entry = zipStream.CreateEntry($"{assignmentName}/attachmentInfo.AttachmentFilename");
            using var file = File.OpenRead($"attachments/{attachmentInfo.AttachmentId}");
            using var entryStream = entry.Open();
            await file.CopyToAsync(entryStream);
        }
    }
}
