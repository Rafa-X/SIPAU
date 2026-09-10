namespace UPZMG.Implementations.Public;

using UPZMG.Shared.Enums;

public sealed class GetIndigenousLanguage
{
    public required Guid Id { get; set; }
    public required Guid SpeakerId { get; set; } //student / employee
    public required Guid LanguageId { get; set; }
}