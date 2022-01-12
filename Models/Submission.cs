namespace NjuCsCmsHelper.Models;

public class Submission
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Grade Grade { get; set; }
    public string Comment { get; set; } = null !;
    public string Track { get; set; } = null !;

    public virtual Student Student { get; set; } = null !;
    public virtual Assignment Assignment { get; set; } = null !;

    public virtual ICollection<Mistake> NeedCorrection { get; set; } = null !;
    public virtual ICollection<Mistake> HasCorrected { get; set; } = null !;
}

public enum Grade
{
    None,
    A,
    Aminus,
    B,
    Bminus,
    C,
    D,
}

public static class GradeExtensions
{
    public static string ToDescriptionString(this Grade grade)
    {
        return grade switch {
            Grade.None => "未批改", Grade.A => "A", Grade.Aminus => "A-", Grade.B => "B",
            Grade.Bminus => "B-",   Grade.C => "C", Grade.D => "D",       _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
