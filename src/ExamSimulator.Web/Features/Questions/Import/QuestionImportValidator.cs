using ExamSimulator.Web.Domain.Questions;

namespace ExamSimulator.Web.Features.Questions.Import;

public sealed class QuestionImportValidator
{
    public IReadOnlyList<string> Validate(QuestionImportItemDto item)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(item.Prompt))
            errors.Add("Prompt is required.");

        // Options are required for all types
        if (item.Options is null || item.Options.Count < 2)
        {
            errors.Add("At least 2 options are required.");
            // Can't validate indices without options — return early
            return errors;
        }

        if (item.Options.Any(string.IsNullOrWhiteSpace))
            errors.Add("Each option must contain text.");

        switch (item.Type)
        {
            case QuestionType.SingleChoice:
                ValidateSingleChoice(item, errors);
                break;

            case QuestionType.MultipleChoice:
                ValidateMultipleChoice(item, errors);
                break;

            case QuestionType.Ordering:
                ValidateOrdering(item, errors);
                break;

            case QuestionType.BuildList:
                ValidateBuildList(item, errors);
                break;

            case QuestionType.Matching:
                ValidateMatching(item, errors);
                break;

            default:
                errors.Add($"Unknown question type '{item.Type}'.");
                break;
        }

        return errors;
    }

    private static void ValidateSingleChoice(QuestionImportItemDto item, List<string> errors)
    {
        if (item.CorrectOptionIndices is null || item.CorrectOptionIndices.Count == 0)
        {
            errors.Add("SingleChoice: exactly one correct option index is required.");
            return;
        }

        if (item.CorrectOptionIndices.Count != 1)
            errors.Add("SingleChoice: exactly one correct option index is required.");

        ValidateIndicesInRange(item, errors);
    }

    private static void ValidateMultipleChoice(QuestionImportItemDto item, List<string> errors)
    {
        if (item.CorrectOptionIndices is null || item.CorrectOptionIndices.Count == 0)
        {
            errors.Add("MultipleChoice: at least one correct option index is required.");
            return;
        }

        ValidateIndicesInRange(item, errors);
    }

    private static void ValidateOrdering(QuestionImportItemDto item, List<string> errors)
    {
        if (item.CorrectOptionIndices is null || item.CorrectOptionIndices.Count == 0)
        {
            errors.Add("Ordering: correct option indices (full permutation) are required.");
            return;
        }

        if (item.CorrectOptionIndices.Count != item.Options!.Count)
            errors.Add("Ordering: correctOptionIndices must contain one index per option (a full permutation).");

        ValidateIndicesInRange(item, errors);
    }

    private static void ValidateBuildList(QuestionImportItemDto item, List<string> errors)
    {
        if (item.CorrectOptionIndices is null || item.CorrectOptionIndices.Count < 2)
        {
            errors.Add("BuildList: at least 2 correct option indices are required.");
            return;
        }

        if (item.CorrectOptionIndices.Count >= item.Options!.Count)
            errors.Add("BuildList: answer must be a proper subset of the options pool.");

        ValidateIndicesInRange(item, errors);
    }

    private static void ValidateMatching(QuestionImportItemDto item, List<string> errors)
    {
        if (item.MatchingTargets is null || item.MatchingTargets.Count == 0)
        {
            errors.Add("Matching: matchingTargets are required.");
            return;
        }

        if (item.MatchingTargets.Count < item.Options!.Count)
            errors.Add("Matching: matchingTargets must have at least as many entries as options (premises).");

        if (item.MatchingTargets.Any(string.IsNullOrWhiteSpace))
            errors.Add("Matching: each matching target must contain text.");

        if (item.CorrectOptionIndices is null || item.CorrectOptionIndices.Count != item.Options.Count)
            errors.Add("Matching: correctOptionIndices must have one pairing index per premise.");

        if (item.CorrectOptionIndices is not null && item.CorrectOptionIndices.Any(i => i < 0 || i >= item.MatchingTargets.Count))
            errors.Add("Matching: all pairing indices must be within the matchingTargets range.");
    }

    private static void ValidateIndicesInRange(QuestionImportItemDto item, List<string> errors)
    {
        if (item.CorrectOptionIndices is null) return;

        if (item.CorrectOptionIndices.Any(i => i < 0 || i >= item.Options!.Count))
            errors.Add("correctOptionIndices contains an out-of-range index.");

        if (item.CorrectOptionIndices.Distinct().Count() != item.CorrectOptionIndices.Count)
            errors.Add("correctOptionIndices must be unique.");
    }
}
