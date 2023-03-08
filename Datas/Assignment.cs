namespace NjuCsCmsHelper.Datas;

[Index(nameof(Name), IsUnique = true)]
public class Assignment
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public int NumberOfProblems { get; set; }
    [Required]
    public DateTimeOffset Deadline { get; set; }
}
