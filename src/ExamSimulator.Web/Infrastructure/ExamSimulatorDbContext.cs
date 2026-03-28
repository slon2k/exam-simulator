using ExamSimulator.Web.Domain.Attempts;
using ExamSimulator.Web.Domain.ExamProfiles;
using ExamSimulator.Web.Domain.Identity;
using ExamSimulator.Web.Domain.Questions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ExamSimulator.Web.Infrastructure;

public class ExamSimulatorDbContext(DbContextOptions<ExamSimulatorDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ExamProfile> ExamProfiles => Set<ExamProfile>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamAttemptAnswer> ExamAttemptAnswers => Set<ExamAttemptAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExamProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.ToTable("ExamProfiles");
            entity.Property(p => p.Id).IsRequired();
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Description).IsRequired(false);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.ToTable("Questions");
            entity.Property(q => q.ExamProfileId).IsRequired();
            entity.HasOne<ExamProfile>()
                .WithMany()
                .HasForeignKey(q => q.ExamProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(q => q.Prompt).IsRequired();
            entity.Property(q => q.TopicTag).IsRequired();
            entity.Property(q => q.Type).IsRequired();
            entity.Property(q => q.Difficulty).IsRequired();
            entity.Property(q => q.Explanation).IsRequired(false);
            entity.Property(q => q.Options)
                .IsRequired()
                .HasConversion(
                    options => System.Text.Json.JsonSerializer.Serialize(options, (System.Text.Json.JsonSerializerOptions?)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    options => options.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    options => options.ToList()));
            entity.Property(q => q.CorrectOptionIndices)
                .IsRequired()
                .HasConversion(
                    indices => System.Text.Json.JsonSerializer.Serialize(indices, (System.Text.Json.JsonSerializerOptions?)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<List<int>>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<int>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    indices => indices.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    indices => indices.ToList()));
            entity.Property(q => q.MatchingTargets)
                .IsRequired(false)
                .HasConversion(
                    targets => targets == null ? null : System.Text.Json.JsonSerializer.Serialize(targets, (System.Text.Json.JsonSerializerOptions?)null),
                    json => json == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>?>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    t => t == null ? 0 : t.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    t => t == null ? null : (IReadOnlyList<string>)t.ToList()));
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.ToTable("ExamAttempts");
            entity.Property(a => a.UserId).IsRequired();
            entity.Property(a => a.ProfileId).IsRequired();
            entity.Property(a => a.TakenAt).IsRequired();
            entity.Property(a => a.Score).IsRequired();
            entity.Property(a => a.Total).IsRequired();
            entity.Property(a => a.RandomOrder).IsRequired();
            entity.Property(a => a.Tags)
                .IsRequired()
                .HasConversion(
                    tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    tags => tags.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    tags => tags.ToList()));
            entity.Property(a => a.Difficulties)
                .IsRequired()
                .HasConversion(
                    difficulties => System.Text.Json.JsonSerializer.Serialize(difficulties, (System.Text.Json.JsonSerializerOptions?)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    difficulties => difficulties.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    difficulties => difficulties.ToList()));
            entity.HasMany(a => a.Answers)
                .WithOne()
                .HasForeignKey(ans => ans.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamAttemptAnswer>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.ToTable("ExamAttemptAnswers");
            entity.Property(a => a.AttemptId).IsRequired();
            entity.Property(a => a.QuestionId).IsRequired();
            entity.Property(a => a.IsCorrect).IsRequired();
            entity.Property(a => a.SelectedOptionIndices)
                .IsRequired(false)
                .HasConversion(
                    indices => indices == null ? null : System.Text.Json.JsonSerializer.Serialize(indices, (System.Text.Json.JsonSerializerOptions?)null),
                    json => json == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<int>>(json, (System.Text.Json.JsonSerializerOptions?)null)!)
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<int>?>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    indices => indices == null ? 0 : indices.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    indices => indices == null ? null : (IReadOnlyList<int>)indices.ToList()));
            // QuestionId is a plain column — no FK constraint so answers survive question deletion
        });
    }
}
