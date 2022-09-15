namespace NjuCsCmsHelper.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int ReviewerId { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = null!;
}

public class StudentDto
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public int ReviewerId { get; set; }
}
