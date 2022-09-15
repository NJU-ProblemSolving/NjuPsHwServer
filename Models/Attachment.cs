namespace NjuCsCmsHelper.Models;

[Index(nameof(SubmissionId))]
public class Attachment
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string Filename { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
