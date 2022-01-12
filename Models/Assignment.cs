namespace NjuCsCmsHelper.Models;

public class Assignment
{
    public int Id { get; set; }
    public int NumberOfProblems { get; set; }
    public DateTimeOffset DeadLine { get; set; }
}
