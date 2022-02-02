namespace NjuCsCmsHelper.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mistake>()
            .HasOne(mistake => mistake.MakedIn)
            .WithMany(submission => submission.NeedCorrection);
        modelBuilder.Entity<Mistake>()
            .HasOne(mistake => mistake.CorrectedIn)
            .WithMany(submission => submission.HasCorrected);

        modelBuilder.Entity<Assignment>()
            .Property(assignment => assignment.Deadline)
            .HasConversion(v => TimeStampHelper.ToUnixTimeMillis(v), v => TimeStampHelper.FromUnixTimeMillsecs(v));
        modelBuilder.Entity<Submission>()
            .Property(submission => submission.SubmittedAt)
            .HasConversion(v => TimeStampHelper.ToUnixTimeMillis(v), v => TimeStampHelper.FromUnixTimeMillsecs(v));
    }

    public DbSet<Student> Students { get; set; } = null !;
    public DbSet<Assignment> Assignments { get; set; } = null !;
    public DbSet<Submission> Submissions { get; set; } = null !;
    public DbSet<Mistake> Mistakes { get; set; } = null !;
    public DbSet<Attachment> Attachments { get; set; } = null !;
    public DbSet<Token> Tokens { get; set; } = null !;
}

public static class TimeStampHelper
{
    public static long ToUnixTimeMillis(DateTimeOffset date)
    {
        if (date < DateTimeOffset.UnixEpoch)
            throw new ArgumentOutOfRangeException($"Date {date} cannot convert to Unix Timestamp");
        return (long)(date - DateTimeOffset.UnixEpoch).TotalMilliseconds;
    }
    public static DateTimeOffset FromUnixTimeMillsecs(long millis)
    {
        return DateTimeOffset.UnixEpoch.AddMilliseconds(millis);
    }
}
