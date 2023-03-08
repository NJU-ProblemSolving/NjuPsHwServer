namespace NjuCsCmsHelper.Models;

public class MistakeDto
{
    [Required]
    public int AssignmentId { get; set; }
    [Required]
    public int ProblemId { get; set; }
    [Required]
    public string Display { get; set; } = null!;

    public override string ToString() => Display;
}

public class MistakesOfStudent
{
    [Required]
    public int StudentId { get; set; }
    [Required]
    public List<MistakeDto> Mistakes { get; set; } = null!;
}
