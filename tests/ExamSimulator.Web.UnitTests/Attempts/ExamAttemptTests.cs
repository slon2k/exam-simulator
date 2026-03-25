using ExamSimulator.Web.Domain.Attempts;

namespace ExamSimulator.Web.UnitTests.Attempts;

public class ExamAttemptTests
{
    private static readonly Guid ValidId = Guid.NewGuid();
    private const string ValidUserId = "user-123";
    private const string ValidProfileId = "az-204";
    private static readonly DateTime ValidTakenAt = DateTime.UtcNow;
    private static readonly string[] ValidTags = ["compute", "storage"];
    private static readonly string[] ValidDifficulties = ["Easy", "Medium"];

    // ── valid construction ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_CreatesAttempt()
    {
        var attempt = new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 7, 10, ValidTags, ValidDifficulties, true);

        Assert.Equal(ValidId, attempt.Id);
        Assert.Equal(ValidUserId, attempt.UserId);
        Assert.Equal(ValidProfileId, attempt.ProfileId);
        Assert.Equal(ValidTakenAt, attempt.TakenAt);
        Assert.Equal(7, attempt.Score);
        Assert.Equal(10, attempt.Total);
        Assert.Equal(ValidTags, attempt.Tags);
        Assert.Equal(ValidDifficulties, attempt.Difficulties);
        Assert.True(attempt.RandomOrder);
    }

    [Fact]
    public void Constructor_ScoreEqualToTotal_IsValid()
    {
        var attempt = new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 10, 10, ValidTags, ValidDifficulties, false);

        Assert.Equal(10, attempt.Score);
        Assert.Equal(10, attempt.Total);
    }

    [Fact]
    public void Constructor_ScoreZero_IsValid()
    {
        var attempt = new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 0, 10, ValidTags, ValidDifficulties, false);

        Assert.Equal(0, attempt.Score);
    }

    [Fact]
    public void Constructor_TotalZero_IsValid()
    {
        var attempt = new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 0, 0, ValidTags, ValidDifficulties, false);

        Assert.Equal(0, attempt.Total);
    }

    [Fact]
    public void Constructor_EmptyTags_IsValid()
    {
        var attempt = new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 0, 0, [], ValidDifficulties, false);

        Assert.Empty(attempt.Tags);
    }

    [Fact]
    public void Constructor_EmptyDifficulties_IsValid()
    {
        var attempt = new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 0, 0, ValidTags, [], false);

        Assert.Empty(attempt.Difficulties);
    }

    // ── id validation ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ExamAttempt(Guid.Empty, ValidUserId, ValidProfileId, ValidTakenAt, 0, 10, ValidTags, ValidDifficulties, false));

        Assert.Contains("id", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── userId validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceUserId_Throws(string userId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ExamAttempt(ValidId, userId, ValidProfileId, ValidTakenAt, 0, 10, ValidTags, ValidDifficulties, false));

        Assert.Contains("userId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── profileId validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceProfileId_Throws(string profileId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ExamAttempt(ValidId, ValidUserId, profileId, ValidTakenAt, 0, 10, ValidTags, ValidDifficulties, false));

        Assert.Contains("profileId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── score/total validation ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_NegativeTotal_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 0, -1, ValidTags, ValidDifficulties, false));

        Assert.Contains("total", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_NegativeScore_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, -1, 10, ValidTags, ValidDifficulties, false));

        Assert.Contains("score", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_ScoreExceedsTotal_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ExamAttempt(ValidId, ValidUserId, ValidProfileId, ValidTakenAt, 11, 10, ValidTags, ValidDifficulties, false));

        Assert.Contains("score", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
