using ExamSimulator.Web.Domain.Questions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ExamSimulator.Web.Infrastructure;

public class ExamSimulatorDbContext(DbContextOptions<ExamSimulatorDbContext> options) : DbContext(options)
{
    public DbSet<Question> Questions => Set<Question>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.ToTable("Questions");
            entity.Property(q => q.ExamProfileId).IsRequired();
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
        });
    }
}
