namespace NjuCsCmsHelper.Models;

using NjuCsCmsHelper.Datas;

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
