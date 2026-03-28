using ExamSimulator.Web.Domain.Attempts;

namespace ExamSimulator.Web.UnitTests.Attempts;

public class ExamAttemptAnswerTests
{
    [Fact]
    public void SelectedOptionIndices_WhenNotProvided_IsNull()
    {
        var answer = new ExamAttemptAnswer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true);

        Assert.Null(answer.SelectedOptionIndices);
    }

    [Fact]
    public void SelectedOptionIndices_WhenSingleChoiceSelection_StoresCorrectly()
    {
        IReadOnlyList<int> indices = [2];

        var answer = new ExamAttemptAnswer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, indices);

        Assert.NotNull(answer.SelectedOptionIndices);
        Assert.Equal([2], answer.SelectedOptionIndices);
    }

    [Fact]
    public void SelectedOptionIndices_WhenMultipleChoiceSelection_StoresCorrectly()
    {
        IReadOnlyList<int> indices = [0, 2];

        var answer = new ExamAttemptAnswer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false, indices);

        Assert.NotNull(answer.SelectedOptionIndices);
        Assert.Equal([0, 2], answer.SelectedOptionIndices);
    }

    [Fact]
    public void SelectedOptionIndices_WhenNull_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            new ExamAttemptAnswer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, null));

        Assert.Null(exception);
    }
}
