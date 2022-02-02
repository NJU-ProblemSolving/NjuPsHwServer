namespace NjuCsCmsHelper.Services;

using System.IO.Compression;
using NjuCsCmsHelper.Models;

public interface IAttachmentService
{
    Task AddSubmissionAsync(Submission submission);
    Task AddAttachmentAsync(Submission submission, string filename, Stream attachmentStream);
    Task GetArchiveAsync(IEnumerable<AttachmentInfo> fileList, Stream outStream);
}

public class AttachmentInfo
{
    public int AttachmentId;
    public string AttachmentFilename = null !;
}

public class MyAttachmentService : IAttachmentService
{
    private readonly AppDbContext dbContext;

    public MyAttachmentService(AppDbContext dbContext) { this.dbContext = dbContext; }

    public async Task AddSubmissionAsync(Submission submission)
    {
        var oldSubmission = await dbContext.Submissions.SingleOrDefaultAsync(
            x => x.StudentId == submission.StudentId && x.AssignmentId == submission.AssignmentId);
        if (oldSubmission != null)
        {
            if (oldSubmission.SubmittedAt >= submission.SubmittedAt) return;

            dbContext.Attachments.RemoveRange(oldSubmission.Attachments);
            dbContext.Submissions.Remove(submission);
        }
        await dbContext.Submissions.AddAsync(submission);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddAttachmentAsync(Submission submission, string filename, Stream attachmentStream)
    {
        var attachment = new Attachment {
            Submission = submission,
            Filename = filename,
        };
        await dbContext.Attachments.AddAsync(attachment);
        using var file = File.Create($"attachments/{attachment.Id}");
        await attachmentStream.CopyToAsync(file);
        await dbContext.SaveChangesAsync();
    }

    public async Task GetArchiveAsync(IEnumerable<AttachmentInfo> attachmentList, Stream outStream)
    {
        using var zipStream = new ZipArchive(outStream, ZipArchiveMode.Create, true);

        foreach (var attachmentInfo in attachmentList)
        {
            var entry = zipStream.CreateEntry(attachmentInfo.AttachmentFilename);
            using var file = File.OpenRead($"attachments/{attachmentInfo.AttachmentId}");
            using var entryStream = entry.Open();
            await file.CopyToAsync(entryStream);
        }
    }
}
