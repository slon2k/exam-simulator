namespace ExamSimulator.Web.Domain.Questions;

public sealed class Question
{
    public Guid Id { get; private set; }

    public string ExamProfileId { get; private set; } = null!;

    public QuestionType Type { get; private set; }

    public Difficulty Difficulty { get; private set; }

    public string Prompt { get; private set; } = null!;

    public IReadOnlyList<string> Options { get; private set; } = null!;

    public IReadOnlyList<int> CorrectOptionIndices { get; private set; } = null!;

    public IReadOnlyList<string>? MatchingTargets { get; private set; }

    public string? Explanation { get; private set; }

    public string TopicTag { get; private set; } = null!;

    private Question() { } // for EF Core

    public Question(
        Guid id,
        string examProfileId,
        QuestionType type,
        Difficulty difficulty,
        string prompt,
        IEnumerable<string> options,
        IEnumerable<int> correctOptionIndices,
        string topicTag,
        string? explanation = null,
        IEnumerable<string>? matchingTargets = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Question id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(examProfileId))
            throw new ArgumentException("Exam profile id is required.", nameof(examProfileId));

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt is required.", nameof(prompt));

        if (string.IsNullOrWhiteSpace(topicTag))
            throw new ArgumentException("Topic tag is required.", nameof(topicTag));

        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(correctOptionIndices);

        var optionList = options.ToList();
        if (optionList.Count < 2)
            throw new ArgumentException("Question must have at least 2 options.", nameof(options));

        if (optionList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Each option must contain text.", nameof(options));

        var indexList = correctOptionIndices.ToList();
        if (indexList.Count == 0)
            throw new ArgumentException("At least one correct option index is required.", nameof(correctOptionIndices));

        if (indexList.Any(i => i < 0 || i >= optionList.Count))
            throw new ArgumentOutOfRangeException(nameof(correctOptionIndices), "All correct option indices must be within the options range.");

        if (indexList.Distinct().Count() != indexList.Count)
            throw new ArgumentException("Correct option indices must be unique.", nameof(correctOptionIndices));

        if (type == QuestionType.SingleChoice && indexList.Count != 1)
            throw new ArgumentException("Single choice questions must have exactly one correct option.", nameof(correctOptionIndices));

        if (type == QuestionType.Ordering && indexList.Count != optionList.Count)
            throw new ArgumentException("Ordering questions must have one index per option (a full permutation).", nameof(correctOptionIndices));

        if (type == QuestionType.BuildList)
        {
            if (indexList.Count < 2)
                throw new ArgumentException("BuildList questions must have at least 2 items in the answer.", nameof(correctOptionIndices));
            if (indexList.Count >= optionList.Count)
                throw new ArgumentException("BuildList answer must be a proper subset of the options pool.", nameof(correctOptionIndices));
        }

        if (type == QuestionType.Matching)
        {
            if (matchingTargets is null)
                throw new ArgumentNullException(nameof(matchingTargets), "Matching questions require matching targets.");
            var targetList = matchingTargets.ToList();
            if (targetList.Count < optionList.Count)
                throw new ArgumentException("Matching targets must have at least as many entries as premises.", nameof(matchingTargets));
            if (targetList.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Each matching target must contain text.", nameof(matchingTargets));
            if (indexList.Count != optionList.Count)
                throw new ArgumentException("Matching questions must have one correct pairing per premise.", nameof(correctOptionIndices));
            if (indexList.Any(i => i < 0 || i >= targetList.Count))
                throw new ArgumentOutOfRangeException(nameof(correctOptionIndices), "All pairing indices must be within the matching targets range.");
            MatchingTargets = targetList.Select(static t => t.Trim()).ToArray();
        }

        Id = id;
        ExamProfileId = examProfileId.Trim();
        Type = type;
        Difficulty = difficulty;
        Prompt = prompt.Trim();
        Options = optionList.Select(static o => o.Trim()).ToArray();
        CorrectOptionIndices = indexList.ToArray();
        TopicTag = topicTag.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
    }
}

