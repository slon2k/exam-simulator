using ExamSimulator.Web.Domain.Questions;

namespace ExamSimulator.Web.UnitTests.Questions;

public class QuestionTests
{
    // ── helpers ────────────────────────────────────────────────────────────────

    private static Question SingleChoice(
        string[]? options = null,
        int[]? correctIndices = null,
        Difficulty difficulty = Difficulty.Medium,
        string? explanation = null) =>
        new(
            Guid.NewGuid(),
            "az-204",
            QuestionType.SingleChoice,
            difficulty,
            "What is Azure App Service?",
            options ?? ["A", "B", "C", "D"],
            correctIndices ?? [0],
            "app-service",
            explanation);

    private static Question MultipleChoice(
        string[]? options = null,
        int[]? correctIndices = null) =>
        new(
            Guid.NewGuid(),
            "az-204",
            QuestionType.MultipleChoice,
            Difficulty.Hard,
            "Which of the following are Azure compute services?",
            options ?? ["App Service", "Blob Storage", "AKS", "Key Vault"],
            correctIndices ?? [0, 2],
            "compute");

    // ── basic construction ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidSingleChoiceInput_CreatesQuestion()
    {
        var id = Guid.NewGuid();

        var question = new Question(
            id,
            "az-204",
            QuestionType.SingleChoice,
            Difficulty.Easy,
            "What is C#?",
            ["A", "B", "C", "D"],
            [2],
            "language",
            "C# is a modern object-oriented language.");

        Assert.Equal(id, question.Id);
        Assert.Equal("az-204", question.ExamProfileId);
        Assert.Equal(QuestionType.SingleChoice, question.Type);
        Assert.Equal(Difficulty.Easy, question.Difficulty);
        Assert.Equal("What is C#?", question.Prompt);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("C", question.Options[2]);
        Assert.Equal([2], question.CorrectOptionIndices);
        Assert.Equal("language", question.TopicTag);
        Assert.Equal("C# is a modern object-oriented language.", question.Explanation);
    }

    [Fact]
    public void Constructor_WithValidMultipleChoiceInput_CreatesQuestion()
    {
        var question = MultipleChoice();

        Assert.Equal(QuestionType.MultipleChoice, question.Type);
        Assert.Equal(2, question.CorrectOptionIndices.Count);
        Assert.Contains(0, question.CorrectOptionIndices);
        Assert.Contains(2, question.CorrectOptionIndices);
    }

    [Fact]
    public void Constructor_WithNullExplanation_SetsExplanationNull()
    {
        var question = SingleChoice(explanation: null);
        Assert.Null(question.Explanation);
    }

    [Fact]
    public void Constructor_WithBlankExplanation_SetsExplanationNull()
    {
        var question = SingleChoice(explanation: "   ");
        Assert.Null(question.Explanation);
    }

    [Fact]
    public void Constructor_WithTwoOptions_CreatesQuestion()
    {
        var question = SingleChoice(options: ["Yes", "No"], correctIndices: [0]);
        Assert.Equal(2, question.Options.Count);
    }

    // ── difficulty ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Medium)]
    [InlineData(Difficulty.Hard)]
    public void Constructor_WithAnyDifficulty_SetsCorrectly(Difficulty difficulty)
    {
        var question = SingleChoice(difficulty: difficulty);
        Assert.Equal(difficulty, question.Difficulty);
    }

    // ── single-choice invariants ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Constructor_SingleChoice_WithValidIndex_Accepts(int index)
    {
        var question = SingleChoice(correctIndices: [index]);
        Assert.Equal([index], question.CorrectOptionIndices);
    }

    [Fact]
    public void Constructor_SingleChoice_WithTwoIndices_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SingleChoice(correctIndices: [0, 1]));

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    // ── multiple-choice invariants ─────────────────────────────────────────────

    [Fact]
    public void Constructor_MultipleChoice_WithAllOptionsCorrect_Accepts()
    {
        var question = MultipleChoice(correctIndices: [0, 1, 2, 3]);
        Assert.Equal(4, question.CorrectOptionIndices.Count);
    }

    [Fact]
    public void Constructor_MultipleChoice_WithDuplicateIndices_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            MultipleChoice(correctIndices: [0, 0]));

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    [Fact]
    public void Constructor_MultipleChoice_WithEmptyIndices_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            MultipleChoice(correctIndices: []));

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Constructor_MultipleChoice_WithOutOfRangeIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            MultipleChoice(correctIndices: [0, index]));

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    // ── ordering invariants ───────────────────────────────────────────────────

    private static Question Ordering(string[]? options = null, int[]? correctIndices = null)
    {
        var opts = options ?? ["First", "Second", "Third", "Fourth"];
        var idx = correctIndices ?? Enumerable.Range(0, opts.Length).ToArray();
        return new(Guid.NewGuid(), "az-204", QuestionType.Ordering, Difficulty.Medium,
            "Arrange in order", opts, idx, "ordering");
    }

    [Fact]
    public void Constructor_Ordering_WithFullPermutation_CreatesQuestion()
    {
        var question = Ordering(correctIndices: [3, 0, 2, 1]);

        Assert.Equal(QuestionType.Ordering, question.Type);
        Assert.Equal([3, 0, 2, 1], question.CorrectOptionIndices);
    }

    [Fact]
    public void Constructor_Ordering_WithFewerIndicesThanOptions_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Ordering(correctIndices: [0, 1, 2])); // 4 options, only 3 indices

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    [Fact]
    public void Constructor_Ordering_WithMoreIndicesThanOptions_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Ordering(options: ["A", "B", "C"], correctIndices: [0, 1, 2, 0])); // 3 options, 4 indices

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    [Fact]
    public void Constructor_Ordering_WithDuplicateIndices_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Ordering(options: ["A", "B", "C"], correctIndices: [0, 0, 2]));

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    [Fact]
    public void Constructor_Ordering_WithOutOfRangeIndex_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ordering(options: ["A", "B", "C"], correctIndices: [0, 1, 5]));

        Assert.Equal("correctOptionIndices", ex.ParamName);
    }

    [Fact]
    public void Constructor_Ordering_WithIdentityPermutation_IsValid()
    {
        var question = Ordering(correctIndices: [0, 1, 2, 3]);

        Assert.Equal([0, 1, 2, 3], question.CorrectOptionIndices);
    }

    [Fact]
    public void Constructor_Ordering_WithTwoOptions_ValidPermutation_Accepts()
    {
        var question = Ordering(options: ["First", "Second"], correctIndices: [1, 0]);

        Assert.Equal(2, question.Options.Count);
        Assert.Equal([1, 0], question.CorrectOptionIndices);
    }

    // ── shared option/prompt invariants ───────────────────────────────────────

    [Fact]
    public void Constructor_WhenPromptIsBlank_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Easy,
                "  ", ["A", "B"], [0], "general"));

        Assert.Equal("prompt", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenOptionsCountIsOne_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Easy,
                "Question", ["A"], [0], "general"));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenAnyOptionIsBlank_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Easy,
                "Question", ["A", " ", "C"], [0], "general"));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenExamProfileIdIsBlank_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "  ", QuestionType.SingleChoice, Difficulty.Easy,
                "Question", ["A", "B"], [0], "general"));

        Assert.Equal("examProfileId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenIdIsEmpty_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.Empty, "az-204", QuestionType.SingleChoice, Difficulty.Easy,
                "Question", ["A", "B"], [0], "general"));

        Assert.Equal("id", ex.ParamName);
    }
}
