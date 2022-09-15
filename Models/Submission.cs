namespace NjuCsCmsHelper.Models;

[Index(nameof(StudentId), nameof(AssignmentId), IsUnique = true)]
public class Submission
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Grade Grade { get; set; } = Grade.None;
    public string Comment { get; set; } = "";
    public string Track { get; set; } = "";

    public virtual Student Student { get; set; } = null!;
    public virtual Assignment Assignment { get; set; } = null!;

    public virtual ICollection<Mistake> NeedCorrection { get; set; } = null!;
    public virtual ICollection<Mistake> HasCorrected { get; set; } = null!;

    public virtual ICollection<Attachment> Attachments { get; set; } = null!;
}

public class SubmissionDto
{
    [Required]
    public int AssignmentId { get; set; }
    [Required]
    public string AssignmentName { get; set; } = "";
    [Required]
    public Grade Grade { get; set; } = Grade.None;
    [Required]
    public DateTimeOffset SubmittedAt { get; set; }
    [Required]
    public List<MistakeDto> NeedCorrection { get; set; } = null!;
    [Required]
    public List<MistakeDto> HasCorrected { get; set; } = null!;
    [Required]
    public string Comment { get; set; } = "";
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
    public static string GetDescription(this Grade grade) => grade switch
    {
        Grade.None => "未批改",
        Grade.A => "A",
        Grade.Aminus => "A-",
        Grade.B => "B",
        Grade.Bminus => "B-",
        Grade.C => "C",
        Grade.D => "D",
        _ => throw new ArgumentOutOfRangeException(nameof(grade)),
    };

    public static float ToScore(this Grade grade) => grade switch
    {
        Grade.A => 100,
        Grade.Aminus => 90,
        Grade.B => 80,
        Grade.Bminus => 75,
        Grade.C => 70,
        Grade.D => 60,
        Grade.None => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(grade)),
    };
}
