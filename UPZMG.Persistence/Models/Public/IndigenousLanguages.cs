using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Public module.
/// Represents the indigenous languages in the public module.
/// </summary>

public class IndigenousLanguages
{
    public Guid Id { get; set; }
    [MaxLength(100)]
    public required string Language { get; set; }
}

/// <summary>
/// Public module.
/// Represents the indigenous languages spoken by individuals in the public module.
/// </summary>
public class IndigenousLanguagesSpeakers
{
    public Guid Id { get; set; }
    public Guid SpeakerId { get; set; } //student / employee
    public Guid LanguageId { get; set; }
}