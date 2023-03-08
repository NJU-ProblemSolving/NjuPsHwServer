namespace NjuCsCmsHelper.Models;

public class StudentDto
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public int ReviewerId { get; set; }
}
