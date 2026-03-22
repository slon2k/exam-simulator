using ExamSimulator.Web.Domain.ExamProfiles;

namespace ExamSimulator.Web.UnitTests.ExamProfiles;

public class ExamProfileTests
{
    // ── valid construction ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidIdAndName_CreatesProfile()
    {
        var profile = new ExamProfile("az-204", "Azure Developer Associate AZ-204");

        Assert.Equal("az-204", profile.Id);
        Assert.Equal("Azure Developer Associate AZ-204", profile.Name);
        Assert.Null(profile.Description);
    }

    [Fact]
    public void Constructor_WithDescription_SetsDescription()
    {
        var profile = new ExamProfile("az-204", "Azure Developer", "Prep for AZ-204");

        Assert.Equal("Prep for AZ-204", profile.Description);
    }

    [Fact]
    public void Constructor_WithNullDescription_SetsDescriptionNull()
    {
        var profile = new ExamProfile("az-204", "Azure Developer", null);

        Assert.Null(profile.Description);
    }

    [Fact]
    public void Constructor_WithWhitespaceDescription_SetsDescriptionNull()
    {
        var profile = new ExamProfile("az-204", "Azure Developer", "   ");

        Assert.Null(profile.Description);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var profile = new ExamProfile("az-204", "  Azure Developer  ");

        Assert.Equal("Azure Developer", profile.Name);
    }

    [Fact]
    public void Constructor_TrimsDescription()
    {
        var profile = new ExamProfile("az-204", "Azure Developer", "  Some description  ");

        Assert.Equal("Some description", profile.Description);
    }

    [Theory]
    [InlineData("az-204")]
    [InlineData("az204")]
    [InlineData("a")]
    [InlineData("az-100-200")]
    [InlineData("sc900")]
    public void Constructor_WithValidSlugId_CreatesProfile(string id)
    {
        var profile = new ExamProfile(id, "Test Exam");

        Assert.Equal(id, profile.Id);
    }

    // ── id validation ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile(null!, "Azure Developer"));
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile("", "Azure Developer"));
    }

    [Fact]
    public void Constructor_WithWhitespaceId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile("   ", "Azure Developer"));
    }

    [Theory]
    [InlineData("AZ-204")]          // uppercase
    [InlineData("az 204")]          // space
    [InlineData("-az-204")]         // leading hyphen
    [InlineData("az-204-")]         // trailing hyphen
    [InlineData("az--204")]         // consecutive hyphens
    [InlineData("az_204")]          // underscore
    [InlineData("az.204")]          // dot
    public void Constructor_WithInvalidSlugId_Throws(string id)
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile(id, "Azure Developer"));
    }

    // ── name validation ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile("az-204", null!));
    }

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile("az-204", ""));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExamProfile("az-204", "   "));
    }
}
