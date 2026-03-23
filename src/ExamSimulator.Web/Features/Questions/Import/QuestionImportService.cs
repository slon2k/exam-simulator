using System.Text.Json;
using ExamSimulator.Web.Domain.Questions;
using ExamSimulator.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ExamSimulator.Web.Features.Questions.Import;

public enum ImportItemStatus { Valid, Invalid, Duplicate }

public sealed record ImportItemPreview(
    int Index,
    QuestionImportItemDto Item,
    ImportItemStatus Status,
    IReadOnlyList<string> Errors
);

public sealed record ImportPreview(
    string ExamProfileId,
    IReadOnlyList<ImportItemPreview> Items
)
{
    public int ValidCount => Items.Count(i => i.Status == ImportItemStatus.Valid);
    public int InvalidCount => Items.Count(i => i.Status == ImportItemStatus.Invalid);
    public int DuplicateCount => Items.Count(i => i.Status == ImportItemStatus.Duplicate);
    public bool CanImport => ValidCount > 0 && InvalidCount == 0;
}

public sealed class QuestionImportService(
    ExamSimulatorDbContext dbContext,
    QuestionImportValidator validator)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<(ImportPreview? Preview, string? Error)> ParseAndValidateAsync(Stream fileStream)
    {
        QuestionImportFileDto? file;
        try
        {
            file = await JsonSerializer.DeserializeAsync<QuestionImportFileDto>(fileStream, JsonOptions);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid JSON: {ex.Message}");
        }

        if (file is null)
            return (null, "File is empty or could not be parsed.");

        if (string.IsNullOrWhiteSpace(file.ExamProfileId))
            return (null, "examProfileId is required.");

        var profileExists = await dbContext.ExamProfiles
            .AnyAsync(p => p.Id == file.ExamProfileId);

        if (!profileExists)
            return (null, $"Exam profile '{file.ExamProfileId}' was not found.");

        if (file.Questions is null || file.Questions.Count == 0)
            return (null, "The file contains no questions.");

        // Collect IDs from items that have one, to batch-check duplicates
        var incomingIds = file.Questions
            .Where(q => q.Id.HasValue)
            .Select(q => q.Id!.Value)
            .Distinct()
            .ToList();

        var existingIds = incomingIds.Count > 0
            ? (await dbContext.Questions
                .Where(q => incomingIds.Contains(q.Id))
                .Select(q => q.Id)
                .ToListAsync())
              .ToHashSet()
            : [];

        var items = new List<ImportItemPreview>(file.Questions.Count);
        for (int i = 0; i < file.Questions.Count; i++)
        {
            var item = file.Questions[i];

            if (item.Id.HasValue && existingIds.Contains(item.Id.Value))
            {
                items.Add(new ImportItemPreview(i + 1, item, ImportItemStatus.Duplicate, []));
                continue;
            }

            var errors = validator.Validate(item);
            var status = errors.Count == 0 ? ImportItemStatus.Valid : ImportItemStatus.Invalid;
            items.Add(new ImportItemPreview(i + 1, item, status, errors));
        }

        return (new ImportPreview(file.ExamProfileId, items), null);
    }

    public async Task<int> CommitAsync(ImportPreview preview)
    {
        var toInsert = preview.Items
            .Where(i => i.Status == ImportItemStatus.Valid)
            .Select(i => new Question(
                i.Item.Id ?? Guid.NewGuid(),
                preview.ExamProfileId,
                i.Item.Type,
                i.Item.Difficulty,
                i.Item.Prompt!,
                i.Item.Options!,
                i.Item.CorrectOptionIndices!,
                i.Item.TopicTag ?? string.Empty,
                i.Item.Explanation,
                i.Item.MatchingTargets))
            .ToList();

        dbContext.Questions.AddRange(toInsert);
        await dbContext.SaveChangesAsync();
        return toInsert.Count;
    }
}
