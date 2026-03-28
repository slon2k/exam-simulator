namespace ExamSimulator.Web.Domain.Attempts;

public sealed class ExamAttemptAnswer
{
    public Guid Id { get; private set; }

    public Guid AttemptId { get; private set; }

    public Guid QuestionId { get; private set; }

    public bool IsCorrect { get; private set; }

    public IReadOnlyList<int>? SelectedOptionIndices { get; private set; }

    private ExamAttemptAnswer() { } // for EF Core

    public ExamAttemptAnswer(Guid id, Guid attemptId, Guid questionId, bool isCorrect, IReadOnlyList<int>? selectedOptionIndices = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Answer id cannot be empty.", nameof(id));

        if (attemptId == Guid.Empty)
            throw new ArgumentException("Attempt id cannot be empty.", nameof(attemptId));

        if (questionId == Guid.Empty)
            throw new ArgumentException("Question id cannot be empty.", nameof(questionId));

        Id = id;
        AttemptId = attemptId;
        QuestionId = questionId;
        IsCorrect = isCorrect;
        SelectedOptionIndices = selectedOptionIndices;
    }
}
