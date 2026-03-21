using ExamSimulator.Web.Domain.Questions;

namespace ExamSimulator.Web.UnitTests.Questions;

public class QuestionTests
{
    [Fact]
    public void Constructor_WithValidInput_CreatesQuestion()
    {
        var id = Guid.NewGuid();
        var options = new[] { "A", "B", "C", "D" };

        var question = new Question(id, "az-204", "What is C#?", options, 2, "language");

        Assert.Equal(id, question.Id);
        Assert.Equal("az-204", question.ExamProfileId);
        Assert.Equal("What is C#?", question.Prompt);
        Assert.Equal(4, question.Options.Count);
        Assert.Equal("C", question.Options[2]);
        Assert.Equal(2, question.CorrectOptionIndex);
        Assert.Equal("language", question.TopicTag);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Constructor_WithValidCorrectOptionIndices_AcceptsValue(int correctOptionIndex)
    {
        var question = new Question(
            Guid.NewGuid(),
            "az-204",
            "Pick the right answer",
            new[] { "A", "B", "C", "D" },
            correctOptionIndex,
            "general");

        Assert.Equal(correctOptionIndex, question.CorrectOptionIndex);
    }

    [Fact]
    public void Constructor_WithTwoOptions_CreatesQuestion()
    {
        var question = new Question(
            Guid.NewGuid(), "az-204", "Is this correct?", new[] { "Yes", "No" }, 0, "general");

        Assert.Equal(2, question.Options.Count);
    }

    [Fact]
    public void Constructor_WithThreeOptions_CreatesQuestion()
    {
        var question = new Question(
            Guid.NewGuid(), "az-204", "Pick one", new[] { "A", "B", "C" }, 1, "general");

        Assert.Equal(3, question.Options.Count);
    }

    [Fact]
    public void Constructor_WhenPromptIsBlank_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "az-204", "  ", new[] { "A", "B", "C", "D" }, 0, "general"));

        Assert.Equal("prompt", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenOptionsCountIsOne_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "az-204", "Question", new[] { "A" }, 0, "general"));

        Assert.Equal("options", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Constructor_WhenCorrectOptionIsOutOfRange_ThrowsArgumentOutOfRangeException(int index)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Question(Guid.NewGuid(), "az-204", "Question", new[] { "A", "B", "C", "D" }, index, "general"));

        Assert.Equal("correctOptionIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenCorrectOptionIndexEqualsCount_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Question(Guid.NewGuid(), "az-204", "Question", new[] { "Yes", "No" }, 2, "general"));

        Assert.Equal("correctOptionIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenAnyOptionIsBlank_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Question(Guid.NewGuid(), "az-204", "Question", new[] { "A", " ", "C", "D" }, 0, "general"));

        Assert.Equal("options", ex.ParamName);
    }
}