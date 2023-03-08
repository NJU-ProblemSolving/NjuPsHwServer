namespace NjuCsCmsHelper.Datas;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mistake>()
            .HasOne(mistake => mistake.MadeIn)
            .WithMany(submission => submission.NeedCorrection);
        modelBuilder.Entity<Mistake>()
            .HasOne(mistake => mistake.CorrectedIn)
            .WithMany(submission => submission.HasCorrected);

        modelBuilder.Entity<Assignment>()
            .Property(assignment => assignment.Deadline)
            .HasConversion(v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        modelBuilder.Entity<Submission>()
            .Property(submission => submission.SubmittedAt)
            .HasConversion(v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v));
    }

    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<Mistake> Mistakes { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;
    public DbSet<Token> Tokens { get; set; } = null!;
}
