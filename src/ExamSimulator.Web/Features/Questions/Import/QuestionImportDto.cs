using System.Text.Json.Serialization;
using ExamSimulator.Web.Domain.Questions;

namespace ExamSimulator.Web.Features.Questions.Import;

public sealed record QuestionImportFileDto(
    [property: JsonPropertyName("examProfileId")] string ExamProfileId,
    [property: JsonPropertyName("questions")] List<QuestionImportItemDto> Questions
);

public sealed record QuestionImportItemDto(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("type")] QuestionType Type,
    [property: JsonPropertyName("difficulty")] Difficulty Difficulty,
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("options")] List<string>? Options,
    [property: JsonPropertyName("correctOptionIndices")] List<int>? CorrectOptionIndices,
    [property: JsonPropertyName("topicTag")] string? TopicTag,
    [property: JsonPropertyName("explanation")] string? Explanation,
    [property: JsonPropertyName("matchingTargets")] List<string>? MatchingTargets
);
