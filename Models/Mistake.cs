namespace NjuCsCmsHelper.Models;

public class Mistake
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public int ProblemId { get; set; }
    public int MakedInId { get; set; }
    public int? CorrectedInId { get; set; }

    public virtual Student Student { get; set; } = null !;
    public virtual Assignment Assignment { get; set; } = null !;
    public virtual Submission MakedIn { get; set; } = null !;
    public virtual Submission? CorrectedIn { get; set; } = null !;
}
