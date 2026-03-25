namespace ExamSimulator.Web.Domain.Attempts;

public sealed class ExamAttempt
{
    public Guid Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public string ProfileId { get; private set; } = null!;

    public DateTime TakenAt { get; private set; }

    public int Score { get; private set; }

    public int Total { get; private set; }

    public IReadOnlyList<string> Tags { get; private set; } = null!;

    public IReadOnlyList<string> Difficulties { get; private set; } = null!;

    public bool RandomOrder { get; private set; }

    private readonly List<ExamAttemptAnswer> _answers = [];

    public IReadOnlyList<ExamAttemptAnswer> Answers => _answers.AsReadOnly();

    private ExamAttempt() { } // for EF Core

    public ExamAttempt(
        Guid id,
        string userId,
        string profileId,
        DateTime takenAt,
        int score,
        int total,
        IEnumerable<string> tags,
        IEnumerable<string> difficulties,
        bool randomOrder)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Attempt id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User id is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile id is required.", nameof(profileId));

        if (total < 0)
            throw new ArgumentException("Total cannot be negative.", nameof(total));

        if (score < 0 || score > total)
            throw new ArgumentException("Score must be between 0 and total.", nameof(score));

        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(difficulties);

        Id = id;
        UserId = userId;
        ProfileId = profileId;
        TakenAt = takenAt;
        Score = score;
        Total = total;
        Tags = tags.ToList();
        Difficulties = difficulties.ToList();
        RandomOrder = randomOrder;
    }
}
