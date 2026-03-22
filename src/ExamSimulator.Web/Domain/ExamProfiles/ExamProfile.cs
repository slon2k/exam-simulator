using System.Text.RegularExpressions;

namespace ExamSimulator.Web.Domain.ExamProfiles;

public sealed class ExamProfile
{
    private static readonly Regex SlugPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    private ExamProfile() { } // for EF Core

    public ExamProfile(string id, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Exam profile id is required.", nameof(id));

        if (!SlugPattern.IsMatch(id))
            throw new ArgumentException(
                "Exam profile id must be a slug: lowercase letters, digits, and hyphens only (no leading/trailing/consecutive hyphens).",
                nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Exam profile name is required.", nameof(name));

        Id = id;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
