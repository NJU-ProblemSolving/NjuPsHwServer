namespace NjuCsCmsHelper.Server.Services;

using NjuCsCmsHelper.Models;
using System.IO.Compression;

public class SubmissionService
{
    private readonly AppDbContext dbContext;

    private readonly string attachmentDir = "data/attachments";

    public SubmissionService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

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

    public async Task<Attachment> AddAttachmentAsync(Submission submission, string filename, Stream attachmentStream)
    {
        var attachment = new Attachment
        {
            Submission = submission,
            Filename = filename,
        };
        await dbContext.Attachments.AddAsync(attachment);
        await dbContext.SaveChangesAsync();

        try
        {
            if (!Directory.Exists(attachmentDir))
            {
                Directory.CreateDirectory(attachmentDir);
            }
            using var file = File.Create($"{attachmentDir}/{attachment.Id}");
            await attachmentStream.CopyToAsync(file);
        }
        catch (Exception)
        {
            dbContext.Attachments.Remove(attachment);
            await dbContext.SaveChangesAsync();
            throw;
        }
        return attachment;
    }

    public async Task RemoveAttachmentsOfSubmission(int SubmissionId)
    {
        var attachments = await dbContext.Attachments.Where(x => x.SubmissionId == SubmissionId).ToListAsync();
        foreach (var attachment in attachments)
            File.Delete($"{attachmentDir}/{attachment.Id}");
        dbContext.Attachments.RemoveRange(attachments);
        await dbContext.SaveChangesAsync();
    }

    public async Task GetArchiveAsync(int assignmentId, int reviewerId, string assignmentName, IEnumerable<AttachmentInfo> attachmentList, Stream outStream)
    {
        using var zipStream = new ZipArchive(outStream, ZipArchiveMode.Create);

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
            var entry = zipStream.CreateEntry($"{assignmentName}/{attachmentInfo.AttachmentFilename}");
            using var file = File.OpenRead($"{attachmentDir}/{attachmentInfo.AttachmentId}");
            using var entryStream = entry.Open();
            await file.CopyToAsync(entryStream);
        }
    }
}