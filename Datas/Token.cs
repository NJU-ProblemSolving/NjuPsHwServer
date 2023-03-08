namespace NjuCsCmsHelper.Datas;

public class Token
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; set; } = null!;
    public int StudentId { get; set; }
    public bool IsAdmin { get; set; }

    public virtual Student Student { get; set; } = null!;
}
