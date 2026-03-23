using ExamSimulator.Web.Domain.Questions;
using ExamSimulator.Web.Features.Questions.Import;

namespace ExamSimulator.Web.UnitTests.Questions;

public class QuestionImportValidatorTests
{
    private readonly QuestionImportValidator _validator = new();

    // ── helpers ────────────────────────────────────────────────────────────────

    private static QuestionImportItemDto SingleChoiceItem(
        string? prompt = "What is Azure App Service?",
        List<string>? options = null,
        List<int>? correctIndices = null) =>
        new(
            Id: Guid.NewGuid(),
            Type: QuestionType.SingleChoice,
            Difficulty: Difficulty.Medium,
            Prompt: prompt,
            Options: options ?? ["A PaaS offering", "An IaaS offering", "A SaaS offering"],
            CorrectOptionIndices: correctIndices ?? [0],
            TopicTag: "app-service",
            Explanation: null,
            MatchingTargets: null
        );

    private static QuestionImportItemDto MatchingItem(
        List<string>? options = null,
        List<string>? targets = null,
        List<int>? correctIndices = null) =>
        new(
            Id: Guid.NewGuid(),
            Type: QuestionType.Matching,
            Difficulty: Difficulty.Hard,
            Prompt: "Match each service to its category.",
            Options: options ?? ["App Service", "Blob Storage"],
            CorrectOptionIndices: correctIndices ?? [0, 1],
            TopicTag: "azure",
            Explanation: null,
            MatchingTargets: targets ?? ["PaaS", "Storage"]
        );

    // ── SingleChoice ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidSingleChoiceItem_ReturnsNoErrors()
    {
        var errors = _validator.Validate(SingleChoiceItem());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyPrompt_ReturnsError()
    {
        var item = SingleChoiceItem(prompt: "   ");

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("Prompt"));
    }

    [Fact]
    public void Validate_NullPrompt_ReturnsError()
    {
        var item = SingleChoiceItem(prompt: null);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("Prompt"));
    }

    [Fact]
    public void Validate_SingleChoiceWithMultipleCorrectIndices_ReturnsError()
    {
        var item = SingleChoiceItem(correctIndices: [0, 1]);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("SingleChoice") && e.Contains("exactly one"));
    }

    [Fact]
    public void Validate_OutOfRangeCorrectIndex_ReturnsError()
    {
        var item = SingleChoiceItem(
            options: ["A", "B", "C"],
            correctIndices: [5]);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("out-of-range"));
    }

    [Fact]
    public void Validate_DuplicateCorrectIndices_ReturnsError()
    {
        var item = new QuestionImportItemDto(
            Id: Guid.NewGuid(),
            Type: QuestionType.MultipleChoice,
            Difficulty: Difficulty.Medium,
            Prompt: "Pick two.",
            Options: ["A", "B", "C"],
            CorrectOptionIndices: [0, 0],
            TopicTag: "test",
            Explanation: null,
            MatchingTargets: null
        );

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("unique"));
    }

    [Fact]
    public void Validate_TooFewOptions_ReturnsError()
    {
        var item = SingleChoiceItem(options: ["OnlyOne"]);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("2 options"));
    }

    // ── Matching ───────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidMatchingItem_ReturnsNoErrors()
    {
        var errors = _validator.Validate(MatchingItem());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MatchingWithoutMatchingTargets_ReturnsError()
    {
        var item = MatchingItem(targets: null);

        // Override with null targets
        var itemWithNullTargets = item with { MatchingTargets = null };

        var errors = _validator.Validate(itemWithNullTargets);

        Assert.Contains(errors, e => e.Contains("matchingTargets") && e.Contains("required"));
    }

    [Fact]
    public void Validate_MatchingWithMismatchedTargetCount_ReturnsError()
    {
        // 3 options but only 1 target
        var item = MatchingItem(
            options: ["A", "B", "C"],
            targets: ["TargetA"],
            correctIndices: [0, 0, 0]);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("matchingTargets") && e.Contains("at least as many"));
    }

    [Fact]
    public void Validate_MatchingWithWrongPairingCount_ReturnsError()
    {
        // 2 premises but 3 pairing indices
        var item = MatchingItem(
            options: ["A", "B"],
            targets: ["T1", "T2"],
            correctIndices: [0, 1, 0]);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("one pairing index per premise"));
    }

    [Fact]
    public void Validate_MatchingWithOutOfRangePairingIndex_ReturnsError()
    {
        // Target index 5 is out of range for 2 targets
        var item = MatchingItem(
            options: ["A", "B"],
            targets: ["T1", "T2"],
            correctIndices: [0, 5]);

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("within the matchingTargets range"));
    }

    // ── Ordering / BuildList ───────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidOrderingItem_ReturnsNoErrors()
    {
        var item = new QuestionImportItemDto(
            Id: Guid.NewGuid(),
            Type: QuestionType.Ordering,
            Difficulty: Difficulty.Easy,
            Prompt: "Order the steps.",
            Options: ["Step A", "Step B", "Step C"],
            CorrectOptionIndices: [2, 0, 1],
            TopicTag: "process",
            Explanation: null,
            MatchingTargets: null
        );

        var errors = _validator.Validate(item);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_OrderingWithWrongIndexCount_ReturnsError()
    {
        var item = new QuestionImportItemDto(
            Id: Guid.NewGuid(),
            Type: QuestionType.Ordering,
            Difficulty: Difficulty.Easy,
            Prompt: "Order the steps.",
            Options: ["Step A", "Step B", "Step C"],
            CorrectOptionIndices: [0, 1],   // only 2 instead of 3
            TopicTag: "process",
            Explanation: null,
            MatchingTargets: null
        );

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("full permutation"));
    }

    [Fact]
    public void Validate_ValidBuildListItem_ReturnsNoErrors()
    {
        var item = new QuestionImportItemDto(
            Id: Guid.NewGuid(),
            Type: QuestionType.BuildList,
            Difficulty: Difficulty.Medium,
            Prompt: "Select and order the steps.",
            Options: ["A", "B", "C", "D"],
            CorrectOptionIndices: [0, 2],
            TopicTag: "process",
            Explanation: null,
            MatchingTargets: null
        );

        var errors = _validator.Validate(item);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BuildListWithAllOptionsSelected_ReturnsError()
    {
        var item = new QuestionImportItemDto(
            Id: Guid.NewGuid(),
            Type: QuestionType.BuildList,
            Difficulty: Difficulty.Medium,
            Prompt: "Select and order.",
            Options: ["A", "B", "C"],
            CorrectOptionIndices: [0, 1, 2],  // all items = not a proper subset
            TopicTag: "process",
            Explanation: null,
            MatchingTargets: null
        );

        var errors = _validator.Validate(item);

        Assert.Contains(errors, e => e.Contains("proper subset"));
    }
}
