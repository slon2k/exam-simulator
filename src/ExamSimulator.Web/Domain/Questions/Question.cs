namespace ExamSimulator.Web.Domain.Questions;

public sealed class Question
{
    public Guid Id { get; private set; }

    public string ExamProfileId { get; private set; } = null!;

    public string Prompt { get; private set; } = null!;

    public IReadOnlyList<string> Options { get; private set; } = null!;

    public int CorrectOptionIndex { get; private set; }

    public string TopicTag { get; private set; } = null!;

    private Question() { } // for EF Core

    public Question(
        Guid id,
        string examProfileId,
        string prompt,
        IEnumerable<string> options,
        int correctOptionIndex,
        string topicTag)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Question id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(examProfileId))
        {
            throw new ArgumentException("Exam profile id is required.", nameof(examProfileId));
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        if (string.IsNullOrWhiteSpace(topicTag))
        {
            throw new ArgumentException("Topic tag is required.", nameof(topicTag));
        }

        ArgumentNullException.ThrowIfNull(options);

        var optionList = options.ToList();
        if (optionList.Count < 2)
        {
            throw new ArgumentException("Question must have at least 2 options.", nameof(options));
        }

        if (optionList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Each option must contain text.", nameof(options));
        }

        if (correctOptionIndex < 0 || correctOptionIndex >= optionList.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(correctOptionIndex),
                $"Correct option index must be between 0 and {optionList.Count - 1}.");
        }

        Id = id;
        ExamProfileId = examProfileId.Trim();
        Prompt = prompt.Trim();
        Options = optionList.Select(static option => option.Trim()).ToArray();
        CorrectOptionIndex = correctOptionIndex;
        TopicTag = topicTag.Trim();
    }
}
