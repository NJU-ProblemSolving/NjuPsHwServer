namespace NjuCsCmsHelper.Models;

[Index(nameof(StudentId), nameof(AssignmentId), nameof(ProblemId), IsUnique = true)]
public class Mistake
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public int ProblemId { get; set; }
    public int MadeInId { get; set; }
    public int? CorrectedInId { get; set; }

    public virtual Student Student { get; set; } = null !;
    public virtual Assignment Assignment { get; set; } = null !;
    public virtual Submission MadeIn { get; set; } = null !;
    public virtual Submission? CorrectedIn { get; set; } = null !;
}
