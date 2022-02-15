namespace NjuCsCmsHelper.Models;

[Index(nameof(Name), IsUnique = true)]
public class Assignment
{
    public int Id {get;set;}
    public string Name { get; set; } = null!;
    public int NumberOfProblems { get; set; }
    public DateTimeOffset Deadline { get; set; }
}
