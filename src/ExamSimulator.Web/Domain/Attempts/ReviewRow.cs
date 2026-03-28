namespace ExamSimulator.Web.Domain.Attempts;

using ExamSimulator.Web.Domain.Questions;

public record ReviewRow(
    int Number,
    Question? Question,
    bool IsCorrect,
    IReadOnlyList<int>? SelectedIndices);
