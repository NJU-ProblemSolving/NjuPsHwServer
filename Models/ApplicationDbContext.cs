using Microsoft.EntityFrameworkCore;

namespace NjuCsCmsHelper.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Mistake>()
                .HasOne(mistake => mistake.MakedIn)
                .WithMany(submission => submission.NeedCorrection);
            modelBuilder.Entity<Mistake>()
                .HasOne(mistake => mistake.CorrectedIn)
                .WithMany(submission => submission.HasCorrected);
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Mistake> Mistakes { get; set; }
    }
}
